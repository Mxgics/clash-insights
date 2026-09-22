using System.Net;
using System.Security.Claims;
using ClashInsights.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ClashInsights.Tests;

public sealed class SecurityPipelineTests : IClassFixture<LocalFactory> {
    private readonly HttpClient client;
    public SecurityPipelineTests(LocalFactory factory) => client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Fact]
    public async Task PrivateApiRejectsAnonymousRequests() {
        using var response = await client.GetAsync("/api/private/dashboard");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(response.Headers.Location is not null);
    }

    [Fact]
    public async Task LivenessCarriesBrowserSecurityHeaders() {
        using var response = await client.GetAsync("/health/live");
        response.EnsureSuccessStatusCode();
        Assert.Contains("frame-ancestors 'none'", response.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
    }

    [Fact]
    public void AuthAllowlistIsDenyByDefault() {
        var options = new AuthOptions { Enabled = true, Domain = "tenant.auth0.com", ClientId = "id", ClientSecret = "secret", AllowedSubjects = ["auth0|allowed"] };
        var allowed = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "auth0|allowed")], "test"));
        var denied = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "auth0|other")], "test"));
        Assert.True(options.Allows(allowed));
        Assert.False(options.Allows(denied));
    }
}

public sealed class LocalFactory : WebApplicationFactory<Program> {
    protected override void ConfigureWebHost(IWebHostBuilder builder) {
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.UseContentRoot(Path.GetFullPath("../../../../../src/ClashInsights.Api", AppContext.BaseDirectory));
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?> {
            ["ConnectionStrings:Clash"] = "Host=fixture.invalid;Database=fixture;Username=fixture;Password=fixture",
            ["Database:MigrateOnStartup"] = "false",
            ["Hosting:Hosted"] = "false",
            ["Auth:Enabled"] = "false",
            ["Tracking:Mode"] = "Demo"
        }));
    }
}
