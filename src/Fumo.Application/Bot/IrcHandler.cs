using Autofac;
using Fumo.Shared.Models;
using Fumo.Shared.Interfaces;
using MiniTwitch.Irc;
using MiniTwitch.Irc.Models;
using Fumo.Database.DTO;
using Quartz;
using Fumo.Application.BackgroundJobs.SevenTV;

namespace Fumo.Application.Bot;

public class IrcHandler
{
    public event Func<ChatMessage, CancellationToken, ValueTask> OnMessage = default!;

    private readonly Serilog.ILogger Logger;
    private readonly ILifetimeScope Scope;
    private readonly AppSettings Settings;
    private readonly CancellationToken CancellationToken;
    private readonly IrcClient IrcClient;
    private readonly IChannelRepository ChannelRepository;
    private readonly ISchedulerFactory SchedulerFactory;
    private readonly MetricsTracker MetricsTracker;

    public IrcHandler(
        Serilog.ILogger logger,
        ILifetimeScope scope,
        AppSettings settings,
        CancellationTokenSource cancellationTokenSource,
        IrcClient ircClient,
        IChannelRepository channelRepository,
        ISchedulerFactory schedulerFactory,
        MetricsTracker metricsTracker)
    {
        Logger = logger.ForContext<IrcHandler>();
        Scope = scope;
        Settings = settings;
        CancellationToken = cancellationTokenSource.Token;
        IrcClient = ircClient;
        ChannelRepository = channelRepository;
        SchedulerFactory = schedulerFactory;
        MetricsTracker = metricsTracker;

        IrcClient.OnMessage += IrcClient_OnMessage;

        ChannelRepository.OnChannelCreated += ChannelRepository_OnChannelCreated;
    }

    private async ValueTask ChannelRepository_OnChannelCreated(ChannelDTO arg)
    {
        var scheduler = await SchedulerFactory.GetScheduler(CancellationToken);

        // Cluegi
        await scheduler.TriggerJob(new(nameof(FetchChannelEditorsJob)), CancellationToken);
        await scheduler.TriggerJob(new(nameof(FetchEmoteSetsJob)), CancellationToken);
    }

    public async ValueTask Start()
    {
        Logger.Information("Connecting to TMI");

        var connected = Settings.DebugTMI switch
        {
            true => await IrcClient.ConnectAsync("ws://localhost:6969/", CancellationToken),
            false => await IrcClient.ConnectAsync(cancellationToken: CancellationToken)
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
            await IrcClient.JoinChannel(channel.TwitchName, CancellationToken);
        }
    }

    private async ValueTask IrcClient_OnMessage(Privmsg privmsg)
    {
        try
        {
            CancellationToken token = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken).Token;

            var messageScope = Scope.BeginLifetimeScope();
            var userRepo = messageScope.Resolve<IUserRepository>();

            var channel = ChannelRepository.GetByID(privmsg.Channel.Id.ToString());
            if (channel is null) return;

            var user = await userRepo.SearchID(privmsg.Author.Id.ToString(), token);

            MetricsTracker.TotalMessagesRead.WithLabels(channel.TwitchName).Inc();

            if (!user.TwitchName.Equals(privmsg.Author.Name))
            {
                user.TwitchName = privmsg.Author.Name;
                user.UsernameHistory.Add(new(user.TwitchName, DateTime.Now));

                await userRepo.SaveChanges(token);
            }

            var input = privmsg.Content
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            bool isBroadcaster = user.TwitchID == channel.TwitchID;
            bool isMod = privmsg.Author.IsMod || isBroadcaster;

            ChatMessage message = new()
            {
                Channel = channel,
                User = user,
                Input = input,
                IsBroadcaster = isBroadcaster,
                IsMod = isMod,
                Scope = messageScope,
                ReplyID = privmsg.Id
            };

            await (OnMessage?.Invoke(message, token) ?? ValueTask.CompletedTask);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to handle message in {Channel}", privmsg.Channel.Name);
        }
    }
}
