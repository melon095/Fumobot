using Fumo.Database;
using Fumo.Interfaces;
using Fumo.Models;
using Serilog;
using Microsoft.Extensions.Configuration;
using Autofac;
using Fumo.Exceptions;
using Fumo.Database.DTO;
using System.Threading;

namespace Fumo.Handlers;

public class CommandHandler : ICommandHandler
{
    public ILifetimeScope LifetimeScope { get; }

    private Application Application { get; }

    private ILogger Logger { get; }

    private ICooldownHandler CooldownHandler { get; }

    private IConfiguration Configuration { get; }

    private CommandRepository CommandRepository { get; }

    private IMessageSenderHandler MessageSenderHandler { get; }

    public CommandHandler(
        ILifetimeScope lifetimeScope,
        Application application,
        ILogger logger,
        ICooldownHandler cooldownHandler,
        IConfiguration configuration,
        CommandRepository commandRepository,
        IMessageSenderHandler messageSenderHandler)
    {
        LifetimeScope = lifetimeScope;
        Application = application;
        Logger = logger;
        CooldownHandler = cooldownHandler;
        Configuration = configuration;
        CommandRepository = commandRepository;
        MessageSenderHandler = messageSenderHandler;

        this.Application.OnMessage += this.OnMessage;
    }

    // On messages that begin with the channel/global prefix are executed.
    private async ValueTask OnMessage(ChatMessage message, CancellationToken cancellationToken)
    {
        var prefix = GetPrefixForChannel(message.Channel);
        var (commandName, input) = ParseMessage(message.Input, prefix);
        if (commandName is null) return;

        if (!message.Input.ElementAt(0).StartsWith(prefix)) return;

        // FIXME: This is very ugly, so figure out how to replace the input list without changing ChatMessage from a record.
        message.Input.Clear();
        message.Input.AddRange(input);

        var result = await this.TryExecute(message, commandName, cancellationToken);

        if (result is null) return;

        var replyId = message.Privmsg.Reply.HasContent ? message.Privmsg.Reply.ParentMessageId : null;

        this.MessageSenderHandler.ScheduleMessage(message.Channel.TwitchName, result.Message, replyId);
    }

    private string GetPrefixForChannel(ChannelDTO channel)
    {
        var channelPrefix = channel.GetSetting(ChannelSettingKey.Prefix);

        if (channelPrefix is not "")
        {
            return channelPrefix;
        }

        return this.Configuration["GlobalPrefix"]!;
    }

    private static (string? commandName, string[] input) ParseMessage(List<string> message, string prefix)
        => (message.Count > 0 ? message[0].Replace(prefix, "") : null, message.Skip(1).ToArray());

    public async Task<CommandResult?> TryExecute(ChatMessage message, string commandName, CancellationToken cancellationToken = default)
    {
        using var commandScope = this.CommandRepository.CreateCommandScope(commandName);

        try
        {
            if (commandScope is null) return null;
            var command = commandScope.Resolve<ChatCommand>();


            bool isMod = message.Privmsg.Author.IsMod;
            bool isBroadcaster = message.User.TwitchID == message.Channel.TwitchID;

            // Fixme: Make cleaner
            if (!command.Permissions.All(message.User.Permissions.Contains))
            {
                return null;
            }
            else if (isMod && command.ModeratorOnly)
            {
                return null;
            }
            else if (isBroadcaster && command.BroadcasterOnly)
            {
                return null;
            }

            bool onCooldown = await this.CooldownHandler.IsOnCooldownAsync(message, command);
            if (onCooldown) return null;

            // FIXME: Add arguments thing here.

            /*
                FIXME: This should be changed. 
            */
            command.Channel = message.Channel;
            command.User = message.User;
            command.Input = message.Input;
            command.Privmsg = message.Privmsg;

            var result = await command.Execute(cancellationToken);

            await this.CooldownHandler.SetCooldownAsync(message, command);

            return result;
        }
        catch (InvalidInputException ex)
        {
            return ex.Message;
        }
        catch (Exception ex)
        {
            this.Logger.Error(ex, "Failed to execute command in {Channel}", message.Channel.TwitchName);
            return "A fatal error occured while executing the command.";
        }
    }
}
