using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
namespace ClashInsights.Api;
public static class Queries {
 public static async Task<DashboardView> Summary(InsightDb db, TrackingOptions options, string status, CancellationToken ct) {
  var source=db.Snapshots.AsNoTracking().Where(s=>s.Source==options.Mode);
  var keys=await source.Select(s=>new {s.Kind,s.Tag}).Distinct().ToListAsync(ct);
  var rows=new List<Snapshot>();
  foreach(var key in keys) rows.AddRange(await source.Where(s=>s.Kind==key.Kind && s.Tag==key.Tag).OrderByDescending(s=>s.ObservedAt).Take(key.Kind=="clan"?2:1).ToListAsync(ct));
  var attempts=await db.Attempts.AsNoTracking().Where(s=>s.Source==options.Mode).OrderByDescending(s=>s.At).Take(25).ToListAsync(ct);
  return Dashboard.Build(rows,attempts,options,status);
 }
 public static async Task<HistoryPoint[]> History(InsightDb db,string mode,string tag,int days,CancellationToken ct) {
  var cutoff=DateTimeOffset.UtcNow.AddDays(-days);
  var rows=await db.Snapshots.AsNoTracking().Where(s=>s.Source==mode && s.Kind=="player" && s.Tag==tag && s.ObservedAt>=cutoff).OrderBy(s=>s.ObservedAt).ToListAsync(ct);
  return Dashboard.Build(rows,[],new(){Mode=mode},"").Players.FirstOrDefault()?.History ?? [];
 }
}
