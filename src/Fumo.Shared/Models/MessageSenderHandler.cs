using Fumo.Database.DTO;
using Fumo.Database;
using Fumo.Shared.Regexes;
using System.Collections.Concurrent;
using Fumo.Database.Extensions;
using Fumo.Shared.ThirdParty.Pajbot1;
using Fumo.Shared.Enums;
using Fumo.Shared.ThirdParty.Helix;
using MiniTwitch.Helix.Responses;

namespace Fumo.Shared.Models;

using MessageQueue = ConcurrentQueue<MessageSendSpec>;

public record MessageSendSpec(string ChannelId, string Message, string? ReplyId = null);

public interface IMessageSenderHandler
{
    /// <summary>
    /// Schedule a message to be sent to a channel after the global message interval rule
    /// </summary>
    void ScheduleMessage(MessageSendSpec spec);

    /// <summary>
    /// Will directly send a message to chat without obeying the message queue
    /// </summary>
    ValueTask SendMessage(MessageSendSpec spec);

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

    public MessageSenderHandler(
        CancellationTokenSource cancellationTokenSource,
        MetricsTracker metricsTracker,
        Serilog.ILogger logger,
        IHelixFactory helixFactory)
    {
        Logger = logger.ForContext<MessageSenderHandler>();
        MetricsTracker = metricsTracker;
        CancellationToken = cancellationTokenSource.Token;
        HelixFactory = helixFactory;

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
            catch (Exception ex)
            {
                Logger.Error(ex, "Error in message sender task");
            }
        }
    }

    private static long Unix() => DateTimeOffset.Now.ToUnixTimeMilliseconds();

    /// <inheritdoc/>
    public void ScheduleMessage(MessageSendSpec spec)
    {
        SendHistory[spec.ChannelId] = Unix();

        if (!Queues.TryGetValue(spec.ChannelId, out var queue))
        {
            queue = new MessageQueue();
            Queues[spec.ChannelId] = queue;
        }

        queue.Enqueue(spec);
    }

    /// <inheritdoc/>
    public async ValueTask SendMessage(MessageSendSpec spec)
    {
        try
        {
            if (string.IsNullOrEmpty(spec.Message)) return;

            SendHistory[spec.ChannelId] = Unix();

            var message = spec.Message.Trim();

            if (message.Length > MaxMessageLength)
            {
                message = message[..(MaxMessageLength - Ellipsis.Length)] + Ellipsis;
            }

            var helix = await HelixFactory.Create(CancellationToken);

            SentMessage sendResult = await helix.SendChatMessage(new(long.Parse(spec.ChannelId), message, replyParentMessageId: spec.ReplyId));
            var sendValue = sendResult.Data[0];

            MetricsTracker.TotalMessagesSent.Inc();

            if (sendValue.IsSent) return;

            Logger.Warning("Failed to send message '{MessageId}' to {ChannelId} {DropReason}", sendValue.MessageId, sendValue.DropReason);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to send message to {ChannelId}", spec.ChannelId);
        }
    }

    public void Cleanup(string channelId)
    {
        if (Queues.TryRemove(channelId, out var queue))
        {
            Logger.Information("Cleaning queue for {ChannelId}", channelId);
            queue.Clear();
        }
        else
        {
            Logger.Information("No queue to clean for {ChannelId}", channelId);
        }
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

        if (channel.GetSetting(ChannelSettingKey.Pajbot1) is not string pajbotUrl)
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
