using Autofac;
using Fumo.Shared.Models;
using Fumo.Shared.Interfaces;
using Microsoft.Extensions.Configuration;
using MiniTwitch.Irc;
using MiniTwitch.Irc.Interfaces;
using MiniTwitch.Irc.Models;
using Serilog;
using Fumo.Database.DTO;
using Quartz;
using Fumo.BackgroundJobs.SevenTV;

namespace Fumo;

public class Application
{
    public event Func<ChatMessage, CancellationToken, ValueTask> OnMessage = default!;

    private readonly ILogger Logger;
    private readonly ILifetimeScope Scope;
    private readonly IConfiguration Configuration;
    private readonly CancellationToken CancellationToken;
    private readonly IrcClient IrcClient;
    private readonly IChannelRepository ChannelRepository;
    private readonly ISchedulerFactory SchedulerFactory;
    private readonly MetricsTracker MetricsTracker;

    public Application(
        ILogger logger,
        ILifetimeScope scope,
        IConfiguration configuration,
        CancellationTokenSource cancellationTokenSource,
        IrcClient ircClient,
        IChannelRepository channelRepository,
        ISchedulerFactory schedulerFactory,
        MetricsTracker metricsTracker)
    {
        Logger = logger.ForContext<Application>();
        Scope = scope;
        Configuration = configuration;
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

    public async Task StartAsync()
    {
        Logger.Information("Connecting to TMI");

        var debugTMI = this.Configuration.GetValue<bool>("DebugTMI");
        var connected = debugTMI switch
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

            bool isBroadcaster = user.TwitchID == channel.TwitchID;
            bool isMod = privmsg.Author.IsMod || isBroadcaster;

            ChatMessage message = new(channel, user, input, isBroadcaster, isMod, privmsg.Id);

            await (OnMessage?.Invoke(message, token) ?? ValueTask.CompletedTask);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to handle message in {Channel}", privmsg.Channel.Name);
        }
    }
}
