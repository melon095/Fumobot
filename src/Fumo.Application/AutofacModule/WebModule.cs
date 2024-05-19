using Autofac;
using Fumo.Application.Web.Service;
using Fumo.Shared.Eventsub;

namespace Fumo.Application.AutofacModule;

public class WebModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<HttpContextAccessor>().As<IHttpContextAccessor>();
        builder.RegisterType<DescriptionService>().SingleInstance();
        builder.RegisterType<HttpUserService>().InstancePerLifetimeScope();
        builder.RegisterType<EventsubManager>().As<IEventsubManager>().SingleInstance();
    }
}
