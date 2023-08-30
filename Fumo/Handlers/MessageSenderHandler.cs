using Fumo.Database;
using Fumo.Interfaces;
using Fumo.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniTwitch.Irc;
using Serilog;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading.Channels;

namespace Fumo.Handlers;

using MessageQueue = ConcurrentQueue<(string Channel, string Message, string? ReplyID)>;


/*
    Made with help by foretack ;P
*/
public class MessageSenderHandler : IMessageSenderHandler
{
    public static readonly long MessageInterval = 1250;

    private ConcurrentDictionary<string, MessageQueue> Queues { get; } = new();

    private ILogger Logger { get; }

    private IrcClient IrcClient { get; }

    private CancellationToken CancellationToken { get; }

    private ConcurrentDictionary<string, long> SendHistory = new();

    private Task MessageTask { get; }

    public MessageSenderHandler(
        ILogger logger,
        IrcClient ircClient,
        CancellationTokenSource cancellationTokenSource)
    {
        Logger = logger;
        IrcClient = ircClient;
        CancellationToken = cancellationTokenSource.Token;

        MessageTask = Task.Factory.StartNew(SendTask, TaskCreationOptions.LongRunning);
    }

    private Task SendTask() => Task.Run(async () =>
    {
        while (true)
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

                        await Task.Delay((int)((now + MessageInterval) - lastSent));
                        await this.SendMessage(message.Channel, message.Message, message.ReplyID);
                        continue;
                    }

                    this.SendHistory[channel] = now;
                    await this.SendMessage(message.Channel, message.Message, message.ReplyID);
                }
            }
        }
    });

    private static long Unix() => DateTimeOffset.Now.ToUnixTimeMilliseconds();

    /// <summary>
    /// Schedule a message to be sent to a channel after the global message interval rule
    /// </summary>
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

    /// <summary>
    /// Will directly send a message to chat without obeying the message queue
    /// </summary>
    public ValueTask SendMessage(string channel, string message, string? replyID = null)
    {
        this.Logger.Debug("Sending message to {Channel}: {Message}", channel, message);

        this.SendHistory[channel] = Unix();

        return replyID is null
            ? this.IrcClient.SendMessage(channel, message, cancellationToken: this.CancellationToken)
            : this.IrcClient.ReplyTo(replyID, channel, message, cancellationToken: this.CancellationToken);
    }
}
