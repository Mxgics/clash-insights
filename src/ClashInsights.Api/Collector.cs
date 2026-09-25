using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
namespace ClashInsights.Api;

public sealed record FetchResult(string Status, string? Json = null, TimeSpan? RetryAfter = null);
public sealed class ClashClient(HttpClient http, IConfiguration config) {
    public static bool ValidPayload(JsonElement data, string route) {
        if (data.ValueKind != JsonValueKind.Object) return false;
        var key = route.EndsWith("/currentwar") ? "state" : "name";
        if (!data.TryGetProperty(key, out var value) || value.ValueKind != JsonValueKind.String) return false;
        if (!data.TryGetProperty("memberList", out var members)) return true;
        return members.ValueKind == JsonValueKind.Array && members.EnumerateArray().All(m => m.ValueKind == JsonValueKind.Object
            && m.TryGetProperty("tag",out var tag) && tag.ValueKind == JsonValueKind.String
            && m.TryGetProperty("name",out var name) && name.ValueKind == JsonValueKind.String);
    }
    public async Task<FetchResult> Fetch(string route, CancellationToken ct) {
        var token = config["Clash:ApiToken"];
        if (string.IsNullOrWhiteSpace(token)) return new("Not configured");
        using var request = new HttpRequestMessage(HttpMethod.Get, route);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        try {
            using var response = await http.SendAsync(request, ct);
            if (response.StatusCode == HttpStatusCode.TooManyRequests) {
                var delay = response.Headers.RetryAfter?.Delta ?? (response.Headers.RetryAfter?.Date - DateTimeOffset.UtcNow) ?? TimeSpan.FromMinutes(5);
                return new("Rate limited", RetryAfter: delay > TimeSpan.Zero ? delay : TimeSpan.FromMinutes(1));
            }
            if (response.StatusCode == HttpStatusCode.Forbidden && route.EndsWith("/currentwar", StringComparison.Ordinal))
                return new("War access denied: check war log visibility, key or IP");
            if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized) return new("Access denied: check key, IP or data visibility");
            if (response.StatusCode == HttpStatusCode.NotFound) return new("Not found");
            if (!response.IsSuccessStatusCode) return new("Upstream unavailable");
            var json = await response.Content.ReadAsStringAsync(ct);
            using var document = JsonDocument.Parse(json);
            if (!ValidPayload(document.RootElement, route)) return new("Invalid upstream data");
            return new("Collected", json);
        } catch (HttpRequestException) { return new("Network unavailable"); }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested) { return new("Request timed out"); }
        catch (JsonException) { return new("Invalid upstream data"); }
    }
}
public sealed class CollectorStatus { public string Message { get; set; } = "Starting"; }
public sealed class Collector(IServiceScopeFactory scopes, IOptions<TrackingOptions> options, CollectorStatus status, ILogger<Collector> log) : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        var settings = options.Value;
        if (settings.Mode == "Demo") { status.Message = "Demo mode · no Supercell requests"; return; }
        while (!stoppingToken.IsCancellationRequested) {
            var wait = TimeSpan.FromMinutes(settings.IntervalMinutes);
            try {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<InsightDb>();
                // A session advisory lock also protects against a second local host.
                await db.Database.OpenConnectionAsync(stoppingToken);
                var connection = db.Database.GetDbConnection();
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT pg_try_advisory_lock(739125)";
                if (!(bool)(await command.ExecuteScalarAsync(stoppingToken))!) { status.Message = "Another collector is running"; }
                else {
                    try {
                        wait = await Collect(scope.ServiceProvider, settings, stoppingToken) ?? wait;
                        var cutoff = DateTimeOffset.UtcNow.AddDays(-settings.RetentionDays);
                        await db.Snapshots.Where(x => x.Source == "Live" && x.ObservedAt < cutoff).ExecuteDeleteAsync(stoppingToken);
                        await db.Attempts.Where(x => x.Source == "Live" && x.At < cutoff).ExecuteDeleteAsync(stoppingToken);
                    }
                    finally { command.CommandText = "SELECT pg_advisory_unlock(739125)"; await command.ExecuteScalarAsync(CancellationToken.None); }
                }
            } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { status.Message = "Collection failed · check local service logs"; log.LogError(ex, "Collection cycle failed"); }
            await Task.Delay(wait, stoppingToken);
        }
    }
    private async Task<TimeSpan?> Collect(IServiceProvider services, TrackingOptions settings, CancellationToken ct) {
        var db = services.GetRequiredService<InsightDb>();
        var client = services.GetRequiredService<ClashClient>();
        var config = services.GetRequiredService<IConfiguration>();
        if (string.IsNullOrWhiteSpace(config["Clash:ApiToken"])) { status.Message = "Live collection paused · API token not configured"; return null; }
        var global = await db.Schedules.FindAsync(["global"], ct);
        if (global is not null && global.NextEligibleAt > DateTimeOffset.UtcNow) {
            status.Message = "Rate limited · waiting for persisted retry time";
            return global.NextEligibleAt - DateTimeOffset.UtcNow;
        }
        var targets = settings.PlayerTags.Select(t => (Kind: "player", Tag: Tags.Normalize(t), Route: "players/" + Uri.EscapeDataString(Tags.Normalize(t))))
            .Concat(settings.ClanTags.SelectMany(t => new[] { (Kind: "clan", Tag: Tags.Normalize(t), Route: "clans/" + Uri.EscapeDataString(Tags.Normalize(t))),
                (Kind: "war", Tag: Tags.Normalize(t), Route: "clans/" + Uri.EscapeDataString(Tags.Normalize(t)) + "/currentwar") })).Distinct().ToArray();
        if (targets.Length == 0) { status.Message = "No tracked tags · update local configuration"; return null; }
        var failures = 0;
        foreach (var target in targets) {
            var id = target.Kind + ":" + target.Tag;
            var schedule = await db.Schedules.FindAsync([id], ct);
            if (schedule is not null && schedule.NextEligibleAt > DateTimeOffset.UtcNow) continue;
            var result = await client.Fetch(target.Route, ct);
            var now = DateTimeOffset.UtcNow;
            db.Attempts.Add(new() { Kind = target.Kind, Tag = target.Tag, At = now, Status = result.Status });
            if (schedule is null) { schedule = new Schedule { Id = id }; db.Schedules.Add(schedule); }
            schedule.NextEligibleAt = now.AddMinutes(settings.IntervalMinutes);
            if (result.RetryAfter is {} retry) {
                global ??= new Schedule { Id = "global" };
                if (db.Entry(global).State == EntityState.Detached) db.Schedules.Add(global);
                global.NextEligibleAt = now.Add(retry);
                schedule.NextEligibleAt = global.NextEligibleAt > schedule.NextEligibleAt ? global.NextEligibleAt : schedule.NextEligibleAt;
            }
            if (result.Json is not null) db.Snapshots.Add(new() { Kind = target.Kind, Tag = target.Tag, ObservedAt = now, Json = result.Json });
            else failures++;
            await db.SaveChangesAsync(ct);
            if (result.RetryAfter is { } delay) { status.Message = "Rate limited · collection deferred"; return delay > TimeSpan.FromMinutes(settings.IntervalMinutes) ? delay : TimeSpan.FromMinutes(settings.IntervalMinutes); }
            await Task.Delay(settings.RequestSpacingMilliseconds, ct);
        }
        status.Message = failures == 0 ? "Collection complete" : "Collection completed with unavailable data";
        return null;
    }
}
