using Autofac;
using Fumo.Shared.Models;
using Fumo.Shared.Interfaces;
using Microsoft.Extensions.Configuration;
using MiniTwitch.Irc;
using MiniTwitch.Irc.Interfaces;
using MiniTwitch.Irc.Models;
using Serilog;

namespace Fumo;

public class Application
{
    public event Func<ChatMessage, CancellationToken, ValueTask> OnMessage = default!;

    private readonly ILogger Logger;
    private readonly ILifetimeScope Scope;
    private readonly IConfiguration Configuration;
    private readonly CancellationTokenSource CancellationTokenSource;
    private readonly IrcClient IrcClient;
    private readonly IChannelRepository ChannelRepository;
    private readonly MetricsTracker MetricsTracker;

    public Application(
        ILogger logger,
        ILifetimeScope scope,
        IConfiguration configuration,
        CancellationTokenSource cancellationTokenSource,
        IrcClient ircClient,
        IChannelRepository channelRepository,
        MetricsTracker metricsTracker)
    {
        Logger = logger.ForContext<Application>();
        Scope = scope;
        Configuration = configuration;
        CancellationTokenSource = cancellationTokenSource;
        IrcClient = ircClient;
        ChannelRepository = channelRepository;
        MetricsTracker = metricsTracker;

        IrcClient.OnMessage += IrcClient_OnMessage;
        IrcClient.OnChannelJoin += IrcClient_OnChannelJoin;
        IrcClient.OnChannelPart += IrcClient_OnChannelPart;
    }

    private ValueTask IrcClient_OnChannelPart(IPartedChannel arg)
    {
        MetricsTracker.ChannelsJoined.Dec();

        return ValueTask.CompletedTask;
    }

    private ValueTask IrcClient_OnChannelJoin(IrcChannel channel)
    {
        MetricsTracker.ChannelsJoined.Inc();

        return ValueTask.CompletedTask;
    }

    public async Task StartAsync()
    {
        Logger.Information("Connecting to TMI");

        var debugTMI = this.Configuration.GetValue<bool>("DebugTMI");
        var connected = debugTMI switch
        {
            true => await IrcClient.ConnectAsync("ws://localhost:6969/", CancellationTokenSource.Token),
            false => await IrcClient.ConnectAsync(cancellationToken: CancellationTokenSource.Token)
        };

        if (!connected)
        {
            Logger.Fatal("Failed to connect to TMI");
            return;
        }

        Logger.Information("Connected to TMI");

        var channels = ChannelRepository.GetAll();

        foreach (var channel in channels)
        {
            await IrcClient.JoinChannel(channel.TwitchName, CancellationTokenSource.Token);
        }
    }

    private async ValueTask IrcClient_OnMessage(Privmsg privmsg)
    {
        try
        {
            CancellationToken token = CancellationTokenSource.CreateLinkedTokenSource(CancellationTokenSource.Token).Token;

            using var scope = Scope.BeginLifetimeScope();
            var userRepo = scope.Resolve<IUserRepository>();

            var channel = ChannelRepository.GetByID(privmsg.Channel.Id.ToString());
            if (channel is null) return;
            var user = await userRepo.SearchIDAsync(privmsg.Author.Id.ToString(), token);

            MetricsTracker.TotalMessagesRead.WithLabels(channel.TwitchName).Inc();

            if (!user.TwitchName.Equals(privmsg.Author.Name))
            {
                user.TwitchName = privmsg.Author.Name;
                user.UsernameHistory.Add(new(user.TwitchName, DateTime.Now));

                await userRepo.SaveChanges();
            }

            var input = privmsg.Content
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            ChatMessage message = new(channel, user, input, privmsg);

            await (OnMessage?.Invoke(message, token) ?? ValueTask.CompletedTask);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to handle message in {Channel}", privmsg.Channel.Name);
        }
    }
}
