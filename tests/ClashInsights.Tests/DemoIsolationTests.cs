using ClashInsights.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ClashInsights.Tests;

public sealed class DemoIsolationTests {
    [Fact]
    public async Task DemoCollectorCompletesWithoutResolvingDatabaseOrUpstreamServices() {
        // No database or HTTP client is registered: resolving either must fail.
        using var services = new ServiceCollection().BuildServiceProvider();
        var status = new CollectorStatus();
        using var collector = new Collector(services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new TrackingOptions { Mode = "Demo", PlayerTags = ["#P0Y28"], ClanTags = ["#Q0Y28"] }),
            status, NullLogger<Collector>.Instance);
        await collector.StartAsync(CancellationToken.None);
        await collector.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("Demo mode · no Supercell requests", status.Message);
        await collector.StopAsync(CancellationToken.None);
    }
}
