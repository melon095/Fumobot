﻿using Autofac;
using Fumo.Interfaces;
using Fumo.Models;
using Fumo.ThirdParty.ThreeLetterAPI;
using Microsoft.Extensions.Configuration;

namespace Fumo.Extensions.AutoFacInstallers;

public static class AutoFacScopedInstaller
{
    public static ContainerBuilder InstallScoped(this ContainerBuilder builder, IConfiguration configuration)
    {
        builder
            .RegisterType<UserRepository>()
            .As<IUserRepository>()
            .InstancePerDependency();

        builder
            .RegisterType<ThreeLetterAPIBuilder>()
             .InstancePerDependency();

        return builder;
    }
}
