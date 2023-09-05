using Autofac;
using Fumo.Interfaces;
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
            .As<IUserRepository>();

        builder
            .RegisterType<ThreeLetterAPI>()
            .As<IThreeLetterAPI>();

        builder
            .RegisterType<SevenTVService>()
            .As<ISevenTVService>();


        return builder;
    }
}
