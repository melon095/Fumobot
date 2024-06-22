using Fumo.Database.DTO;
using Fumo.Database;
using Fumo.Shared.Interfaces;
using Fumo.Shared.Models;
using Fumo.Shared.Regexes;
using System.Collections.Concurrent;
using Fumo.Database.Extensions;
using Fumo.Shared.ThirdParty.Pajbot1;
using Fumo.Shared.Enums;
using Fumo.Shared.ThirdParty.Helix;

using QueueValue = (string ChannelId, string Message, string? ReplyId);
using MiniTwitch.Helix.Responses;

namespace Fumo.Application.Bot;

using MessageQueue = ConcurrentQueue<QueueValue>;

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
                                await SendMessage(value.ChannelId, value.Message, value.ReplyId);
                                continue;
                            }

                            await Task.Delay(MessageSendInterval);
                            await SendMessage(value.ChannelId, value.Message, value.ReplyId);
                            continue;
                        }

                        SendHistory[channelId] = now;
                        await SendMessage(value.ChannelId, value.Message, value.ReplyId);
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
    public void ScheduleMessage(string channelId, string message, string? replyId = null)
    {
        SendHistory[channelId] = Unix();

        if (!Queues.TryGetValue(channelId, out var queue))
        {
            queue = new MessageQueue();
            Queues[channelId] = queue;
        }

        queue.Enqueue((channelId, message, replyId));
    }

    /// <inheritdoc/>
    public async ValueTask SendMessage(string channelId, string message, string? replyId = null)
    {
        try
        {
            if (string.IsNullOrEmpty(message)) return;

            SendHistory[channelId] = Unix();

            message = message.Trim();

            if (message.Length > MaxMessageLength)
            {
                message = message[..(MaxMessageLength - Ellipsis.Length)] + Ellipsis;
            }

            var helix = await HelixFactory.Create(CancellationToken);

            SentMessage sendResult = await helix.SendChatMessage(new(long.Parse(channelId), message, replyParentMessageId: replyId));
            var sendValue = sendResult.Data[0];

            MetricsTracker.TotalMessagesSent.Inc();

            if (sendValue.IsSent) return;

            Logger.Warning("Failed to send message '{MessageId}' to {ChannelId} {DropReason}", sendValue.MessageId, sendValue.DropReason);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to send message to {ChannelId}", channelId);
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
