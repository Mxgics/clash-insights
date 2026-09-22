using ClashInsights.Api;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Net;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
var migrateOnly = args.Contains("--migrate-only", StringComparer.Ordinal);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);

var hosted = builder.Configuration.GetValue<bool>("Hosting:Hosted");
var authSettings = builder.Configuration.GetSection("Auth").Get<AuthOptions>() ?? new();
var sentryDsn = builder.Configuration["Sentry:Dsn"];
if (!string.IsNullOrWhiteSpace(sentryDsn)) {
    builder.WebHost.UseSentry(options => {
        options.Dsn = sentryDsn;
        options.Environment = builder.Environment.EnvironmentName;
        options.Release = builder.Configuration["Sentry:Release"];
        options.SendDefaultPii = false;
        options.TracesSampleRate = builder.Configuration.GetValue("Sentry:TracesSampleRate", 0.05);
        options.MaxRequestBodySize = Sentry.Extensibility.RequestSize.None;
    });
}

builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize =
    builder.Configuration.GetValue<long?>("Hosting:MaxRequestBodyBytes") ?? 1_048_576);
builder.Services.AddProblemDetails();
builder.Services.AddAntiforgery(options => {
    options.Cookie.Name = "__Host-clash-xsrf";
    options.Cookie.HttpOnly = false;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = hosted ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
    options.HeaderName = "X-XSRF-TOKEN";
});
builder.Services.AddRateLimiter(options => {
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("auth", limiter => {
        limiter.PermitLimit = 10;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
        limiter.AutoReplenishment = true;
    });
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions {
                PermitLimit = 240,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

builder.Services.AddOptions<TrackingOptions>().BindConfiguration("Tracking")
    .Validate(options => options.IsValid(), "Invalid tracking mode, tags, public allowlists, interval or retention.")
    .Validate(options => !hosted || options.Mode == "Live", "Hosted operation must use Live tracking mode.")
    .Validate(options => !hosted || options.PublicClanTags.Select(Tags.Normalize).Distinct().Count() == 3,
        "Hosted operation requires exactly three distinct public clan tags.")
    .ValidateOnStart();
builder.Services.AddOptions<HostingOptions>().BindConfiguration("Hosting")
    .Validate(options => options.IsValid(), "Hosted deployments require explicit trusted proxy IPs and a bounded request size.")
    .ValidateOnStart();
builder.Services.AddOptions<AuthOptions>().BindConfiguration("Auth")
    .Validate(options => options.IsValid(hosted), "Hosted deployments require Auth0 HTTPS settings and an explicit allowed-user list.")
    .ValidateOnStart();
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection("Database"));

builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = authSettings.Enabled ? OpenIdConnectDefaults.AuthenticationScheme : CookieAuthenticationDefaults.AuthenticationScheme;
}).AddCookie(options => {
    options.Cookie.Name = "__Host-clash-session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = hosted ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
    options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
});
if (authSettings.Enabled) {
    builder.Services.AddAuthentication().AddOpenIdConnect(options => {
        options.Authority = authSettings.NormalizedAuthority;
        options.ClientId = authSettings.ClientId;
        options.ClientSecret = authSettings.ClientSecret;
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.UsePkce = true;
        options.CallbackPath = "/auth/callback";
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.SaveTokens = false;
        options.MapInboundClaims = false;
        options.TokenValidationParameters.NameClaimType = "name";
    });
}
builder.Services.AddAuthorization(options => options.AddPolicy("PrivateWorkspace", policy =>
    policy.RequireAuthenticatedUser().RequireAssertion(context => authSettings.Allows(context.User))));

var dataProtection = builder.Services.AddDataProtection().SetApplicationName("ClashInsights");
var dataProtectionPath = builder.Configuration["Hosting:DataProtectionPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionPath)) dataProtection.PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath));

builder.Services.AddDbContext<InsightDb>((services, options) => {
    var connection = services.GetRequiredService<IConfiguration>().GetConnectionString("Clash")
        ?? throw new InvalidOperationException("Configure ConnectionStrings__Clash. See docs/runbooks/v1-local.md.");
    options.UseNpgsql(connection);
});
builder.Services.AddHttpClient<ClashClient>(client => {
    client.BaseAddress = new Uri("https://api.clashofclans.com/v1/");
    client.Timeout = TimeSpan.FromSeconds(20);
});
builder.Services.AddSingleton<CollectorStatus>();
builder.Services.AddHostedService<Collector>();

var app = builder.Build();

if (hosted) {
    var forwarding = new ForwardedHeadersOptions {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
        ForwardLimit = 1
    };
    foreach (var proxy in app.Services.GetRequiredService<IOptions<HostingOptions>>().Value.KnownProxies)
        forwarding.KnownProxies.Add(IPAddress.Parse(proxy));
    app.UseForwardedHeaders(forwarding);
    app.UseExceptionHandler();
    app.UseHsts();
    app.UseHttpsRedirection();
} else {
    app.UseExceptionHandler();
    app.Use(async (context, next) => {
        var ip = context.Connection.RemoteIpAddress;
        if (ip is not null && !IPAddress.IsLoopback(ip)) { context.Response.StatusCode = StatusCodes.Status403Forbidden; return; }
        await next();
    });
}

app.Use(async (context, next) => {
    context.Response.OnStarting(() => {
        var headers = context.Response.Headers;
        headers.ContentSecurityPolicy = "default-src 'self'; base-uri 'self'; object-src 'none'; frame-ancestors 'none'; form-action 'self'; img-src 'self' data:; font-src 'self'; style-src 'self' 'unsafe-inline'; script-src 'self'; connect-src 'self' https://*.ingest.sentry.io";
        headers.XContentTypeOptions = "nosniff";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=(), usb=()";
        headers["X-Frame-Options"] = "DENY";
        if (context.Request.Path.StartsWithSegments("/api/private") || context.User.Identity?.IsAuthenticated == true) {
            headers.CacheControl = "private, no-store";
            headers["X-Robots-Tag"] = "noindex, nofollow";
        }
        return Task.CompletedTask;
    });
    await next();
});
app.UseStaticFiles();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope()) {
    var database = scope.ServiceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
    if (database.MigrateOnStartup || migrateOnly) {
        var db = scope.ServiceProvider.GetRequiredService<InsightDb>();
        await db.Database.MigrateAsync();
        if (scope.ServiceProvider.GetRequiredService<IOptions<TrackingOptions>>().Value.Mode == "Demo") await DemoData.Seed(db);
    }
}
if (migrateOnly) return;

static bool InvalidHistoryRequest(string tag, int days) => !Tags.IsValid(tag) || days is not (7 or 14 or 90);
static string SafeReturnUrl(string? value) => !string.IsNullOrWhiteSpace(value) && value.StartsWith('/') && !value.StartsWith("//") ? value : "/";

var publicApi = app.MapGroup("/api/public").AllowAnonymous();
publicApi.MapGet("/dashboard", async (InsightDb db, IOptions<TrackingOptions> options, CancellationToken ct) => {
    var settings = options.Value;
    var tags = !hosted || settings.Mode == "Demo" ? null : settings.PublicPlayerTags.Concat(settings.PublicClanTags).ToArray();
    var status = settings.Mode == "Demo" ? "Demo mode · no Supercell requests" : "Public observations";
    return Results.Ok(await Queries.Summary(db, settings, status, ct, tags, includeAttempts: false));
});
publicApi.MapGet("/players/{tag}/history", async (string tag, int days, InsightDb db, IOptions<TrackingOptions> options, CancellationToken ct) => {
    if (InvalidHistoryRequest(tag, days)) return Results.BadRequest(new { error = "Use a valid player tag and days=7, 14 or 90." });
    var settings = options.Value;
    var allowed = !hosted || settings.Mode == "Demo" ? null : settings.PublicPlayerTags;
    var history = await Queries.History(db, settings.Mode, tag, days, ct, allowed);
    return history is null ? Results.NotFound() : Results.Ok(history);
});

var privateApi = app.MapGroup("/api/private").RequireAuthorization("PrivateWorkspace");
privateApi.MapGet("/dashboard", async (InsightDb db, IOptions<TrackingOptions> options, CollectorStatus status, CancellationToken ct) =>
    Results.Ok(await Queries.Summary(db, options.Value, status.Message, ct)));
privateApi.MapGet("/players/{tag}/history", async (string tag, int days, InsightDb db, IOptions<TrackingOptions> options, CancellationToken ct) => {
    if (InvalidHistoryRequest(tag, days)) return Results.BadRequest(new { error = "Use a valid player tag and days=7, 14 or 90." });
    return Results.Ok(await Queries.History(db, options.Value.Mode, tag, days, ct));
});

if (!hosted) {
    app.MapGet("/api/dashboard", async (InsightDb db, IOptions<TrackingOptions> options, CollectorStatus status, CancellationToken ct) =>
        Results.Ok(await Queries.Summary(db, options.Value, status.Message, ct)));
    app.MapGet("/api/players/{tag}/history", async (string tag, int days, InsightDb db, IOptions<TrackingOptions> options, CancellationToken ct) => {
        if (InvalidHistoryRequest(tag, days)) return Results.BadRequest(new { error = "Use a valid player tag and days=7, 14 or 90." });
        return Results.Ok(await Queries.History(db, options.Value.Mode, tag, days, ct));
    });
}

app.MapGet("/api/session", (HttpContext context, IAntiforgery antiforgery) => {
    antiforgery.GetAndStoreTokens(context);
    return Results.Ok(new SessionView(context.User.Identity?.IsAuthenticated == true && authSettings.Allows(context.User), context.User.Identity?.Name, authSettings.Enabled));
}).AllowAnonymous();
app.MapGet("/api/client-config", () => Results.Ok(new ClientConfig(
    builder.Configuration["Sentry:BrowserDsn"], builder.Environment.EnvironmentName,
    builder.Configuration["Sentry:Release"], builder.Configuration.GetValue("Sentry:BrowserTracesSampleRate", 0.02)))).AllowAnonymous();
if (authSettings.Enabled) {
    app.MapGet("/auth/login", (string? returnUrl) => Results.Challenge(
        new AuthenticationProperties { RedirectUri = SafeReturnUrl(returnUrl) },
        [OpenIdConnectDefaults.AuthenticationScheme])).RequireRateLimiting("auth").AllowAnonymous();
    app.MapPost("/auth/logout", async (HttpContext context, IAntiforgery antiforgery) => {
        await antiforgery.ValidateRequestAsync(context);
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties { RedirectUri = "/" });
        return Results.Empty;
    }).RequireAuthorization("PrivateWorkspace").RequireRateLimiting("auth");
}

app.MapGet("/health/live", () => Results.Ok(new { status = "healthy" })).AllowAnonymous();
app.MapGet("/health/ready", async (InsightDb db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct) ? Results.Ok(new { status = "healthy" }) : Results.StatusCode(503)).AllowAnonymous();
app.MapGet("/health", async (InsightDb db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct) ? Results.Ok(new { status = "healthy" }) : Results.StatusCode(503)).AllowAnonymous();
app.MapFallback("/api/{**path}", () => Results.NotFound());
app.MapFallbackToFile("index.html");
app.Run();

public partial class Program { }
