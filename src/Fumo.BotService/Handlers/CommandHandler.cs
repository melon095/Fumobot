﻿using Autofac;
using Fumo.Database;
using Fumo.Database.DTO;
using Fumo.Database.Extensions;
using Fumo.Shared.Enums;
using Fumo.Shared.Exceptions;
using Fumo.Shared.Interfaces;
using Fumo.Shared.Models;
using Fumo.Shared.Repositories;
using Fumo.Shared.ThirdParty.Exceptions;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.Diagnostics;

namespace Fumo.Handlers;

internal class CommandHandler : ICommandHandler
{
    private readonly ILogger Logger;
    private readonly ICooldownHandler CooldownHandler;
    private readonly CommandRepository CommandRepository;
    private readonly IMessageSenderHandler MessageSenderHandler;
    private readonly DatabaseContext DatabaseContext;
    private readonly string GlobalPrefix;

    public CommandHandler(
        Application application,
        ILogger logger,
        ICooldownHandler cooldownHandler,
        AppSettings settings,
        CommandRepository commandRepository,
        IMessageSenderHandler messageSenderHandler,
        DatabaseContext databaseContext)
    {
        Logger = logger.ForContext<CommandHandler>();
        CooldownHandler = cooldownHandler;
        CommandRepository = commandRepository;
        MessageSenderHandler = messageSenderHandler;
        DatabaseContext = databaseContext;
        GlobalPrefix = settings.GlobalPrefix;

        application.OnMessage += this.OnMessage;

        Logger.Information("CommandHandler Initialized. Global Prefix is {GlobalPrefix}", GlobalPrefix);
    }


    // On messages that begin with the channel/global prefix are executed.
    private async ValueTask OnMessage(ChatMessage message, CancellationToken cancellationToken)
    {
        var prefix = GetPrefixForChannel(message.Channel);
        if (!message.Input[0].StartsWith(prefix)) return;

        var (commandName, input) = ParseMessage(string.Join(' ', message.Input), prefix);
        if (commandName is null) return;

        message.Input.Clear();
        message.Input.AddRange(input);

        var result = await this.TryExecute(message, commandName, cancellationToken);

        if (result is null) return;

        ScheduleMessageSpecification spec = new(message.Channel.TwitchName, result.Message)
        {
            IgnoreBanphrase = result.IgnoreBanphrase,
            ReplyID = result.ReplyID,
        };

        MessageSenderHandler.ScheduleMessage(spec);
    }

    private string GetPrefixForChannel(ChannelDTO channel)
    {
        var channelPrefix = channel.GetSetting(ChannelSettingKey.Prefix);

        if (!string.IsNullOrEmpty(channelPrefix))
        {
            return channelPrefix;
        }

        return GlobalPrefix;
    }

    private static (string?, ArraySegment<string>) ParseMessage(string message, string prefix)
    {
        var cleanMessage = ReplaceFirst(message, prefix, string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (cleanMessage.Length < 1) return (null, Array.Empty<string>());

        var commandName = cleanMessage.FirstOrDefault();

        return (commandName, new ArraySegment<string>(cleanMessage, 1, cleanMessage.Length - 1));

        static string ReplaceFirst(string input, string search, string replace)
        {
            ReadOnlySpan<char> i = input;
            int pos = i.IndexOf(search);
            if (pos < 0)
            {
                return input;
            }

            Span<char> output = stackalloc char[i.Length - search.Length + replace.Length];
            // Copy chars to output until 'pos' index
            i[..pos].CopyTo(output[..pos]);
            // Insert replacement at 'pos'
            replace.AsSpan().CopyTo(output[pos..]);
            // Copy the rest of input (after 'search' value) to output (after 'replace' value)
            i[(pos + search.Length)..].CopyTo(output[(pos + replace.Length)..]);
            return output.ToString();
        }
    }

    public async ValueTask<CommandResult?> TryExecute(ChatMessage message, string commandName, CancellationToken cancellationToken = default)
    {
        using var commandScope = this.CommandRepository.CreateCommandScope(commandName);
        if (commandScope is null) return null;
        var command = commandScope.Resolve<ChatCommand>();

        var stopwatch = Stopwatch.StartNew();
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
            if (!message.User.HasPermission("admin.execute"))
            {
                if (!command.Permissions.All(message.User.Permissions.Contains) ||
                    (!message.IsMod && command.ModeratorOnly) ||
                    (!message.IsBroadcaster && command.BroadcasterOnly))
                {
                    return null;
                }
            }

            bool onCooldown = await this.CooldownHandler.IsOnCooldown(message, command);
            if (onCooldown) return null;

            command.ParseArguments(message.Input);
            command.Channel = message.Channel;
            command.User = message.User;
            command.Input = message.Input;
            command.CommandInvocationName = commandName;

            this.Logger.Debug("Executing command {CommandName}", command.NameMatcher);

            var result = await command.Execute(cancellationToken);

            commandExecutionLogs.Success = true;
            commandExecutionLogs.Result = result.Message.Length > 0
                ? result.Message
                : "(No Response)";

            result.IgnoreBanphrase = command.Flags.HasFlag(ChatCommandFlags.IgnoreBanphrase);

            await this.CooldownHandler.SetCooldown(message, command);

            if (command.Flags.HasFlag(ChatCommandFlags.Reply))
            {
                result.ReplyID = message.ReplyID;
            }

            return result;
        }
        catch (Exception ex) when (ex is InvalidInputException || // xdd
                                   ex is UserNotFoundException ||
                                   ex is InvalidCommandArgumentException ||
                                   ex is GraphQLException)
        {
            commandExecutionLogs.Success = false;
            commandExecutionLogs.Result = ex.Message;

            return ex.Message;
        }
        catch (Exception ex)
        {
            commandExecutionLogs.Success = false;
            commandExecutionLogs.Result = ex.Message;

            this.Logger.Error(ex, "Failed to execute command in {Channel}", message.Channel.TwitchName);

            if (message.User.HasPermission("user.chat_error"))
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
            stopwatch.Stop();

            commandExecutionLogs.Duration = stopwatch.ElapsedMilliseconds;

            if (!string.IsNullOrEmpty(commandExecutionLogs.Result))
            {
                await this.DatabaseContext.CommandExecutionLogs.AddAsync(commandExecutionLogs, cancellationToken);

                await this.DatabaseContext.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
