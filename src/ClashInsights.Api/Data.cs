using Microsoft.EntityFrameworkCore;
namespace ClashInsights.Api;
public sealed class InsightDb(DbContextOptions<InsightDb> options) : DbContext(options) {
    public DbSet<Snapshot> Snapshots => Set<Snapshot>();
    public DbSet<Attempt> Attempts => Set<Attempt>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    protected override void OnModelCreating(ModelBuilder model) {
        model.Entity<Snapshot>().HasIndex(x => new { x.Source, x.Kind, x.Tag, x.ObservedAt }).IsUnique();
        model.Entity<Snapshot>().Property(x => x.Json).HasColumnType("jsonb");
        model.Entity<Attempt>().HasIndex(x => new { x.Source, x.At });
    }
}
public sealed class Snapshot {
    public long Id { get; set; }
    public string Source { get; set; } = "Live";
    public string Kind { get; set; } = "player";
    public string Tag { get; set; } = "";
    public DateTimeOffset ObservedAt { get; set; }
    public string Json { get; set; } = "{}";
}
public sealed class Attempt {
    public long Id { get; set; }
    public string Source { get; set; } = "Live";
    public string Kind { get; set; } = "";
    public string Tag { get; set; } = "";
    public DateTimeOffset At { get; set; }
    public string Status { get; set; } = "";
}
public sealed class TrackingOptions {
    public string Mode { get; set; } = "Demo";
    public string[] PlayerTags { get; set; } = [];
    public string[] ClanTags { get; set; } = [];
    public string[] PublicPlayerTags { get; set; } = [];
    public string[] PublicClanTags { get; set; } = [];
    public int IntervalMinutes { get; set; } = 60;
    public int RetentionDays { get; set; } = 90;
    public int RequestSpacingMilliseconds { get; set; } = 1000;
    public bool IsValid() => (Mode is "Demo" or "Live") && IntervalMinutes is >= 1 and <= 1440
        && RetentionDays is >= 1 and <= 3650 && RequestSpacingMilliseconds is >= 250 and <= 60000
        && PlayerTags.Length <= 10 && ClanTags.Length <= 10
        && PlayerTags.Concat(ClanTags).Concat(PublicPlayerTags).Concat(PublicClanTags).All(Tags.IsValid)
        && PublicPlayerTags.Select(Tags.Normalize).Except(PlayerTags.Select(Tags.Normalize)).Any() == false
        && PublicClanTags.Select(Tags.Normalize).Except(ClanTags.Select(Tags.Normalize)).Any() == false;
}
public static class Tags {
    public static string Normalize(string tag) => "#" + tag.Trim().TrimStart('#').ToUpperInvariant();
    public static bool IsValid(string tag) => !string.IsNullOrWhiteSpace(tag)
        && System.Text.RegularExpressions.Regex.IsMatch(Normalize(tag), "^#[0289PYLQGRJCUV]{3,15}$");
}
