using Autofac;
using Fumo.Shared.Interfaces;
using Fumo.Shared.Repositories;
using Fumo.ThirdParty.Emotes.SevenTV;
using Fumo.ThirdParty.ThreeLetterAPI;
using Microsoft.Extensions.Configuration;

namespace Fumo.Extensions.AutoFacInstallers;

public static class AutoFacScopedInstaller
{
    public static ContainerBuilder InstallScoped(this ContainerBuilder builder, IConfiguration _)
    {
        builder
            .RegisterType<UserRepository>()
            .As<IUserRepository>()
            .InstancePerLifetimeScope();

        builder
            .RegisterType<ChannelRepository>()
            .As<IChannelRepository>()
            .InstancePerLifetimeScope();

        builder
            .RegisterType<ThreeLetterAPI>()
            .As<IThreeLetterAPI>()
            .InstancePerLifetimeScope();

        builder
            .RegisterType<SevenTVService>()
            .As<ISevenTVService>()
            .InstancePerLifetimeScope();


        return builder;
    }
}
