using ClashInsights.Api;
using Microsoft.Extensions.Configuration;
using System.Net;
namespace ClashInsights.Tests;
public class BehaviourTests {
    [Theory] [InlineData(" p0y28 ","#P0Y28")] [InlineData("#Q0Y28","#Q0Y28")]
    public void NormalizesTags(string input,string expected) => Assert.Equal(expected,Tags.Normalize(input));
    [Theory] [InlineData("bad/url")] [InlineData("")] [InlineData("#ABC")]
    public void RejectsInvalidTags(string tag) => Assert.False(Tags.IsValid(tag));
    [Fact] public void ResetIsNotNegativeDonationActivity() => Assert.Null(Dashboard.DonationDelta(500,10));
    [Fact] public async Task MissingCredentialsNeverMakeNetworkCall() {
        var handler=new Stub(HttpStatusCode.OK);
        var client=new ClashClient(new HttpClient(handler),new ConfigurationBuilder().Build());
        Assert.Equal("Not configured",(await client.Fetch("players/test",default)).Status);
        Assert.Equal(0,handler.Calls);
    }
    [Fact] public async Task RateLimitPreservesRetryAfter() {
        var handler=new Stub(HttpStatusCode.TooManyRequests);
        var client=new ClashClient(new HttpClient(handler){BaseAddress=new Uri("https://example.test/")},new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"Clash:ApiToken","fixture-token"}}).Build());
        var result=await client.Fetch("players/test",default);
        Assert.Equal("Rate limited",result.Status); Assert.Null(result.Json); Assert.Equal(TimeSpan.FromMinutes(5),result.RetryAfter);
    }
    [Fact] public void UnavailableWarDoesNotInventOpponent() {
        var view=Dashboard.Build([new(){Kind="war",Tag="#P0Y28",Json="{\"state\":\"notInWar\"}",ObservedAt=DateTimeOffset.UtcNow}],[],new(),"test");
        Assert.Equal("Unavailable",view.Wars[0].Opponent);
    }
    [Fact] public void RosterDifferenceUsesObservedSnapshots() {
        var view=Dashboard.Build([new(){Kind="clan",Tag="#P0Y28",ObservedAt=DateTimeOffset.UtcNow.AddDays(-1),Json="{\"memberList\":[{\"tag\":\"#P0Y29\",\"name\":\"Old\"}]}"},new(){Kind="clan",Tag="#P0Y28",ObservedAt=DateTimeOffset.UtcNow,Json="{\"memberList\":[{\"tag\":\"#P0Y2L\",\"name\":\"New\"}]}"}],[],new(),"test");
        Assert.Equal(["New"],view.Clans[0].Joined); Assert.Equal(["Old"],view.Clans[0].Left);
    }
    [Fact] public void RichClanAndPlayerFieldsPreserveUnavailableValues() {
        var at=DateTimeOffset.UtcNow;
        var view=Dashboard.Build([
            new(){Kind="player",Tag="#P0Y28",ObservedAt=at,Json="{\"name\":\"Player\",\"leagueTier\":{\"name\":\"Legend League\"},\"bestTrophies\":6000}"},
            new(){Kind="clan",Tag="#P0Y29",ObservedAt=at,Json="{\"name\":\"Clan\",\"isWarLogPublic\":false,\"warWins\":12,\"warLeague\":{\"name\":\"Master League I\"},\"memberList\":[{\"tag\":\"#P0Y2L\",\"name\":\"Member\",\"role\":\"leader\",\"clanRank\":1}]}"}
        ],[],new(),"test");
        Assert.Equal("Legend League",view.Players.Single().League);
        Assert.Equal(6000,view.Players.Single().BestTrophies);
        Assert.Null(view.Players.Single().WarStars);
        Assert.False(view.Clans.Single().WarLogPublic);
        Assert.Equal("Master League I",view.Clans.Single().WarLeague);
        Assert.Null(view.Clans.Single().Members.Single().Donations);
    }
    private sealed class Stub(HttpStatusCode code):HttpMessageHandler {
        public int Calls {get;private set;}
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct) { Calls++;return Task.FromResult(new HttpResponseMessage(code)); }
    }
}
