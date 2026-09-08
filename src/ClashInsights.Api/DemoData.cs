using System.Text.Json;
using Microsoft.EntityFrameworkCore;
namespace ClashInsights.Api;
public static class DemoData {
    public static async Task Seed(InsightDb db) {
        if (await db.Snapshots.AnyAsync(x => x.Source == "Demo")) return;
        var names = new[] { "Zach", "Ember", "Atlas", "Nova", "Scout", "Oak" };
        var tags = new[] { "#P0Y28", "#P0Y29", "#P0Y2L", "#P0Y2Q", "#P0Y2G", "#P0Y2R" };
        var now = DateTimeOffset.UtcNow;
        for (var day = 0; day < 336; day++) {
            var at = now.AddHours(day-335);
            if (day is >= 180 and <= 185) continue;
            for (var i=0; i<names.Length; i++) db.Snapshots.Add(new() { Source="Demo", Kind="player", Tag=tags[i], ObservedAt=at,
                Json=JsonSerializer.Serialize(new { tag=tags[i],name=names[i],townHallLevel=16-i/2,trophies=4200-i*310+day*(14-i),donations=100+i*42+day*37,donationsReceived=120+day*8 }) });
            var count = day < 335 ? 5 : 6;
            db.Snapshots.Add(new() { Source="Demo", Kind="clan",Tag="#Q0Y28",ObservedAt=at,Json=JsonSerializer.Serialize(new {
                tag="#Q0Y28", name="The Night Watch",clanLevel=21,clanPoints=38240+day*80,
                memberList=Enumerable.Range(0,count).Select(i => new { tag=tags[i],name=names[i],role=i==0?"leader":"member",trophies=4200-i*310+day*(14-i),donations=100+i*42+day*37 }) }) });
        }
        db.Snapshots.Add(new() { Source="Demo",Kind="war",Tag="#Q0Y28",ObservedAt=now,Json=JsonSerializer.Serialize(new { state="inWar",teamSize=15,clan=new { stars=32,attacks=14 },opponent=new { name="Iron Wolves",stars=28 } }) });
        db.Attempts.Add(new() { Source="Demo",Kind="fixture",Tag="Demo",At=now,Status="Synthetic sample data seeded" });
        await db.SaveChangesAsync();
    }
}
