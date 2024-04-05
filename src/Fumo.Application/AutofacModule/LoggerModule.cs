using System.Reflection;
using Autofac;
using Fumo.Shared.Models;
using Serilog;
using Serilog.Events;
using Serilog.Templates;
using Serilog.Templates.Themes;

namespace Fumo.Application.AutofacModule;

internal class LoggerModule(AppSettings settings) : Autofac.Module
{
    private static readonly ExpressionTemplate LoggingFormat = new("[{@t:HH:mm:ss} {@l,-11}] {Coalesce(SourceContext, '<none>')} {@m}\n{@x}", theme: TemplateTheme.Code);

    protected override void Load(ContainerBuilder builder)
    {
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
            .As<Serilog.ILogger>()
            .SingleInstance();
    }
}
