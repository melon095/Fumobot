using Autofac;
using Fumo.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Templates;

namespace Fumo.Shared.Extensions;

public static class AutoFacSharedInstaller
{
    public static ContainerBuilder InstallConfig(this ContainerBuilder builder, IConfiguration config)
    {
        builder.RegisterInstance(config).SingleInstance();

        return builder;
    }

    private static readonly ExpressionTemplate LoggingFormat = new("[{@t:HH:mm:ss} {@l,-11}] {Coalesce(SourceContext, '<none>')} {@m}\n{@x}");

    public static ContainerBuilder InstallSerilog(this ContainerBuilder builder, IConfiguration config)
    {
        var logFileFormat = config["FUMO_PROG_TYPE"] switch
        {
            "bot" => "logs_bot.txt",
            "web" => "logs_web.txt",
            _ => "logs.txt"
        };

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(config.GetValue<LogEventLevel>("Logging:LogLevel"))
            // Really fucking annoying during debugging. Ado debug logs are not pleasent to look at.
            .MinimumLevel.Override("Quartz", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .WriteTo.Console(LoggingFormat)
            .WriteTo.File(LoggingFormat, config.GetValue<string>("Logging:OutputFolder") + logFileFormat, rollingInterval: RollingInterval.Day)
            .CreateLogger();

        builder
            .RegisterInstance(Log.Logger)
            .As<ILogger>()
            .SingleInstance();

        return builder;
    }

    public static ContainerBuilder InstallDatabase(this ContainerBuilder builder, IConfiguration config)
    {
        var connectionString = config["Connections:Postgres"];

        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseNpgsql(connectionString)
            .Options;

        builder.Register(x =>
        {
            return new DatabaseContext(options);
        }).AsSelf().InstancePerLifetimeScope();

        return builder;
    }
}
