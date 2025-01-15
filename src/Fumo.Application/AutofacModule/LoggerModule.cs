using Autofac;
using Serilog;

namespace Fumo.Application.AutofacModule;

internal class LoggerModule(IConfiguration rawConfig) : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(rawConfig)
            .CreateLogger();

        builder
            .RegisterInstance(Log.Logger)
            .As<Serilog.ILogger>()
            .SingleInstance();
    }
}
