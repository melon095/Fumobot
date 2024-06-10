using Autofac;
using Fumo.Application.Bot;
using Fumo.Shared.Eventsub;
using Fumo.Shared.Interfaces;
using Fumo.Shared.Models;
using Fumo.Shared.Repositories;
using Fumo.Shared.ThirdParty.Emotes.SevenTV;
using Fumo.Shared.ThirdParty.Helix;
using Fumo.Shared.ThirdParty.ThreeLetterAPI;
using MiniTwitch.Irc;
using Serilog;
using StackExchange.Redis;
using StackExchange.Redis.KeyspaceIsolation;

namespace Fumo.Application.AutofacModule;

internal class SingletonModule(AppSettings settings) : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder
            .RegisterType<CommandRepository>()
            .AsSelf()
            .SingleInstance();

        builder.Register(x =>
        {
            var ircClient = new IrcClient(x =>
            {
                x.Username = settings.Twitch.Username;
                x.OAuth = settings.Twitch.Token;
                x.Logger = new LoggerFactory().AddSerilog(Log.Logger.ForContext("IsSubLogger", true).ForContext("Client", "Main")).CreateLogger<IrcClient>();

                // https://dev.twitch.tv/docs/irc/#rate-limits
                if (settings.Twitch.Verified)
                    x.JoinRateLimit = 2000;
            });

            return ircClient;
        }).SingleInstance();

        builder.RegisterInstance(ConnectionMultiplexer.Connect(settings.Connections.Redis)).SingleInstance();

        builder.Register(x => x.Resolve<ConnectionMultiplexer>().GetDatabase().WithKeyPrefix("fumobot:"));

        builder
            .RegisterType<CommandHandler>()
            .As<ICommandHandler>()
            .SingleInstance();

        builder
            .RegisterType<IrcHandler>()
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

        builder
            .RegisterType<HelixFactory>()
            .As<IHelixFactory>()
            .SingleInstance();

        builder
            .RegisterType<EventsubCommandRegistry>()
            .As<IEventsubCommandRegistry>()
            .SingleInstance();
    }
}
