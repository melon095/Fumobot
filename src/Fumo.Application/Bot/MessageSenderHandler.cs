using Fumo.Database.DTO;
using Fumo.Database;
using Fumo.Shared.Interfaces;
using Fumo.Shared.Models;
using Fumo.Shared.Regexes;
using MiniTwitch.Irc;
using System.Collections.Concurrent;
using Fumo.Database.Extensions;
using Fumo.Shared.ThirdParty.Pajbot1;

namespace Fumo.Application.Bot;

using MessageQueue = ConcurrentQueue<ScheduleMessageSpecification>;

public class MessageSenderHandler : IMessageSenderHandler, IDisposable
{
    public static readonly int MessageInterval = 1250;

    private readonly ConcurrentDictionary<string, MessageQueue> Queues = new();
    private readonly IrcClient IrcClient;
    private readonly CancellationToken CancellationToken;
    private readonly ConcurrentDictionary<string, long> SendHistory = new();
    private readonly Task MessageTask;
    private readonly MetricsTracker MetricsTracker;
    private readonly IChannelRepository ChannelRepository;
    private readonly Serilog.ILogger Logger;
    private readonly PajbotClient Pajbot = new();

    public MessageSenderHandler(
        IrcClient ircClient,
        CancellationTokenSource cancellationTokenSource,
        MetricsTracker metricsTracker,
        Serilog.ILogger logger,
        IChannelRepository channelRepository)
    {
        Logger = logger.ForContext<MessageSenderHandler>();
        IrcClient = ircClient;
        MetricsTracker = metricsTracker;
        ChannelRepository = channelRepository;
        CancellationToken = cancellationTokenSource.Token;

        //channelRepository.OnChannelDeleted += DoCleanChannelQueue;

        MessageTask = Task.Factory.StartNew(SendTask, TaskCreationOptions.LongRunning);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        MessageTask.Dispose();
    }

    private ValueTask DoCleanChannelQueue(ChannelDTO channel)
    {
        Logger.Information("Cleaning queue for {Channel}", channel.TwitchName);

        if (Queues.TryRemove(channel.TwitchName, out var queue))
        {
            queue.Clear();
        }

        return ValueTask.CompletedTask;
    }

    private Task SendTask() => Task.Run(async () =>
    {
        while (true)
            try
            {
                long now = Unix();
                foreach (var (channel, queue) in Queues)
                {
                    while (queue.TryDequeue(out var spec))
                    {
                        if (SendHistory.TryGetValue(channel, out var lastSent))
                        {
                            if (now - lastSent > MessageInterval)
                            {
                                await SendMessage(spec);
                                continue;
                            }

                            await Task.Delay(MessageInterval);
                            await SendMessage(spec);
                            continue;
                        }

                        SendHistory[channel] = now;
                        await SendMessage(spec);
                    }
                }

                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error in message sender task");
            }
    });

    private static long Unix() => DateTimeOffset.Now.ToUnixTimeMilliseconds();

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
            return await Pajbot.Check(message, pajbot1Instance, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Asking pajbot for banphrase for {Channel}", channel.TwitchName);
            // Lole @brian6932
            return (true, "Pepega 📣 SOMETHING'S WRONG WITH YOUR BANPHRASES -> (Check help command to remove it)");
        }
    }

    /// <inheritdoc/>
    public void ScheduleMessage(ScheduleMessageSpecification spec)
    {
        SendHistory[spec.Channel] = Unix();

        if (!Queues.TryGetValue(spec.Channel, out var queue))
        {
            queue = new MessageQueue();
            Queues[spec.Channel] = queue;
        }

        queue.Enqueue(spec);
    }

    public void ScheduleMessage(string channel, string message)
    {
        ScheduleMessage(new ScheduleMessageSpecification
        {
            Channel = channel,
            Message = message
        });
    }

    /// <inheritdoc/>
    public async ValueTask SendMessage(ScheduleMessageSpecification spec)
    {
        if (string.IsNullOrEmpty(spec.Message)) return;

        var dto = ChannelRepository.GetByName(spec.Channel)
            ?? throw new Exception($"Channel {spec.Channel} not found");

        if (!spec.IgnoreBanphrase)
        {
            var (banphraseCheck, banphraseReason) = await CheckBanphrase(dto, spec.Message, CancellationToken);
            if (banphraseCheck)
            {
                // Overwrite the output
                spec.Message = banphraseReason;
            }
        }

        SendHistory[spec.Channel] = Unix();

        spec.Message = spec.Message
            .Replace("\r", string.Empty)
            .Replace("\n", string.Empty)
            .Trim();

        MetricsTracker.TotalMessagesSent.Inc();

        if (spec.ReplyID is null)
        {
            await IrcClient.SendMessage(spec.Channel, spec.Message, cancellationToken: CancellationToken);
        }
        else
        {
            await IrcClient.ReplyTo(spec.ReplyID, spec.Channel, spec.Message, cancellationToken: CancellationToken);
        }
    }

    public ValueTask SendMessage(string channel, string message)
    {
        return SendMessage(new ScheduleMessageSpecification
        {
            Channel = channel,
            Message = message
        });
    }
}
