using Autofac;
using Fumo.Application.Startable;

namespace Fumo.Application.AutofacModule;

public class StartableModule : Module
{
    public static readonly IReadOnlyList<Type> Order =
    [
        typeof(InitialDataStarter),
        typeof(CreateBotMetadataStarter),
        typeof(BackgroundJobStarter),
        typeof(IrcStarter)
    ];

    protected override void Load(ContainerBuilder builder)
    {
        foreach (var type in Order)
            builder.RegisterType(type).SingleInstance();
    }
}
