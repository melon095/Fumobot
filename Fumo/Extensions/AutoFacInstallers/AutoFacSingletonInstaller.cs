using Autofac;
using Fumo.Handlers;
using Fumo.Interfaces;
using Fumo.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MiniTwitch.Irc;
using Serilog;

namespace Fumo.Extensions.AutoFacInstallers;

public static class AutoFacSingletonInstaller
{
    public static ContainerBuilder InstallSingletons(this ContainerBuilder builder, IConfiguration config)
    {
        builder.Register(x =>
        {
            var socket = new SocketsHttpHandler()
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            };

            return new HttpClient(socket);
        });

        builder.Register(x =>
        {
            var ircClient = new IrcClient(x =>
            {
                x.Username = config["Twitch:Username"] ?? throw new ArgumentException($"{typeof(IrcClient)}");
                x.OAuth = config["Twitch:Token"] ?? throw new ArgumentException($"{typeof(IrcClient)}");
                x.Logger = new LoggerFactory().AddSerilog(Log.Logger.ForContext("IsSubLogger", true).ForContext("Client", "Main")).CreateLogger<IrcClient>();
            });

            return ircClient;
        });

        builder
            .RegisterType<CommandHandler>()
            .As<ICommandHandler>()
            .SingleInstance();

        builder
            .RegisterType<MessageHandler>()
            .AsSelf()
            .SingleInstance();

        builder
            .RegisterType<CooldownHandler>()
            .As<ICooldownHandler>()
            .SingleInstance();

        builder
            .RegisterType<MessageSenderHandler>()
            .As<IMessageSenderHandler>()
            .SingleInstance();

        builder
            .RegisterType<CommandRepository>()
            .AsSelf()
            .SingleInstance();

        return builder;
    }
}
