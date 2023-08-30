using Fumo.Database;
using Fumo.Interfaces;
using Fumo.Models;
using Serilog;
using Microsoft.Extensions.Configuration;
using Autofac;
using Fumo.Exceptions;
using Fumo.Database.DTO;

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

    private async ValueTask OnMessage(ChatMessage message, CancellationToken cancellationToken)
    {

        var prefix = GetPrefixForChannel(message.Channel);
        var (commandName, input) = ParseMessage(message.Input, prefix);
        if (commandName is null) return;

        if (!input.ElementAt(0).StartsWith(prefix)) return;

        var replyId = message.Privmsg.Reply.HasContent ? message.Privmsg.Reply.ParentMessageId : null;

        using var commandScope = this.CommandRepository.CreateCommandScope(commandName);

        try
        {
            if (commandScope is null) return;
            var command = commandScope.Resolve<ChatCommand>();


            bool isMod = message.Privmsg.Author.IsMod;
            bool isBroadcaster = message.User.TwitchID == message.Channel.TwitchID;

            bool allowedToRun = (
                command.Permissions.All(message.User.Permissions.Contains) &&
                isMod == command.ModeratorOnly &&
                isBroadcaster == command.BroadcasterOnly
            );

            if (!allowedToRun) return;

            bool onCooldown = await this.CooldownHandler.IsOnCooldownAsync(message, command);
            if (onCooldown) return;

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

            this.MessageSenderHandler.ScheduleMessage(message.Channel.TwitchName, result.Message, replyId);
        }
        catch (InvalidInputException ex)
        {
            this.MessageSenderHandler.ScheduleMessage(message.Channel.TwitchName, ex.Message, replyId);
            return;
        }
        catch (Exception ex)
        {
            this.Logger.Error(ex, "Failed to execute command in {Channel}", message.Channel.TwitchName);
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

        return this.Configuration["GlobalPrefix"]!;
    }

    private static (string? commandName, string[] input) ParseMessage(List<string> message, string prefix)
        => (message.Count > 0 ? message[0].Replace(prefix, "") : null, message.Skip(1).ToArray());

    public Task<CommandResult> TryExecute(ChatMessage message, string commandName)
    {
        throw new NotImplementedException();
    }
}
