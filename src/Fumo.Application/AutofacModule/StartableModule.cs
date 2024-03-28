using Autofac;
using Fumo.Application.Startable;

namespace Fumo.Application.AutofacModule;

public class StartableModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<BackgroundJobStarter>().SingleInstance();
        builder.RegisterType<CreateBotMetadataStarter>().SingleInstance();
        builder.RegisterType<IrcStarter>().SingleInstance();
    }
}
