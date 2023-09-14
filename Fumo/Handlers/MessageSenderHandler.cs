using Fumo.Shared.Interfaces;
using MiniTwitch.Irc;
using Serilog;
using System.Collections.Concurrent;

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

    public MessageSenderHandler(
        IrcClient ircClient,
        CancellationTokenSource cancellationTokenSource)
    {
        IrcClient = ircClient;
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
    });

    private static long Unix() => DateTimeOffset.Now.ToUnixTimeMilliseconds();

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
    public ValueTask SendMessage(string channel, string message, string? replyID = null)
    {
        this.SendHistory[channel] = Unix();

        message = message
            .Replace("\r", string.Empty)
            .Replace("\n", string.Empty)
            .Trim();

        return replyID is null
            ? this.IrcClient.SendMessage(channel, message, cancellationToken: this.CancellationToken)
            : this.IrcClient.ReplyTo(replyID, channel, message, cancellationToken: this.CancellationToken);
    }
}
