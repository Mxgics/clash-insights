using System.Net;
using System.Security.Claims;

namespace ClashInsights.Api;

public sealed class HostingOptions {
    public bool Hosted { get; set; }
    public string[] KnownProxies { get; set; } = [];
    public string? DataProtectionPath { get; set; }
    public long MaxRequestBodyBytes { get; set; } = 1_048_576;
    public bool IsValid() => MaxRequestBodyBytes is >= 16_384 and <= 10_485_760
        && (!Hosted || KnownProxies.Length > 0)
        && KnownProxies.All(value => IPAddress.TryParse(value, out _));
}

public sealed class DatabaseOptions {
    public bool MigrateOnStartup { get; set; } = true;
}

public sealed class AuthOptions {
    public bool Enabled { get; set; }
    public string Domain { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string[] AllowedSubjects { get; set; } = [];
    public string[] AllowedEmails { get; set; } = [];
    public bool IsValid(bool hosted) => !Enabled
        ? !hosted
        : Uri.TryCreate(NormalizedAuthority, UriKind.Absolute, out var authority)
            && authority.Scheme == Uri.UriSchemeHttps
            && !string.IsNullOrWhiteSpace(ClientId)
            && !string.IsNullOrWhiteSpace(ClientSecret)
            && AllowedSubjects.Concat(AllowedEmails).Any(value => !string.IsNullOrWhiteSpace(value));
    public string NormalizedAuthority => Domain.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        ? Domain.TrimEnd('/') + "/"
        : $"https://{Domain.Trim().TrimEnd('/')}/";
    public bool Allows(ClaimsPrincipal user) {
        var subject = user.FindFirstValue("sub");
        var email = user.FindFirstValue(ClaimTypes.Email) ?? user.FindFirstValue("email");
        return (!string.IsNullOrWhiteSpace(subject) && AllowedSubjects.Contains(subject, StringComparer.Ordinal))
            || (!string.IsNullOrWhiteSpace(email) && AllowedEmails.Contains(email, StringComparer.OrdinalIgnoreCase));
    }
}

public sealed record SessionView(bool Authenticated, string? Name, bool LoginAvailable);
public sealed record ClientConfig(string? SentryDsn, string Environment, string? Release, double TracesSampleRate);
