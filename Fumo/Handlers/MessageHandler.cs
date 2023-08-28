using Autofac;
using Fumo.Database;
using Fumo.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MiniTwitch.Irc;
using MiniTwitch.Irc.Models;
using Serilog;

namespace Fumo.Handlers;

public class MessageHandler
{
    public Serilog.ILogger Logger { get; }

    public IConfiguration Config { get; }

    public ICommandHandler CommadHandler { get; }

    public DatabaseContext Database { get; }

    public IUserRepository UserRepository { get; }

    public CancellationToken CancellationToken { get; }

    public IrcClient IrcClient { get; }

    public MessageHandler(
        Serilog.ILogger logger,
        IConfiguration config,
        ICommandHandler commadHandler,
        DatabaseContext database,
        IUserRepository userRepository,
        CancellationTokenSource cancellationTokenSource,
        IrcClient ircClient)
    {
        Logger = logger.ForContext<MessageHandler>();
        Config = config;
        CommadHandler = commadHandler;
        Database = database;
        UserRepository = userRepository;
        CancellationToken = cancellationTokenSource.Token;
        IrcClient = ircClient;

        IrcClient.OnConnect += IrcClient_OnConnect;
        IrcClient.OnReconnect += IrcClient_OnConnect;
    }

    public async Task StartAsync()
    {
        Logger.Information("Connecting to TMI");

        var connected = await IrcClient.ConnectAsync(cancellationToken: CancellationToken);

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

        await IrcClient.JoinChannels(channels, CancellationToken);
    }

    private async ValueTask IrcClient_OnMessage(Privmsg privmsg)
    {
        var channel = await Database.Channels.SingleOrDefaultAsync(x => x.TwitchID.Equals(privmsg.Channel.Id));
        if (channel is null) return;

        var prefix = GetPrefixForChannel(channel);
        var (commandName, input) = ParseMessage(privmsg.Content, prefix);
        if (commandName is null) return;

        if (!privmsg.Content.StartsWith(prefix)) return;

        try
        {
            var user = await UserRepository.SearchIDAsync(privmsg.Author.Id.ToString(), CancellationToken);

            var result = await CommadHandler.TryExecute(channel, user, commandName, input);

            if (result is null) return;

            await IrcClient.ReplyTo(privmsg, result.Message, cancellationToken: CancellationToken);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to execute command in {Channel}", channel.TwitchName);
            return;
        }
    }

    private string GetPrefixForChannel(ChannelDTO channel)
    {
        var channelPrefix = channel.GetSetting(ChannelSettingKey.Prefix);

        if (channelPrefix is not "")
        {
            return channelPrefix;
        }

        return Config["GlobalPrefix"]!;
    }

    private static (string? commandName, string[] input) ParseMessage(string message, string prefix)
    {
        string[] parts = message
            .Replace(prefix, "")
            .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        string? commandName = parts.Length > 0 ? parts[0] : null;
        string[] input = parts.Skip(1).ToArray();

        return (commandName, input);
    }
}
