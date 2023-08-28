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

namespace Fumo.Handlers;

public class MessageHandler : IMessageHandler
{
    public event Func<ChatMessage, CancellationToken, ValueTask> OnMessage;

    private ILogger Logger { get; }

    private DatabaseContext Database { get; }

    private IUserRepository UserRepository { get; }

    private CancellationTokenSource CancellationTokenSource { get; }

    private IrcClient IrcClient { get; }

    public MessageHandler(
        ILogger logger,
        DatabaseContext database,
        IUserRepository userRepository,
        CancellationTokenSource cancellationTokenSource,
        IrcClient ircClient)
    {
        Logger = logger.ForContext<MessageHandler>();
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

        var connected = await IrcClient.ConnectAsync(cancellationToken: this.CancellationTokenSource.Token);

        if (!connected)
        {
            Logger.Fatal("Failed to connect to TMI");
            return;
        }

        Logger.Information("Connected to TMI");

        IrcClient.OnMessage += IrcClient_OnMessage;
    }

    private async ValueTask IrcClient_OnConnect()
    {
        var channels = Database.Channels
            .Where(x => !x.SetForDeletion)
            .ToList()
            .Select(x => x.TwitchName);

        await IrcClient.JoinChannels(channels, this.CancellationTokenSource.Token);
    }

    private async ValueTask IrcClient_OnMessage(Privmsg privmsg)
    {
        try
        {
            var channel = await Database.Channels.SingleOrDefaultAsync(x => x.TwitchID.Equals(privmsg.Channel.Id));
            if (channel is null) return;
            var user = await UserRepository.SearchIDAsync(privmsg.Author.Id.ToString(), this.CancellationTokenSource.Token);

            await CheckForRename(privmsg.Author, user);

            var input = privmsg.Content.Split(' ').ToList();

            ChatMessage message = new(channel, user, input, new(privmsg));
            CancellationToken token = CancellationTokenSource.CreateLinkedTokenSource(this.CancellationTokenSource.Token).Token;

            await this.OnMessage.Invoke(message, token);
        }
        catch (Exception ex)
        {
            this.Logger.Error(ex, "Failed to handle message in {Channel}", privmsg.Channel);
        }
    }

    private async Task CheckForRename(MessageAuthor tmiUser, UserDTO user)
    {
        if (!user.TwitchName.Equals(tmiUser.Name))
        {
            user.TwitchName = tmiUser.Name;

            this.Database.Entry(user).State = EntityState.Modified;
            await this.Database.SaveChangesAsync(this.CancellationTokenSource.Token);
        }
    }
}
