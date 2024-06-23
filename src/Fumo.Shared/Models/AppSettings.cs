using Serilog.Events;

namespace Fumo.Shared.Models;

public record AppSettings(
    LoggingSettings Logging,
    ConnectionsSettings Connections,
    TwitchSettings Twitch,
    SevenTVSettings SevenTV,
    MetricsSettings Metrics,
    WebsiteSettings Website,
    bool DebugTMI,
    string GlobalPrefix = "!"
);

public record ConnectionsSettings(
    string Postgres,
    string Redis
);

public record LoggingSettings(
    LogEventLevel LogLevel,
    string OutputFolder
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
