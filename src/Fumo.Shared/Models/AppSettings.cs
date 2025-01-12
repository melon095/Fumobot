using System.Text.Json.Serialization;
using Serilog.Events;

namespace Fumo.Shared.Models;

public record AppSettings(
    ConnectionsSettings Connections,
    TwitchSettings Twitch,
    SevenTVSettings SevenTV,
    MetricsSettings Metrics,
    WebsiteSettings Website,
    bool DebugTMI,
    string GlobalPrefix = "!",
    MessageSendingMethod MessageSendingMethod = MessageSendingMethod.Helix,
    string? UserAgent = null
);

public record ConnectionsSettings(
    string Postgres,
    string Redis
);

public record MetricsSettings(
    int Port,
    bool Enabled
);

public record SevenTVSettings(
    string Bearer
);

public record TwitchSettings(
    string Username,
    string UserID,
    string Token,
    string ThreeLetterAPI,
    string ClientID,
    string ClientSecret,
    bool Verified
);

public record WebsiteSettings(
    Uri PublicURL,
    DataProtectionSettings DataProtection
);

public record DataProtectionSettings(
    string RedisKey,
    string CertificateFile,
    string CertificatePass
);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MessageSendingMethod
{
    Helix,
    Console
}