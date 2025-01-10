using Fumo.Database.DTO;
using Fumo.Database;
using Fumo.Shared.Regexes;
using System.Collections.Concurrent;
using Fumo.Database.Extensions;
using Fumo.Shared.ThirdParty.Pajbot1;
using Fumo.Shared.Enums;
using Fumo.Shared.ThirdParty.Helix;
using MiniTwitch.Irc;
using SerilogTracing;
using Serilog.Context;
using Serilog.Core;
using System.Diagnostics;

namespace Fumo.Shared.Models;

using MessageQueue = ConcurrentQueue<(MessageSendData, ILogEventEnricher, LoggerActivity)>;

public abstract record MessageSendMethod(string Identifier)
{
    public sealed record Irc(string ChannelName) : MessageSendMethod(ChannelName);
    public sealed record Helix(string ChannelId) : MessageSendMethod(ChannelId);
}

public record MessageSendData(string Message, MessageSendMethod SendMethod, string? ReplyId = null);

public interface IMessageSenderHandler
{
    MessageSendMethod DecideSendMethod(ChannelDTO channel);

    MessageSendData Prepare(string message, ChannelDTO channel, string? replyId = null);

    void ScheduleMessage(MessageSendData data);

    void ScheduleMessageWithBanphraseCheck(MessageSendData data, ChannelDTO channel);

    ValueTask SendMessage(MessageSendData data);

    void Cleanup(MessageSendMethod method);

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
    private readonly PajbotClient Pajbot;
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
        Pajbot = new(Logger);

        MessageTask = Task.Factory.StartNew(SendTask, TaskCreationOptions.LongRunning);
    }

    public void Dispose()
    {
        MessageTask.Wait();
        Pajbot.Dispose();

        GC.SuppressFinalize(this);
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
                    await ProcessQueue(channelId, queue, now);
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

    private async Task ProcessQueue(string channelId, MessageQueue queue, long now)
    {
        while (queue.TryDequeue(out var item))
        {
            var (value, enrichers, activity) = item;

            using (LogContext.Push(enrichers))
            {
                using (Activity.Current = activity.Activity)
                {
                    using (activity)
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
            }
        }
    }

    private static long Unix() => DateTimeOffset.Now.ToUnixTimeMilliseconds();

    private static string CleanTheMessage(string input)
    {
        var message = input.Trim();

        if (message.Length > MaxMessageLength)
        {
            message = message[..(MaxMessageLength - Ellipsis.Length)] + Ellipsis;
        }

        return message;
    }

    private async ValueTask<bool> SendIrc(MessageSendData data)
    {
        Logger.Information("Sending to {ChannelId} with IRC");

        //var message = CleanTheMessage(data.Message);

        //if (data.ReplyId is string replyId)
        //    await Irc.ReplyTo(replyId, data.SendMethod.Identifier, message, cancellationToken: CancellationToken);
        //else
        //    await Irc.SendMessage(data.SendMethod.Identifier, message, cancellationToken: CancellationToken);

        return true;
    }

    private async ValueTask<bool> SendHelix(MessageSendData data)
    {
        Logger.Information("Sending to {ChannelId} with Helix", data.SendMethod.Identifier);

        //var message = CleanTheMessage(data.Message);

        //var helix = await HelixFactory.Create(CancellationToken);

        //var sendResult = await helix.SendChatMessage(new(long.Parse(data.SendMethod.Identifier), message, replyParentMessageId: data.ReplyId));
        //if (!sendResult.Success)
        //{
        //    Logger.Error("Failed to send message to {ChannelId}. {Error}", data.SendMethod.Identifier, sendResult.Message);
        //    return false;
        //}

        //var sendValue = sendResult.Value.Data[0];

        //if (sendValue.IsSent) return true;

        //Logger.Warning("Tried sending '{Message}' to {ChannelId} but got dropped. {DropReason}", message, sendValue.DropReason);

        return false;
    }

    public MessageSendMethod DecideSendMethod(ChannelDTO channel)
         => channel.GetSettingBool(ChannelSettingKey.ConnectedWithEventsub) switch
         {
             true => new MessageSendMethod.Helix(channel.TwitchID),
             false => new MessageSendMethod.Irc(channel.TwitchName),
         };

    public MessageSendData Prepare(string message, ChannelDTO channel, string? replyId = null)
        => new(message, DecideSendMethod(channel), replyId);

    public void ScheduleMessage(MessageSendData data)
    {
        SendHistory[data.SendMethod.Identifier] = Unix();

        if (!Queues.TryGetValue(data.SendMethod.Identifier, out var queue))
        {
            queue = new MessageQueue();
            Queues[data.SendMethod.Identifier] = queue;
        }

        queue.Enqueue((data, LogContext.Clone(), Logger.StartActivity("Schedule Message {ChannelId}")));
    }

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

            ScheduleMessage(finalData);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to schedule message with banphrase check for {ChannelId}");
        }
    }

    public async ValueTask SendMessage(MessageSendData data)
    {
        if (string.IsNullOrEmpty(data.Message)) return;

        SendHistory[data.SendMethod.Identifier] = Unix();

        bool success = false;
        try
        {
            switch (data.SendMethod)
            {
                case MessageSendMethod.Irc:
                    {
                        success = await SendIrc(data);
                    }
                    break;

                case MessageSendMethod.Helix:
                    {
                        success = await SendHelix(data);
                    }
                    break;

                default:
                    throw new NotImplementedException($"Unknown send method {data.SendMethod}");
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to send message to {ChannelId}");
        }
        finally
        {
            if (success)
                MetricsTracker.TotalMessagesSent.Inc();
        }
    }

    public void Cleanup(MessageSendMethod method)
    {
        Logger.Information("Cleaning queue for {ChannelId}", method.Identifier);

        SendHistory.TryRemove(method.Identifier, out _);
        if (Queues.TryRemove(method.Identifier, out var queue))
            queue.Clear();
    }

    public async ValueTask<BanphraseReason> CheckBanphrase(ChannelDTO channel, string message, CancellationToken cancellationToken = default)
    {
        using var activity = Logger.StartActivity("Banphrase check for {ChannelId}");

        foreach (var func in BanphraseRegex.GlobalRegexes)
        {
            if (func(message))
            {
                Logger.Information("Global Banphrase detected");

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
            {
                Logger.Information("Pajbot Banphrase detected");
                return BanphraseReason.Pajbot;
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to query pajbot for '{ChannelName} {ChannelId}'");

            return BanphraseReason.PajbotTimeout;
        }

        return BanphraseReason.None;
    }
}
