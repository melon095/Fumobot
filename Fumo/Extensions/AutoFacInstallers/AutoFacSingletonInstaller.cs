using Autofac;
using Fumo.Handlers;
using Fumo.Shared.Interfaces;
using Fumo.Shared.Models;
using Fumo.Shared.Repositories;
using Fumo.ThirdParty.Emotes.SevenTV;
using Fumo.ThirdParty.ThreeLetterAPI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MiniTwitch.Irc;
using Prometheus;
using Serilog;
using StackExchange.Redis;
using StackExchange.Redis.KeyspaceIsolation;

namespace Fumo.Extensions.AutoFacInstallers;

public static class AutoFacSingletonInstaller
{
    public static ContainerBuilder InstallSingletons(this ContainerBuilder builder, IConfiguration config)
    {
        builder.Register(x =>
        {
            var ircClient = new IrcClient(x =>
            {
                x.Username = config["Twitch:Username"] ?? throw new ArgumentException($"{typeof(IrcClient)}");
                x.OAuth = config["Twitch:Token"] ?? throw new ArgumentException($"{typeof(IrcClient)}");
                x.Logger = new LoggerFactory().AddSerilog(Log.Logger.ForContext("IsSubLogger", true).ForContext("Client", "Main")).CreateLogger<IrcClient>();

                // https://dev.twitch.tv/docs/irc/#rate-limits
                if (config.GetValue<bool>("Twitch:Verified"))
                {
                    x.JoinRateLimit = 2000;
                }
            });

            return ircClient;
        }).SingleInstance();

        // Register Multiplexer
        builder.RegisterInstance(ConnectionMultiplexer.Connect(config["Connections:Redis"]!)).SingleInstance();

        // Register redis IDatabase with key prefix
        builder.Register(x => x.Resolve<ConnectionMultiplexer>().GetDatabase().WithKeyPrefix("fumobot:"));

        builder
            .RegisterType<CommandHandler>()
            .As<ICommandHandler>()
            .SingleInstance();

        builder
            .RegisterType<Application>()
                .As<IApplication>()
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
            .RegisterType<MetricsTracker>()
            .SingleInstance();

        builder
            .RegisterType<ChannelRepository>()
            .As<IChannelRepository>()
            .SingleInstance();

        builder
            .RegisterType<ThreeLetterAPI>()
            .As<IThreeLetterAPI>()
            .SingleInstance();

        builder
            .RegisterType<SevenTVService>()
            .As<ISevenTVService>()
            .SingleInstance();


        return builder;
    }
}
