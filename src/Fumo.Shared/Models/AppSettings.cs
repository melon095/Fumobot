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
    bool Verified
);

public record WebsiteSettings(
    string PublicURL
);

