using Autofac;
using Fumo.Database;
using Fumo.Interfaces;
using Fumo.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniTwitch.Irc;
using MiniTwitch.Irc.Models;
using Serilog;
using System.Runtime.InteropServices;

namespace Fumo;

public class Application
{
    public event Func<ChatMessage, CancellationToken, ValueTask> OnMessage;

    public List<ChannelDTO> Channels = new();

    private ILogger Logger { get; }

    private DatabaseContext Database { get; }

    private IUserRepository UserRepository { get; }

    private CancellationTokenSource CancellationTokenSource { get; }

    private IrcClient IrcClient { get; }

    public Application(
        ILogger logger,
        DatabaseContext database,
        IUserRepository userRepository,
        CancellationTokenSource cancellationTokenSource,
        IrcClient ircClient)
    {
        Logger = logger.ForContext<Application>();
        Database = database;
        UserRepository = userRepository;
        CancellationTokenSource = cancellationTokenSource;
        IrcClient = ircClient;

        IrcClient.OnConnect += IrcClient_OnConnect;
        IrcClient.OnReconnect += IrcClient_OnConnect;
    }

    public async Task StartAsync()
    {
        Logger.Information("Connecting to TMI");

        var connected = await IrcClient.ConnectAsync(cancellationToken: CancellationTokenSource.Token);

        if (!connected)
        {
            Logger.Fatal("Failed to connect to TMI");
            return;
        }

        Logger.Information("Connected to TMI");

        IrcClient.OnMessage += IrcClient_OnMessage;

        var channels = await this.Database.Channels.ToListAsync(CancellationTokenSource.Token);

        this.Channels = channels;

        await IrcClient.JoinChannels(channels.Select(x => x.TwitchName), CancellationTokenSource.Token);
    }

    private async ValueTask IrcClient_OnConnect()
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
            var channel = await Database.Channels.SingleOrDefaultAsync(x => x.TwitchID.Equals(privmsg.Channel.Id));
            if (channel is null) return;
            var user = await UserRepository.SearchIDAsync(privmsg.Author.Id.ToString(), CancellationTokenSource.Token);

            await CheckForRename(privmsg.Author, user);

            var input = privmsg.Content.Split(' ').ToList();

            ChatMessage message = new(channel, user, input, privmsg);
            CancellationToken token = CancellationTokenSource.CreateLinkedTokenSource(CancellationTokenSource.Token).Token;

            await OnMessage.Invoke(message, token);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to handle message in {Channel}", privmsg.Channel);
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
