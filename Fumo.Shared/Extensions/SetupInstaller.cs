using System.Reflection;
using Autofac;
using Fumo.Database;
using Fumo.Shared.Models;
using Fumo.Shared.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Templates;

namespace Fumo.Shared.Extensions;

public static class SetupInstaller
{
    private static readonly ExpressionTemplate LoggingFormat = new("[{@t:HH:mm:ss} {@l,-11}] {Coalesce(SourceContext, '<none>')} {@m}\n{@x}");

    // Probably looks cleaner with multiple classes and methods but this is so much simpler :P
    public static ContainerBuilder InstallShared(this ContainerBuilder builder, AppSettings settings)
    {
        builder
            .RegisterType<CommandRepository>()
                .AsSelf()
                .SingleInstance();

        var logFileFormat = Environment.GetEnvironmentVariable("FUMO_PROG_TYPE") switch
        {
            string type => $"logs_{type.ToLower()}.txt",
            null => $"logs_{Assembly.GetEntryAssembly()?.GetName().Name}.txt"
        };

        var logPath = Path.Combine(settings.Logging.OutputFolder, logFileFormat);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(settings.Logging.LogLevel)
            // Really fucking annoying during debugging. Ado debug logs are not pleasent to look at.
            .MinimumLevel.Override("Quartz", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .WriteTo.Console(LoggingFormat)
            .WriteTo.File(LoggingFormat, logPath, rollingInterval: RollingInterval.Day)
            .CreateLogger();

        builder
            .RegisterInstance(Log.Logger)
            .As<ILogger>()
            .SingleInstance();

        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseNpgsql(settings.Connections.Postgres)
            .Options;

        builder.Register(x =>
        {
            return new DatabaseContext(options);
        }).AsSelf().InstancePerLifetimeScope();

        return builder;
    }

    public static ContainerBuilder InstallAppSettings(this ContainerBuilder builder, IConfiguration config, out AppSettings settings)
    {
        settings = config.Get<AppSettings>()
            ?? throw new InvalidOperationException($"Unable to bind {nameof(AppSettings)} from config");

        builder.RegisterInstance(config).As<IConfiguration>().SingleInstance();
        builder.RegisterInstance(settings).SingleInstance();

        return builder;
    }

    public static IConfigurationRoot PrepareConfig(string[] args)
    {
        var configPath = args.Length > 0 ? args[0] : "config.json";

        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(configPath, optional: false, reloadOnChange: true)
            .Build();
    }
}
