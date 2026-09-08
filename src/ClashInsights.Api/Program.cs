using ClashInsights.Api;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);
builder.Services.AddProblemDetails();
builder.Services.AddOptions<TrackingOptions>().BindConfiguration("Tracking")
    .Validate(o => o.IsValid(), "Invalid tracking mode, tags, interval or retention.").ValidateOnStart();
var connection = builder.Configuration.GetConnectionString("Clash")
    ?? throw new InvalidOperationException("Configure ConnectionStrings__Clash. See docs/runbooks/v1-local.md.");
builder.Services.AddDbContext<InsightDb>(o => o.UseNpgsql(connection));
builder.Services.AddHttpClient<ClashClient>(c => { c.BaseAddress = new Uri("https://api.clashofclans.com/v1/"); c.Timeout = TimeSpan.FromSeconds(20); });
builder.Services.AddSingleton<CollectorStatus>();
builder.Services.AddHostedService<Collector>();
var app = builder.Build();
app.UseExceptionHandler();
app.Use(async (context, next) => {
    var ip = context.Connection.RemoteIpAddress;
    if (ip is not null && !IPAddress.IsLoopback(ip)) { context.Response.StatusCode = 403; return; }
    await next();
});
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<InsightDb>();
    await db.Database.MigrateAsync();
    if (app.Services.GetRequiredService<IOptions<TrackingOptions>>().Value.Mode == "Demo") await DemoData.Seed(db);
}
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapGet("/api/dashboard", async (InsightDb db, IOptions<TrackingOptions> options, CollectorStatus status, CancellationToken ct) => {
    return Results.Ok(await Queries.Summary(db, options.Value, status.Message, ct));
});
app.MapGet("/api/players/{tag}/history", async (string tag, int days, InsightDb db, IOptions<TrackingOptions> options, CancellationToken ct) => {
    if (!Tags.IsValid(tag) || days is not (7 or 14 or 90)) return Results.BadRequest(new { error = "Use a valid player tag and days=7, 14 or 90." });
    return Results.Ok(await Queries.History(db,options.Value.Mode,Tags.Normalize(tag),days,ct));
});
app.MapGet("/health", async (InsightDb db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct) ? Results.Ok(new { status = "healthy" }) : Results.StatusCode(503));
app.MapFallback("/api/{**path}", () => Results.NotFound());
app.MapFallbackToFile("index.html");
app.Run();
public partial class Program { }
