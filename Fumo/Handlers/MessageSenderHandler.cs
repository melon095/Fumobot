using Fumo.Interfaces;
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
    public static readonly long MessageInterval = 1250;

    private ConcurrentDictionary<string, MessageQueue> Queues { get; } = new();

    private IrcClient IrcClient { get; }

    private CancellationToken CancellationToken { get; }

    private readonly ConcurrentDictionary<string, long> SendHistory = new();

    private Task MessageTask { get; }

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

                        await Task.Delay((int)((now + MessageInterval) - lastSent));
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

        return replyID is null
            ? this.IrcClient.SendMessage(channel, message, cancellationToken: this.CancellationToken)
            : this.IrcClient.ReplyTo(replyID, channel, message, cancellationToken: this.CancellationToken);
    }
}
