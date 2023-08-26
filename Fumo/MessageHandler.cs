using Fumo.Database;
using Fumo.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MiniTwitch.Irc;
using MiniTwitch.Irc.Models;
using Serilog;

namespace Fumo;

public class MessageHandler
{
    public Serilog.ILogger Logger { get; }

    public IConfiguration Config { get; }

    public ICommandHandler CommadHandler { get; }
    public DatabaseContext Database { get; }
    public IUserRepository UserRepository { get; }
    public CancellationToken CancellationToken { get; }

    public IrcClient IrcClient { get; }

    public MessageHandler(Serilog.ILogger logger, IConfiguration config, ICommandHandler commadHandler, DatabaseContext database, IUserRepository userRepository, CancellationTokenSource cancellationTokenSource)
    {
        this.Logger = logger.ForContext<MessageHandler>();
        this.Config = config;
        this.CommadHandler = commadHandler;
        this.Database = database;
        this.UserRepository = userRepository;
        this.CancellationToken = cancellationTokenSource.Token;

        this.IrcClient = new(x =>
        {
            x.Username = config["Connections:Twitch:Username"] ?? throw new ArgumentException($"{typeof(IrcClient)}");
            x.OAuth = config["Connections:Twitch:Token"] ?? throw new ArgumentException($"{typeof(IrcClient)}");
            x.Logger = new LoggerFactory().AddSerilog(logger.ForContext("IsSubLogger", true).ForContext("Client", "Main")).CreateLogger<IrcClient>();
        });
    }

    public async Task StartAsync()
    {
        var connected = await this.IrcClient.ConnectAsync(cancellationToken: this.CancellationToken);

        if (!connected)
        {
            this.Logger.Fatal("Failed to connect to TMI");
            return;
        }

        // TODO: Join channels

        this.IrcClient.OnMessage += IrcClient_OnMessage;
    }

    private async ValueTask IrcClient_OnMessage(Privmsg privmsg)
    {
        var channel = await this.Database.Channels.SingleOrDefaultAsync(x => x.TwitchID.Equals(privmsg.Channel.Id));
        if (channel is null) return;

        var user = await this.UserRepository.SearchIDAsync(privmsg.Author.Id.ToString(), this.CancellationToken);

        var prefix = this.GetPrefixForChannel(channel);
        var (commandName, input) = ParseMessage(privmsg.Content, prefix);
        if (commandName is null) return;

        if (!privmsg.Content.StartsWith(prefix)) return;

        try
        {
            var result = await this.CommadHandler.TryExecute(channel, user, commandName, input);

            if (result is null) return;

            await this.IrcClient.ReplyTo(privmsg, result.Message, cancellationToken: this.CancellationToken);
        }
        catch (Exception ex)
        {
            this.Logger.Error(ex, "Failed to execute command in {Channel}", channel.TwitchName);
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

        return this.Config["GlobalPrefix"]!;
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
