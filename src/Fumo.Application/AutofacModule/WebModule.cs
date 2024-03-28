using Autofac;
using Fumo.Application.Web.Service;

namespace Fumo.Application.AutofacModule;

public class WebModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<DescriptionService>().SingleInstance();
    }
}
