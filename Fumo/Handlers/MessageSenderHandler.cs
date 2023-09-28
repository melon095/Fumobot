using Fumo.Database.DTO;
using Fumo.Database;
using Fumo.Shared.Interfaces;
using Fumo.Shared.Models;
using Fumo.Shared.Regexes;
using MiniTwitch.Irc;
using System.Collections.Concurrent;
using Fumo.Database.Extensions;
using Fumo.ThirdParty.Pajbot1;
using Serilog;

namespace Fumo.Handlers;

using MessageQueue = ConcurrentQueue<(string Channel, string Message, string? ReplyID)>;

/*
    Made with help by foretack ;P
*/
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
    private readonly ILogger Logger;
    private readonly PajbotClient Pajbot = new();

    public MessageSenderHandler(
        IrcClient ircClient,
        CancellationTokenSource cancellationTokenSource,
        MetricsTracker metricsTracker,
        ILogger logger,
        IChannelRepository channelRepository)
    {
        Logger = logger.ForContext<MessageSenderHandler>();
        IrcClient = ircClient;
        MetricsTracker = metricsTracker;
        ChannelRepository = channelRepository;
        CancellationToken = cancellationTokenSource.Token;

        MessageTask = Task.Factory.StartNew(SendTask, TaskCreationOptions.LongRunning);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        this.MessageTask.Dispose();
    }

    private Task SendTask() => Task.Run(async () =>
    {
        while (true)
            try
            {

                {
                    long now = Unix();
                    foreach (var (channel, queue) in this.Queues)
                    {
                        while (queue.TryDequeue(out var message))
                        {
                            if (this.SendHistory.TryGetValue(channel, out var lastSent))
                            {
                                if (now - lastSent > MessageInterval)
                                {
                                    await this.SendMessage(message.Channel, message.Message, message.ReplyID);
                                    continue;
                                }

                                await Task.Delay(MessageInterval);
                                await this.SendMessage(message.Channel, message.Message, message.ReplyID);
                                continue;
                            }

                            this.SendHistory[channel] = now;
                            await this.SendMessage(message.Channel, message.Message, message.ReplyID);
                        }
                    }

                    // This super duper incredibly important line of code stops the garbage collector from going insane.
                    // Without it the garbage collector would run level 1 collector every few milliseconds for a few seconds.
                    await Task.Delay(100);
                }
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
            return (true, "Internal error");
        }
    }


    /// <inheritdoc/>
    public void ScheduleMessage(string channel, string message, string? replyID = null)
    {
        this.SendHistory[channel] = Unix();

        if (!this.Queues.TryGetValue(channel, out var queue))
        {
            queue = new MessageQueue();
            this.Queues[channel] = queue;
        }

        queue.Enqueue((channel, message, replyID));
    }

    /// <inheritdoc/>
    public async ValueTask SendMessage(string channel, string message, string? replyID = null)
    {
        if (string.IsNullOrEmpty(message)) return;

        var dto = await ChannelRepository.GetByName(channel, CancellationToken)
            ?? throw new Exception($"Channel {channel} not found");

        var (banphraseCheck, banphraseReason) = await CheckBanphrase(dto, message, CancellationToken);
        if (banphraseCheck)
        {
            // Overwrite the output
            message = $"FeelsOkayMan blocked by 👉 {banphraseReason}";
        }

        this.SendHistory[channel] = Unix();

        message = message
            .Replace("\r", string.Empty)
            .Replace("\n", string.Empty)
            .Trim();

        MetricsTracker.TotalMessagesSent.Inc();

        if (replyID is null)
        {
            await this.IrcClient.SendMessage(channel, message, cancellationToken: this.CancellationToken);
        }
        else
        {
            await this.IrcClient.ReplyTo(replyID, channel, message, cancellationToken: this.CancellationToken);
        }
    }
}
