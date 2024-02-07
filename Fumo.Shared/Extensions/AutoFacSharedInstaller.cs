using System.Reflection;
using Autofac;
using Fumo.Database;
using Fumo.Shared.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Templates;

namespace Fumo.Shared.Extensions;

public static class AutoFacSharedInstaller
{
    private static readonly ExpressionTemplate LoggingFormat = new("[{@t:HH:mm:ss} {@l,-11}] {Coalesce(SourceContext, '<none>')} {@m}\n{@x}");

    // Probably looks cleaner with multiple classes and methods but this is so much simpler :P
    public static ContainerBuilder InstallShared(this ContainerBuilder builder, IConfiguration config)
    {
        builder
            .RegisterType<CommandRepository>()
                .AsSelf()
                .SingleInstance();

        builder.RegisterInstance(config).SingleInstance();

        var logFileFormat = config["FUMO_PROG_TYPE"] switch
        {
            string type => $"logs_{type.ToLower()}.txt",
            null => $"logs_{Assembly.GetEntryAssembly()?.GetName().Name}.txt"
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
