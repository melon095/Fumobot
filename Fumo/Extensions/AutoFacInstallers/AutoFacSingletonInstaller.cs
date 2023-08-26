using Autofac;
using Fumo.Interfaces;
using Fumo.Models;
using Microsoft.Extensions.Configuration;
using System.Reflection.Metadata.Ecma335;

namespace Fumo.Extensions.AutoFacInstallers;

public static class AutoFacSingletonInstaller
{
    public static ContainerBuilder InstallSingletons(this ContainerBuilder builder, IConfiguration config)
    {
        builder
            .RegisterType<CommandHandler>()
            .As<ICommandHandler>()
            .SingleInstance();

        builder
            .RegisterType<MessageHandler>()
            .AsSelf()
            .SingleInstance();

        return builder;
    }
}
