using Autofac;
using System.Diagnostics;
using Fumo.Database;
using Fumo.Database.DTO;
using Fumo.Database.Extensions;
using Fumo.Shared.Enums;
using Fumo.Shared.Exceptions;
using Fumo.Shared.Models;
using Fumo.Shared.Repositories;
using Fumo.Shared.ThirdParty.Exceptions;
using MediatR;
using Fumo.Shared.Mediator;
using StackExchange.Redis;

namespace Fumo.Application.MediatorHandlers;

public class MessageReceivedCommandHandler(
    Serilog.ILogger logger,
    IDatabase redis,
    IMessageSenderHandler messageSenderHandler,
    CommandRepository commandRepository,
    DatabaseContext databaseContext,
    AppSettings settings)
        : INotificationHandler<MessageReceivedCommand>
{
    private readonly Serilog.ILogger Logger = logger.ForContext<MessageReceivedCommandHandler>();
    private readonly IDatabase Redis = redis;
    private readonly IMessageSenderHandler MessageSenderHandler = messageSenderHandler;
    private readonly CommandRepository CommandRepository = commandRepository;
    private readonly DatabaseContext DatabaseContext = databaseContext;
    private readonly string GlobalPrefix = settings.GlobalPrefix;

    private static string CooldownKey(ChatMessage message, ChatCommand command) => $"channel:{message.Channel.TwitchID}:cooldown:{command.NameMatcher}:{message.User.TwitchID}";

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

    private string GetPrefix(ChannelDTO channel)
        => channel.GetSetting(ChannelSettingKey.Prefix) switch
        {
            string prefix when !string.IsNullOrEmpty(prefix) => prefix,
            _ => GlobalPrefix,
        };

    private async ValueTask<CommandResult?> Execute(
        ChatCommand command,
        ChatMessage message,
        string commandInvocationName,
        CancellationToken cancellationToken = default)
    {
        CommandExecutionLogsDTO commandExecutionLogs = new()
        {
            Id = Guid.NewGuid(),
            CommandName = command.NameMatcher.ToString(),
            ChannelId = message.Channel.TwitchID,
            UserId = message.User.TwitchID,
            Success = false,
            Input = message.Input,
        };

        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (!message.User.HasPermission("admin.execute"))
            {
                if (!command.Permissions.All(message.User.Permissions.Contains) ||
                    !message.IsMod && command.ModeratorOnly ||
                    !message.IsBroadcaster && command.BroadcasterOnly)
                {
                    return null;
                }
            }

            bool onCooldown = await Redis.KeyExistsAsync(CooldownKey(message, command));
            if (onCooldown) return null;

            command.ParseArguments(message.Input);
            command.Channel = message.Channel;
            command.User = message.User;
            command.Input = message.Input;
            command.CommandInvocationName = commandInvocationName;

            Logger.Debug("Executing command {CommandName}", command.NameMatcher);

            var result = await command.Execute(cancellationToken);

            commandExecutionLogs.Success = true;
            commandExecutionLogs.Result = result.Message.Length > 0
                ? result.Message
                : "(No Response)";

            result.IgnoreBanphrase = command.Flags.HasFlag(ChatCommandFlags.IgnoreBanphrase);

            await Redis.StringSetAsync(CooldownKey(message, command), 1, expiry: command.Cooldown);

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

            Logger.Error(ex, "Failed to execute command {CommandName} in {Channel}", commandInvocationName, message.Channel.TwitchName);

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
                await DatabaseContext.CommandExecutionLogs.AddAsync(commandExecutionLogs, cancellationToken);

                await DatabaseContext.SaveChangesAsync(cancellationToken);
            }
        }
    }

    public async Task Handle(MessageReceivedCommand notification, CancellationToken cancellationToken)
    {
        var message = notification.Message;

        var prefix = GetPrefix(message.Channel);
        if (!message.Input[0].StartsWith(prefix)) return;

        var (commandName, input) = ParseMessage(string.Join(' ', message.Input), prefix);
        if (commandName is null) return;

        message.Input.Clear();
        message.Input.AddRange(input);

        var commandType = CommandRepository.GetCommandType(commandName);
        if (commandType is null) return;

        if (message.Scope.Resolve(commandType) is not ChatCommand commandInstance) return;

        var result = await Execute(commandInstance, message, commandName, cancellationToken);
        if (result is null || result.Message.Length == 0) return;

        var responseSpec = new MessageSendSpec(message.Channel.TwitchID, result.Message, result.ReplyID);


        await MessageSenderHandler.ScheduleMessageWithBanphraseCheck(responseSpec, message.Channel, cancellationToken);
    }
}
