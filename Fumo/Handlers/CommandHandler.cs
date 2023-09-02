using Autofac;
using Fumo.Database;
using Fumo.Database.DTO;
using Fumo.Enums;
using Fumo.Exceptions;
using Fumo.Interfaces;
using Fumo.Models;
using Fumo.Shared.Regexes;
using Fumo.Shared.Repositories;
using Fumo.ThirdParty.Pajbot1;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Fumo.Handlers;

internal class CommandHandler : ICommandHandler
{
    public ILifetimeScope LifetimeScope { get; }

    private IApplication Application { get; }

    private ILogger Logger { get; }

    private ICooldownHandler CooldownHandler { get; }

    private IConfiguration Configuration { get; }

    private CommandRepository CommandRepository { get; }

    private IMessageSenderHandler MessageSenderHandler { get; }

    private PajbotClient Pajbot { get; } = new();

    public CommandHandler(
        ILifetimeScope lifetimeScope,
        IApplication application,
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
        if (!message.Input.ElementAt(0).StartsWith(prefix)) return;

        var (commandName, input) = ParseMessage(message.Input, prefix);
        if (commandName is null) return;

        message.Input.Clear();
        message.Input.AddRange(input);

        var result = await this.TryExecute(message, commandName, cancellationToken);

        if (result is null) return;

        this.MessageSenderHandler.ScheduleMessage(message.Channel.TwitchName, result.Message, result.ReplyID);
    }

    private string GetPrefixForChannel(ChannelDTO channel)
    {
        var channelPrefix = channel.GetSetting(ChannelSettingKey.Prefix);

        if (!string.IsNullOrEmpty(channelPrefix))
        {
            return channelPrefix;
        }

        return this.Configuration["GlobalPrefix"]!;
    }

    private static (string?, List<string>) ParseMessage(List<string> message, string prefix)
    {
        var cleanMessage = message
            .Select(x => x.Replace(prefix, string.Empty))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        var commandName = cleanMessage.FirstOrDefault();

        return (commandName, cleanMessage.Skip(1).ToList());
    }

    private async Task<(bool Banned, string Reason)> CheckBanphrase(ChannelDTO channel, string message, CancellationToken cancellationToken)
    {
        foreach (var func in BanphraseRegex.GlobalRegex)
        {
            if (func(message))
            {
                return (true, "Global banphrase");
            }
        }

        var pajbot1Instance = channel.GetSetting(ChannelSettingKey.Pajbot1);
        if (string.IsNullOrEmpty(pajbot1Instance))
        {
            return (false, string.Empty);
        }

        try
        {
            var result = await this.Pajbot.Check(message, pajbot1Instance, cancellationToken);

            if (result.Banned)
            {
                return (true, "Pajbot");
            }

            return (false, string.Empty);
        }
        catch (Exception ex)
        {
            this.Logger.Error(ex, "Asking pajbot for banphrase for {Channel}", channel.TwitchName);
            return (true, "Internal error");
        }
    }

    public async Task<CommandResult?> TryExecute(ChatMessage message, string commandName, CancellationToken cancellationToken = default)
    {
        using var commandScope = this.CommandRepository.CreateCommandScope(commandName);
        if (commandScope is null) return null;

        try
        {
            var command = commandScope.Resolve<ChatCommand>();


            bool isMod = message.Privmsg.Author.IsMod;
            bool isBroadcaster = message.User.TwitchID == message.Channel.TwitchID;

            // Fixme: Make cleaner
            if (!command.Permissions.All(message.User.Permissions.Contains))
            {
                return null;
            }
            else if (!isMod && command.ModeratorOnly)
            {
                return null;
            }
            else if (!isBroadcaster && command.BroadcasterOnly)
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

            this.Logger.Debug("Executing command {CommandName}", command.NameMatcher);

            var result = await command.Execute(cancellationToken);

            await this.CooldownHandler.SetCooldownAsync(message, command);

            // FIXME: add some result logging

            if (string.IsNullOrEmpty(result.Message))
            {
                return null;
            }

            var (pajbotBanned, pajbotReason) = await this.CheckBanphrase(message.Channel, result.Message, cancellationToken);
            if (pajbotBanned)
            {
                result.Message = $"monkaS the response was blocked due to {pajbotReason}";
            }

            if ((command.Flags & ChatCommandFlags.Reply) != 0)
            {
                result.ReplyID = message.Privmsg.Id;
            }

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

