using Fumo.Database.DTO;
using Fumo.Database;
using Fumo.Shared.Regexes;
using System.Collections.Concurrent;
using Fumo.Database.Extensions;
using Fumo.Shared.ThirdParty.Pajbot1;
using Fumo.Shared.Enums;
using Fumo.Shared.ThirdParty.Helix;
using MiniTwitch.Irc;

namespace Fumo.Shared.Models;

using MessageQueue = ConcurrentQueue<MessageSendData>;

public abstract record MessageSendMethod(string Identifier)
{
    public sealed record Irc(string ChannelName) : MessageSendMethod(ChannelName);
    public sealed record Helix(string ChanneLId) : MessageSendMethod(ChanneLId);
}

public record MessageSendData(
    string ChannelId, string Message,
    string? ReplyId = null, MessageSendMethod? SendMethod = null);

public interface IMessageSenderHandler
{
    /// <summary>
    /// Schedule a message to be sent to a channel after the global message interval rule
    /// </summary>
    void ScheduleMessage(MessageSendData data);
    void ScheduleMessage(MessageSendData data, ChannelDTO channel);

    /// <summary>
    /// It's <see cref="ScheduleMessage"/> but will run <see cref="CheckBanphrase"/> before sending.
    /// This will send the <see cref="BanphraseReason"/> if the message is banned.
    /// </summary>
    void ScheduleMessageWithBanphraseCheck(MessageSendData data, ChannelDTO channel);

    /// <summary>
    /// Will directly send a message to chat without obeying the message queue
    /// </summary>
    ValueTask SendMessage(MessageSendData data);

    void Cleanup(string channelId);

    ValueTask<BanphraseReason> CheckBanphrase(ChannelDTO channel, string message, CancellationToken cancellationToken = default);
}

public class MessageSenderHandler : IMessageSenderHandler, IDisposable
{
    private const int MessageSendInterval = 1250;
    private const int QueueCheckInterval = 100;
    private const int MaxMessageLength = 500;
    private const string Ellipsis = "…";

    private readonly ConcurrentDictionary<string, MessageQueue> Queues = new();
    private readonly ConcurrentDictionary<string, long> SendHistory = new();

    private readonly Task MessageTask;
    private readonly CancellationToken CancellationToken;

    private readonly MetricsTracker MetricsTracker;
    private readonly PajbotClient Pajbot = new();
    private readonly Serilog.ILogger Logger;
    private readonly IHelixFactory HelixFactory;
    private readonly IrcClient Irc;

    public MessageSenderHandler(
        CancellationTokenSource cancellationTokenSource,
        MetricsTracker metricsTracker,
        Serilog.ILogger logger,
        IHelixFactory helixFactory,
        IrcClient irc)
    {
        Logger = logger.ForContext<MessageSenderHandler>();
        MetricsTracker = metricsTracker;
        CancellationToken = cancellationTokenSource.Token;
        HelixFactory = helixFactory;
        Irc = irc;

        MessageTask = Task.Factory.StartNew(SendTask, TaskCreationOptions.LongRunning);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        MessageTask.Wait();
    }

    private async Task SendTask()
    {
        while (!CancellationToken.IsCancellationRequested)
        {
            try
            {
                long now = Unix();
                foreach (var (channelId, queue) in Queues)
                {
                    while (queue.TryDequeue(out var value))
                    {
                        if (SendHistory.TryGetValue(channelId, out var lastSent))
                        {
                            if (now - lastSent > MessageSendInterval)
                            {
                                await SendMessage(value);
                                continue;
                            }

                            await Task.Delay(MessageSendInterval);
                            await SendMessage(value);
                            continue;
                        }

                        SendHistory[channelId] = now;
                        await SendMessage(value);
                    }
                }

                await Task.Delay(QueueCheckInterval, CancellationToken);
            }
            catch (TaskCanceledException)
            {
                Logger.Information("Message sender task was cancelled");

                return;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error in message sender task");
            }
        }
    }

    private static long Unix() => DateTimeOffset.Now.ToUnixTimeMilliseconds();

    private static MessageSendMethod DecideSendMethod(ChannelDTO channel)
        => channel.GetSettingBool(ChannelSettingKey.ConnectedWithEventsub) switch
        {
            true => new MessageSendMethod.Helix(channel.TwitchID),
            false => new MessageSendMethod.Irc(channel.TwitchName),
        };

    private static string CleanTheMessage(string input)
    {
        var message = input.Trim();

        if (message.Length > MaxMessageLength)
        {
            message = message[..(MaxMessageLength - Ellipsis.Length)] + Ellipsis;
        }

        return message;
    }

    private async ValueTask<bool> SendIrc(MessageSendData data, MessageSendMethod method)
    {
        var message = CleanTheMessage(data.Message);

        if (data.ReplyId is string replyId)
            await Irc.ReplyTo(replyId, method.Identifier, message, cancellationToken: CancellationToken);
        else
            await Irc.SendMessage(method.Identifier, message, cancellationToken: CancellationToken);

        return true;
    }

    private async ValueTask<bool> SendHelix(MessageSendData data, MessageSendMethod method)
    {
        var message = CleanTheMessage(data.Message);

        var helix = await HelixFactory.Create(CancellationToken);

        var sendResult = await helix.SendChatMessage(new(long.Parse(method.Identifier), message, replyParentMessageId: data.ReplyId));
        if (!sendResult.Success)
        {
            Logger.Error("Failed to send message to {ChannelId}. {Error}", method.Identifier, sendResult.Message);
            return false;
        }

        var sendValue = sendResult.Value.Data[0];

        if (sendValue.IsSent) return true;

        Logger.Warning("Tried sending '{Message}' to {ChannelId} but got dropped. {DropReason}", message, sendValue.DropReason);

        return false;
    }

    /// <inheritdoc/>
    public void ScheduleMessage(MessageSendData data)
    {
        SendHistory[data.ChannelId] = Unix();

        if (!Queues.TryGetValue(data.ChannelId, out var queue))
        {
            queue = new MessageQueue();
            Queues[data.ChannelId] = queue;
        }

        queue.Enqueue(data);
    }

    public void ScheduleMessage(MessageSendData data, ChannelDTO channel)
    {
        var sendMethod = DecideSendMethod(channel);

        ScheduleMessage(data with { SendMethod = sendMethod });
    }

    /// <inheritdoc/>
    public async void ScheduleMessageWithBanphraseCheck(MessageSendData data, ChannelDTO channel)
    {
        try
        {
            var bancheckResult = await CheckBanphrase(channel, data.Message, CancellationToken);
            var finalData = bancheckResult switch
            {
                BanphraseReason.None => data,
                BanphraseReason.PajbotTimeout => data with { Message = $"⚠ {data.Message}" },
                _ => data with { Message = bancheckResult.ToReasonString() },
            };

            ScheduleMessage(finalData, channel);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to schedule message with banphrase check for {ChannelId}", data.ChannelId);
        }
    }

    /// <inheritdoc/>
    public async ValueTask SendMessage(MessageSendData data)
    {
        if (string.IsNullOrEmpty(data.Message)) return;

        SendHistory[data.ChannelId] = Unix();

        bool success = false;
        try
        {
            switch (data.SendMethod)
            {
                case MessageSendMethod.Irc:
                    {
                        success = await SendIrc(data, data.SendMethod);
                    }
                    break;

                case MessageSendMethod.Helix:
                    {
                        success = await SendHelix(data, data.SendMethod);
                    }
                    break;

                default:
                    throw new NotImplementedException($"Unknown send method {data.SendMethod}");
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to send message to {ChannelId}", data.ChannelId);
        }
        finally
        {
            if (success)
                MetricsTracker.TotalMessagesSent.Inc();
        }
    }

    public void Cleanup(string channelId)
    {
        Logger.Information("Cleaning queue for {ChannelId}", channelId);

        SendHistory.TryRemove(channelId, out _);
        if (Queues.TryRemove(channelId, out var queue))
            queue.Clear();
    }

    public async ValueTask<BanphraseReason> CheckBanphrase(ChannelDTO channel, string message, CancellationToken cancellationToken = default)
    {
        foreach (var func in BanphraseRegex.GlobalRegexes)
        {
            if (func(message))
            {
                return BanphraseReason.Global;
            }
        }

        var pajbotUrl = channel.GetSetting(ChannelSettingKey.Pajbot1);
        if (string.IsNullOrEmpty(pajbotUrl))
        {
            return BanphraseReason.None;
        }

        try
        {
            if (await Pajbot.Check(message, pajbotUrl, cancellationToken) is true)
                return BanphraseReason.Pajbot;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to query pajbot for '{ChannelName} {ChannelId}'", channel.TwitchName, channel.TwitchID);

            return BanphraseReason.PajbotTimeout;
        }

        return BanphraseReason.None;
    }
}
