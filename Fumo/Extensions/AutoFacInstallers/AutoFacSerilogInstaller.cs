using Autofac;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace Fumo.Extensions.AutoFacInstallers;

internal static class AutoFacSerilogInstaller
{
    private static readonly string LoggingFormat = "[{Timestamp:HH:mm:ss} {Level}] {SourceContext} - {Message:lj} {NewLine}{Exception}";
    private static readonly string LoggingFileFormat = "logs_.txt";

    public static ContainerBuilder InstallSerilog(this ContainerBuilder builder, IConfiguration config)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(config.GetValue<LogEventLevel>("Logging:LogLevel"))
            .WriteTo.Console(outputTemplate: LoggingFormat)
            .WriteTo.File(config.GetValue<string>("Logging:OutputFolder") + LoggingFileFormat, rollingInterval: RollingInterval.Day, outputTemplate: LoggingFormat)
            .CreateLogger();

        builder
            .RegisterInstance(Log.Logger)
            .As<ILogger>()
            .SingleInstance();

        return builder;
    }
}
