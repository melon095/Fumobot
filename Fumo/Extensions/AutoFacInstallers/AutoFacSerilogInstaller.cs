using Autofac;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Templates;

namespace Fumo.Extensions.AutoFacInstallers;

internal static class AutoFacSerilogInstaller
{
    private static readonly ExpressionTemplate LoggingFormat = new("[{@t:HH:mm:ss} {@l,-11}] {Coalesce(SourceContext, '<none>')} {@m}\n{@x}");
    private static readonly string LoggingFileFormat = "logs_.txt";

    public static ContainerBuilder InstallSerilog(this ContainerBuilder builder, IConfiguration config)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(config.GetValue<LogEventLevel>("Logging:LogLevel"))
            // Really fucking annoying during debugging. Ado debug logs are not pleasent to look at.
            .MinimumLevel.Override("Quartz", LogEventLevel.Information)
            .WriteTo.Console(LoggingFormat)
            .WriteTo.File(LoggingFormat, config.GetValue<string>("Logging:OutputFolder") + LoggingFileFormat, rollingInterval: RollingInterval.Day)
            .CreateLogger();

        builder
            .RegisterInstance(Log.Logger)
            .As<ILogger>()
            .SingleInstance();

        return builder;
    }
}
