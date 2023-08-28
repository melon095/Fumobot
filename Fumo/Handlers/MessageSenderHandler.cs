using Fumo.Database;
using Fumo.Interfaces;
using Fumo.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using MiniTwitch.Irc;
using MiniTwitch.Irc.Models;
using Serilog;
using System.Threading.Channels;

namespace Fumo.Handlers;

// Channel, Message, ReplyID?
using MessageQueue = Queue<(string, string, string?)>;

internal class ChannelSchedule
{
    public MessageScheduleInterval Interval { get; set; }

    public MessageQueue Queue { get; set; }

    public ChannelSchedule(MessageScheduleInterval interval, MessageQueue queue)
    {
        Interval = interval;
        Queue = queue;
    }
}

public class MessageSenderHandler : IMessageSenderHandler
{
    private Dictionary<string, ChannelSchedule> MessageQueues = new();
    private Dictionary<string, Task> MessageTasks = new();

    private string BotID;

    private ILogger Logger { get; }

    private IMessageHandler MessageHandler { get; }

    private DatabaseContext DatabaseContext { get; }

    private IrcClient IrcClient { get; }

    private CancellationTokenSource CancellationTokenSource { get; }

    public MessageSenderHandler(
        ILogger logger,
        IConfiguration configuration,
        IMessageHandler messageHandler,
        DatabaseContext databaseContext,
        IrcClient ircClient,
        CancellationTokenSource cancellationTokenSource)
    {
        BotID = configuration["Twitch:UserID"]!;

        Logger = logger;
        MessageHandler = messageHandler;
        DatabaseContext = databaseContext;
        IrcClient = ircClient;
        CancellationTokenSource = cancellationTokenSource;

        this.MessageHandler.OnMessage += MessageHandler_OnMessage;
    }

    private async ValueTask MessageHandler_OnMessage(ChatMessage message, CancellationToken cancellationToken)
    {
        if (message.User.TwitchID.Equals(this.BotID)) return;

        var channelName = message.Channel.TwitchName;
        var queue = this.MessageQueues.TryGetValue(channelName, out var existingQueue)
            ? existingQueue
            : this.CreateDefaultQueue(channelName);

        bool botIsMod = message.Data.Privmsg.Author.IsMod;
        bool botisVip = message.Data.Privmsg.Author.IsVip;
        bool botIsBroadcaster = message.Channel.TwitchID == message.User.TwitchID;

        if (queue.Interval.Equals(MessageScheduleInterval.Read)) return;

        MessageScheduleInterval newInterval = MessageScheduleInterval.Write;

        if (botIsBroadcaster && !queue.Interval.Equals(MessageScheduleInterval.Bot))
        {
            newInterval = MessageScheduleInterval.Bot;
        }
        else if (botIsMod && !queue.Interval.Equals(MessageScheduleInterval.Mod))
        {
            newInterval = MessageScheduleInterval.Mod;
        }
        else if (botisVip && !queue.Interval.Equals(MessageScheduleInterval.VIP))
        {
            newInterval = MessageScheduleInterval.VIP;
        }

        queue.Interval = newInterval;

        message.Channel.SetSetting(ChannelSettingKey.MessageInterval, newInterval.ToString());

        this.DatabaseContext.Entry(message.Channel).State = EntityState.Modified;
        await this.DatabaseContext.SaveChangesAsync(cancellationToken);
    }

    private ChannelSchedule CreateDefaultQueue(string channel)
    {
        var newQueue = new ChannelSchedule(MessageScheduleInterval.Write, new());
        this.MessageQueues.Add(channel, newQueue);
        return newQueue;
    }

    private async Task ProcessQueueAsync(ChannelSchedule schedule)
    {
        while (true)
        {
            try
            {
                if (schedule.Queue.Count > 0)
                {
                    var (channel, message, replyID) = schedule.Queue.Dequeue();

                    if (replyID is null)
                    {
                        await this.IrcClient.SendMessage(channel, message, cancellationToken: this.CancellationTokenSource.Token);
                    }
                    else
                    {
                        await this.IrcClient.ReplyTo(replyID, channel, message, cancellationToken: this.CancellationTokenSource.Token);
                    }
                }
            }
            catch (Exception ex)
            {
                this.Logger.Error(ex, "Failed to handle message queue");
            }
        }
    }

    private async void Thread_Method()
    {
        foreach (var kvp in this.MessageQueues)
        {
            var channelname = kvp.Key;
            var channelSchedule = kvp.Value;

            var processingTask = this.ProcessQueueAsync(channelSchedule);

            this.MessageTasks[channelname] = processingTask;
        }

        // Not going to do any cleanup here.
    }

    public void Init()
    {
        try
        {
            IEnumerable<ChannelDTO> channels = this.DatabaseContext.Channels.ToList();

            foreach (var channel in channels)
            {
                if (Enum.TryParse<MessageScheduleInterval>(channel.GetSetting(ChannelSettingKey.MessageInterval), out var interval))
                {
                    this.MessageQueues.Add(channel.TwitchName, new(interval, new()));
                }
            }
        }
        finally
        {
            new Task(Thread_Method, this.CancellationTokenSource.Token, TaskCreationOptions.LongRunning).Start();
        }
    }

    public void ScheduleMessage(string channel, string message, string? replyID = null)
    {
        var queue = this.MessageQueues.TryGetValue(channel, out var existingQueue)
            ? existingQueue
            : this.CreateDefaultQueue(channel);

        queue.Queue.Enqueue((channel, message, replyID));
    }
}
