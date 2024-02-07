using Autofac;
using Fumo.Shared.Interfaces;
using Fumo.Shared.Repositories;
using Microsoft.Extensions.Configuration;

namespace Fumo.Extensions.AutoFacInstallers;

public static class AutoFacScopedInstaller
{
    public static ContainerBuilder InstallScoped(this ContainerBuilder builder)
    {
        builder
            .RegisterType<UserRepository>()
            .As<IUserRepository>()
            .InstancePerLifetimeScope();

        return builder;
    }
}
