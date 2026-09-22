using ClashInsights.Api;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Net;
using System.Text.Json;
namespace ClashInsights.Tests;
public class DatabaseTests {
 [Fact] public async Task PersistentHistoryAndCollectorRecovery() {
  var connection=Environment.GetEnvironmentVariable("CLASH_TEST_CONNECTION");
  Assert.False(string.IsNullOrEmpty(connection),"Run scripts/Test-Local.ps1 to supply the isolated test database connection.");
  var name="clash_test_"+Guid.NewGuid().ToString("N");
  var admin=new NpgsqlConnectionStringBuilder(connection){Database="postgres"};
  await using var control=new NpgsqlConnection(admin.ConnectionString);await control.OpenAsync();
  await using(var create=new NpgsqlCommand("CREATE DATABASE "+name,control))await create.ExecuteNonQueryAsync();
  var testConnection=new NpgsqlConnectionStringBuilder(connection){Database=name}.ConnectionString;
  try {
   var dbOptions=new DbContextOptionsBuilder<InsightDb>().UseNpgsql(testConnection).Options;
   await using(var db=new InsightDb(dbOptions)) {
    await db.Database.MigrateAsync();
    var now=DateTimeOffset.UtcNow;
    // More than the previous shared cap, with an older player that must remain visible.
    db.Snapshots.Add(new(){Source="Live",Kind="player",Tag="#P0Y28",ObservedAt=now.AddDays(-80),Json="{\"name\":\"Older player\",\"trophies\":120}"});
    for(int i=0;i<30240;i++)db.Snapshots.Add(new(){Source="Live",Kind="player",Tag="#P0Y29",ObservedAt=now.AddSeconds(-i),Json="{\"name\":\"Recent player\",\"trophies\":200}"});
    db.Snapshots.Add(new(){Source="Live",Kind="player",Tag="#P0Y2L",ObservedAt=now.AddDays(-100),Json="{\"name\":\"Expired\"}"});
    await db.SaveChangesAsync();
    var summary=await Queries.Summary(db,new(){Mode="Live"},"test",default);
    Assert.Contains(summary.Players,p=>p.Tag=="#P0Y28");
    Assert.Single((await Queries.History(db,"Live","#P0Y28",90,default))!);
    Assert.Empty((await Queries.History(db,"Live","#P0Y28",7,default))!);
    Assert.Equal(30240,(await Queries.History(db,"Live","#P0Y29",90,default))!.Length);
    var publicSummary=await Queries.Summary(db,new(){Mode="Live"},"public",default,["#P0Y29"],includeAttempts:false);
    Assert.DoesNotContain(publicSummary.Players,player=>player.Tag=="#P0Y28");
    Assert.Contains(publicSummary.Players,player=>player.Tag=="#P0Y29");
    Assert.Empty(publicSummary.Attempts);
    Assert.Null(await Queries.History(db,"Live","#P0Y28",90,default,["#P0Y29"]));
   }
   // PostgreSQL ownership spans independent connections.
   await using(var a=new NpgsqlConnection(testConnection))await using(var b=new NpgsqlConnection(testConnection)) {
    await a.OpenAsync();await b.OpenAsync();
    await using var ca=new NpgsqlCommand("SELECT pg_try_advisory_lock(739125)",a);
    await using var cb=new NpgsqlCommand("SELECT pg_try_advisory_lock(739125)",b);
    Assert.Equal(true,await ca.ExecuteScalarAsync());Assert.Equal(false,await cb.ExecuteScalarAsync());
    ca.CommandText="SELECT pg_advisory_unlock(739125)";await ca.ExecuteScalarAsync();
   }
   var handler=new ThrottledHandler();
   await RunCollector(testConnection,handler,false);
   Assert.Equal(1,handler.Calls);
   await RunCollector(testConnection,handler,true);
   Assert.Equal(1,handler.Calls); // restart respects database-wide Retry-After
   await using(var db=new InsightDb(dbOptions)) {
    Assert.False(await db.Snapshots.AnyAsync(s=>s.Tag=="#P0Y2L"));
    Assert.True(await db.Snapshots.AnyAsync(s=>s.Tag=="#P0Y28"));
    Assert.Equal("Rate limited",(await db.Attempts.SingleAsync()).Status);
    Assert.True((await db.Schedules.FindAsync("global"))!.NextEligibleAt>DateTimeOffset.UtcNow.AddMinutes(20));
   }
  } finally {
   NpgsqlConnection.ClearAllPools();
   await using var drop=new NpgsqlCommand("DROP DATABASE "+name+" WITH (FORCE)",control);await drop.ExecuteNonQueryAsync();
  }
 }
 private static async Task RunCollector(string connection,ThrottledHandler handler,bool restarted) {
  var services=new ServiceCollection();services.AddLogging();
  services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"Clash:ApiToken","test-only"}}).Build());
  services.AddDbContext<InsightDb>(o=>o.UseNpgsql(connection));
  services.AddTransient(p=>new ClashClient(new HttpClient(handler,false){BaseAddress=new Uri("https://fixture.invalid/")},p.GetRequiredService<IConfiguration>()));
  using var provider=services.BuildServiceProvider();var status=new CollectorStatus();
  using var collector=new Collector(provider.GetRequiredService<IServiceScopeFactory>(),Options.Create(new TrackingOptions{Mode="Live",PlayerTags=["#P0Y28"]}),status,provider.GetRequiredService<ILogger<Collector>>());
  await collector.StartAsync(default);
  using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(15));
  while(!(restarted?status.Message.Contains("persisted"):status.Message.Contains("deferred")))await Task.Delay(30,timeout.Token);
  await collector.StopAsync(default);
 }
 private sealed class ThrottledHandler:HttpMessageHandler {
  public int Calls;
  protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct){Calls++;var r=new HttpResponseMessage(HttpStatusCode.TooManyRequests);r.Headers.RetryAfter=new(TimeSpan.FromMinutes(30));return Task.FromResult(r);}
 }
 [Fact]public void MissingRosterCannotInventDepartures(){
  var rows=new List<Snapshot>{new(){Kind="clan",Tag="#P0Y28",ObservedAt=DateTimeOffset.UtcNow.AddHours(-1),Json="{\"name\":\"Clan\",\"memberList\":[{\"tag\":\"#P0Y29\",\"name\":\"Member\"}]}"},new(){Kind="clan",Tag="#P0Y28",ObservedAt=DateTimeOffset.UtcNow,Json="{\"name\":\"Clan\"}"}};
  var clan=Dashboard.Build(rows,[],new(),"").Clans.Single();Assert.Empty(clan.Left);Assert.Null(clan.Points);
 }
 [Fact] public void CapacityIsBounded(){Assert.False(new TrackingOptions{PlayerTags=Enumerable.Repeat("#P0Y28",11).ToArray()}.IsValid());}
 [Fact] public void PublicTagsMustBeTracked(){Assert.False(new TrackingOptions{ClanTags=["#P0Y28"],PublicClanTags=["#P0Y29"]}.IsValid());}
 [Theory][InlineData("[]")][InlineData("{\"error\":\"bad\"}")][InlineData("{\"name\":\"Clan\",\"memberList\":null}")]
 public void MalformedUpstreamIsRejected(string json){using var doc=JsonDocument.Parse(json);Assert.False(ClashClient.ValidPayload(doc.RootElement,"clans/test"));}
}
