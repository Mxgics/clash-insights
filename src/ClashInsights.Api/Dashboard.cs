using System.Text.Json;
namespace ClashInsights.Api;
public sealed record HistoryPoint(DateTimeOffset At, int? Trophies, int? Donations);
public sealed record PlayerView(string Tag, string Name, int? TownHall, int? Trophies, int? Donations, int? Received, int? TrophyChange, DateTimeOffset ObservedAt, HistoryPoint[] History, string? League, int? BestTrophies, int? BuilderBaseTrophies, int? WarStars);
public sealed record MemberView(string Tag, string Name, string Role, int? Trophies, int? Donations, int? TownHall, int? Received, string? League, int? Rank);
public sealed record ClanView(string Tag, string Name, int? Level, int? Points, DateTimeOffset ObservedAt, MemberView[] Members, string[] Joined, string[] Left, bool RosterAvailable, bool RosterComparisonAvailable, bool? WarLogPublic, int? WarWins, int? WarWinStreak, string? WarLeague);
public sealed record WarView(string Tag, string State, string Opponent, int? Stars, int? OpponentStars, int? Attacks, int? TeamSize, DateTimeOffset ObservedAt);
public sealed record DashboardView(string Mode, string CollectorStatus, int IntervalMinutes, int RetentionDays, PlayerView[] Players, ClanView[] Clans, WarView[] Wars, Attempt[] Attempts);
public static class Dashboard {
    public static DashboardView Build(List<Snapshot> snapshots, List<Attempt> attempts, TrackingOptions settings, string status) {
        var players = snapshots.Where(x => x.Kind == "player").GroupBy(x => x.Tag).Select(group => {
            var rows = group.OrderBy(x => x.ObservedAt).ToArray();
            var latest = rows[^1]; var p = JsonSerializer.Deserialize<JsonElement>(latest.Json);
            var history = rows.Select(x => { var j = JsonSerializer.Deserialize<JsonElement>(x.Json); return new HistoryPoint(x.ObservedAt, Number(j,"trophies"), Number(j,"donations")); }).ToArray();
            return new PlayerView(latest.Tag, Text(p,"name"), Number(p,"townHallLevel"), Number(p,"trophies"), Number(p,"donations"), Number(p,"donationsReceived"), history.Length > 1 ? history[^1].Trophies-history[0].Trophies : null, latest.ObservedAt, history, League(p), Number(p,"bestTrophies"), Number(p,"builderBaseTrophies"), Number(p,"warStars"));
        }).OrderByDescending(x => x.Trophies).ToArray();
        var clans = snapshots.Where(x => x.Kind == "clan").GroupBy(x => x.Tag).Select(group => {
            var rows = group.OrderByDescending(x => x.ObservedAt).ToArray(); var latest = rows[0];
            var j = JsonSerializer.Deserialize<JsonElement>(latest.Json); var members = Members(j);
            var previousJson = rows.Length > 1 ? JsonSerializer.Deserialize<JsonElement>(rows[1].Json) : j;
            var comparable = rows.Length > 1 && Child(j,"memberList").ValueKind == JsonValueKind.Array && Child(previousJson,"memberList").ValueKind == JsonValueKind.Array;
            var previous = comparable ? Members(previousJson) : members;
            return new ClanView(latest.Tag, Text(j,"name"), Number(j,"clanLevel"), Number(j,"clanPoints"), latest.ObservedAt, members,
                members.Where(m => previous.All(p => p.Tag != m.Tag)).Select(m => m.Name).ToArray(),
                previous.Where(m => members.All(p => p.Tag != m.Tag)).Select(m => m.Name).ToArray(), Child(j,"memberList").ValueKind == JsonValueKind.Array, comparable, Boolean(j,"isWarLogPublic"), Number(j,"warWins"), Number(j,"warWinStreak"), OptionalText(Child(j,"warLeague"),"name"));
        }).ToArray();
        var wars = snapshots.Where(x => x.Kind == "war").GroupBy(x => x.Tag).Select(g => {
            var latest = g.MaxBy(x => x.ObservedAt)!; var j = JsonSerializer.Deserialize<JsonElement>(latest.Json);
            var clan = Child(j,"clan"); var opponent = Child(j,"opponent");
            return new WarView(latest.Tag, Text(j,"state"), Text(opponent,"name"), Number(clan,"stars"), Number(opponent,"stars"), Number(clan,"attacks"), Number(j,"teamSize"), latest.ObservedAt);
        }).ToArray();
        return new(settings.Mode,status,settings.IntervalMinutes,settings.RetentionDays,players,clans,wars,attempts.ToArray());
    }
    public static int? DonationDelta(int before, int after) => after < before ? null : after - before;
    private static MemberView[] Members(JsonElement j) => j.TryGetProperty("memberList", out var members) && members.ValueKind == JsonValueKind.Array
        ? members.EnumerateArray().Select(m => new MemberView(Text(m,"tag"), Text(m,"name"), Text(m,"role"), Number(m,"trophies"), Number(m,"donations"), Number(m,"townHallLevel"), Number(m,"donationsReceived"), League(m), Number(m,"clanRank"))).ToArray() : [];
    private static string? League(JsonElement j) => OptionalText(Child(j,"leagueTier"),"name") ?? OptionalText(Child(j,"league"),"name");
    private static string? OptionalText(JsonElement j, string key) { var p=Child(j,key); return p.ValueKind == JsonValueKind.String ? p.GetString() : null; }
    private static bool? Boolean(JsonElement j, string key) => Child(j,key).ValueKind switch { JsonValueKind.True => true, JsonValueKind.False => false, _ => null };
    private static JsonElement Child(JsonElement j, string key) => j.ValueKind == JsonValueKind.Object && j.TryGetProperty(key,out var p) ? p : default;
    private static string Text(JsonElement j, string key) { var p=Child(j,key); return p.ValueKind == JsonValueKind.String ? p.GetString()! : "Unavailable"; }
    private static int? Number(JsonElement j,string key) { var p=Child(j,key); return p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out var n) ? n : null; }
}
