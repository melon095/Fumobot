using Autofac;
using Fumo.Database;
using Fumo.Database.DTO;
using Fumo.Interfaces;
using Fumo.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniTwitch.Irc;
using MiniTwitch.Irc.Models;
using Serilog;

namespace Fumo;

public class Application : IApplication
{
    public event Func<ChatMessage, CancellationToken, ValueTask> OnMessage = default!;

    public Dictionary<string, ChannelDTO> Channels { get; set; } = new();

    public DateTime StartTime { get; } = DateTime.Now;

    private ILogger Logger { get; }
    public IConfiguration Configuration { get; }
    private DatabaseContext Database { get; }

    private IUserRepository UserRepository { get; }

    private CancellationTokenSource CancellationTokenSource { get; }

    private IrcClient IrcClient { get; }

    public Application(
        ILogger logger,
        IConfiguration configuration,
        DatabaseContext database,
        IUserRepository userRepository,
        CancellationTokenSource cancellationTokenSource,
        IrcClient ircClient)
    {
        Logger = logger.ForContext<Application>();
        Configuration = configuration;
        Database = database;
        UserRepository = userRepository;
        CancellationTokenSource = cancellationTokenSource;
        IrcClient = ircClient;

        IrcClient.OnReconnect += IrcClient_OnReconnect;
        IrcClient.OnMessage += IrcClient_OnMessage;
    }

    public async Task StartAsync()
    {
        var channels = await this.Database.Channels.Where(x => !x.SetForDeletion).ToListAsync(CancellationTokenSource.Token);

        this.Channels = channels.ToDictionary(x => x.TwitchName);

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

        await IrcClient.JoinChannels(Channels.Select(x => x.Key), CancellationTokenSource.Token);
    }

    private async ValueTask IrcClient_OnReconnect()
    {
        var channels = Database.Channels
            .Where(x => !x.SetForDeletion)
            .ToList()
            .Select(x => x.TwitchName);

        await IrcClient.JoinChannels(channels, CancellationTokenSource.Token);
    }

    private async ValueTask IrcClient_OnMessage(Privmsg privmsg)
    {
        try
        {
            this.Logger.Information(privmsg.Content);

            var channel = this.Channels[privmsg.Channel.Name];
            if (channel is null) return;
            var user = await UserRepository.SearchIDAsync(privmsg.Author.Id.ToString(), CancellationTokenSource.Token);

            await CheckForRename(privmsg.Author, user);

            var input = privmsg.Content.Split(' ').ToList();

            ChatMessage message = new(channel, user, input, privmsg);
            CancellationToken token = CancellationTokenSource.CreateLinkedTokenSource(CancellationTokenSource.Token).Token;

            await (OnMessage?.Invoke(message, token) ?? ValueTask.CompletedTask);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to handle message in {Channel}", privmsg.Channel.Name);
        }
    }

    private async Task CheckForRename(MessageAuthor tmiUser, UserDTO user)
    {
        if (!user.TwitchName.Equals(tmiUser.Name))
        {
            user.TwitchName = tmiUser.Name;

            Database.Entry(user).State = EntityState.Modified;
            await Database.SaveChangesAsync(CancellationTokenSource.Token);
        }
    }
}
