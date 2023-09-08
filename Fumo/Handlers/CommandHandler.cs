﻿using Autofac;
using Fumo.Database;
using Fumo.Database.DTO;
using Fumo.Database.Extensions;
using Fumo.Enums;
using Fumo.Exceptions;
using Fumo.Interfaces;
using Fumo.Models;
using Fumo.Repository;
using Fumo.Shared.Regexes;
using Fumo.ThirdParty.Exceptions;
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

    private DatabaseContext DatabaseContext { get; }

    private PajbotClient Pajbot { get; } = new();

    public CommandHandler(
        ILifetimeScope lifetimeScope,
        IApplication application,
        ILogger logger,
        ICooldownHandler cooldownHandler,
        IConfiguration configuration,
        CommandRepository commandRepository,
        IMessageSenderHandler messageSenderHandler,
        DatabaseContext databaseContext)
    {
        LifetimeScope = lifetimeScope;
        Application = application;
        Logger = logger.ForContext<CommandHandler>();
        CooldownHandler = cooldownHandler;
        Configuration = configuration;
        CommandRepository = commandRepository;
        MessageSenderHandler = messageSenderHandler;
        DatabaseContext = databaseContext;

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
        var command = commandScope.Resolve<ChatCommand>();

        CommandExecutionLogsDTO commandExecutionLogs = new()
        {
            Id = Guid.NewGuid(),
            CommandName = command.NameMatcher.ToString(),
            ChannelId = message.Channel.TwitchID,
            UserId = message.User.TwitchID,
            Success = false,
            Input = message.Input,
        };

        try
        {
            bool isMod = message.Privmsg.Author.IsMod;
            bool isBroadcaster = message.User.TwitchID == message.Channel.TwitchID;

            if (!message.User.MatchesPermission("admin.execute"))
            {
                if (!command.Permissions.All(message.User.Permissions.Contains) ||
                    (!isMod && command.ModeratorOnly) ||
                    (!isBroadcaster && command.BroadcasterOnly))
                {
                    return null;
                }
            }
            else
            {
                Logger.Debug("Bypass permission");
            }

            bool onCooldown = await this.CooldownHandler.IsOnCooldownAsync(message, command);
            if (onCooldown) return null;

            command.ParseArguments(message.Input);

            /*
                FIXME: This should be changed. 
            */
            command.Channel = message.Channel;
            command.User = message.User;
            command.Input = message.Input;
            command.Privmsg = message.Privmsg;
            command.CommandInvocationName = commandName;

            this.Logger.Debug("Executing command {CommandName}", command.NameMatcher);

            var result = await command.Execute(cancellationToken);

            commandExecutionLogs.Success = true;
            commandExecutionLogs.Result = result.Message.Length > 0
                ? result.Message
                : "(No Response)";

            await this.CooldownHandler.SetCooldownAsync(message, command);

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
        catch (Exception ex) when (ex is InvalidInputException ||
                                   ex is UserNotFoundException ||
                                   ex is GraphQLException)
        {
            commandExecutionLogs.Success = false;

            return ex.Message;
        }
        catch (Exception ex)
        {
            commandExecutionLogs.Success = false;
            this.Logger.Error(ex, "Failed to execute command in {Channel}", message.Channel.TwitchName);

            if (message.User.MatchesPermission("user.chat_error"))
            {
                return $"FeelsDankMan -> {ex.Message}";
            }
            else
            {
                return "FeelsDankMan something broke!";
            }
        }
        finally
        {
            if (!string.IsNullOrEmpty(commandExecutionLogs.Result))
            {
                await this.DatabaseContext.CommandExecutionLogs.AddAsync(commandExecutionLogs, cancellationToken);

                await this.DatabaseContext.SaveChangesAsync(cancellationToken);
            }
        }
    }
}

