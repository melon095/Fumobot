﻿using Autofac;
using Fumo.Database;
using Fumo.Database.DTO;
using Fumo.Database.Extensions;
using Fumo.Shared.Eventsub;
using Fumo.Shared.Models;
using Fumo.Shared.Repositories;
using MediatR;
using MiniTwitch.Helix.Responses;
using MiniTwitch.Irc;
using Serilog;

namespace Fumo.Shared.Mediator;

#region Subscribed

[EventsubCommand(EventsubCommandType.Subscribed, "channel.chat.message")]
internal record ChannelChatMessageSubscribedCommand(CreatedSubscription.Info Info)
    : EventsubVerificationCommand<EventsubBasicCondition>(Info.Condition);

internal class ChannelChatMessageSubscribedCommandHandler : IRequestHandler<ChannelChatMessageSubscribedCommand>
{
    private const string MigratedMessage = ":) 👋 Hi again.";
    private const string JoinMessage = "FeelsDankMan 👋 Hi.";

    private readonly ILogger Logger;
    private readonly IChannelRepository ChannelRepository;
    private readonly IUserRepository UserRepository;
    private readonly IMessageSenderHandler MessageSenderHandler;
    private readonly IrcClient IrcClient;

    public ChannelChatMessageSubscribedCommandHandler(
        ILogger logger,
        IChannelRepository channelRepository,
        IUserRepository userRepository,
        IMessageSenderHandler messageSenderHandler,
        IrcClient ircClient)
    {
        Logger = logger.ForContext<ChannelChatMessageSubscribedCommandHandler>();
        ChannelRepository = channelRepository;
        UserRepository = userRepository;
        MessageSenderHandler = messageSenderHandler;
        IrcClient = ircClient;
    }

    public async Task Handle(ChannelChatMessageSubscribedCommand request, CancellationToken ct)
    {
        var user = await UserRepository.SearchID(request.Condition.BroadcasterId, ct);
        if (user is null)
        {
            Logger.Warning("User {BroadcasterId} not in database", request.Condition.BroadcasterId);
            return;
        }

        Logger.Information("Received chat message verification: {UserID} {UserName}", user.TwitchID, user.TwitchName);

        var joinMessage = string.Empty;
        var channel = ChannelRepository.GetByID(user.TwitchID);
        if (channel is not null)
        {
            await IrcClient.PartChannel(channel.TwitchName, ct);
            channel.SetSetting(ChannelSettingKey.ConnectedWithEventsub, true.ToString());

            Logger.Information("Parted previous channel {ChannelName} from IRC", channel.TwitchName);

            joinMessage = MigratedMessage;
        }
        else
        {
            ChannelDTO newChannel = new()
            {
                TwitchID = user.TwitchID,
                TwitchName = user.TwitchName,
                Settings =
                [
                    new() { Key = ChannelSettingKey.ConnectedWithEventsub, Value = true.ToString() }
                ],
                UserTwitchID = user.TwitchID
            };

            channel = await ChannelRepository.Create(newChannel, ct);

            Logger.Information("Created new channel {ChannelName} in database", channel.TwitchName);

            joinMessage = JoinMessage;
        }

        await ChannelRepository.Update(channel, ct);

        MessageSenderHandler.ScheduleMessage(new(joinMessage, new MessageSendMethod.Helix(channel.TwitchID)));
    }
}

#endregion

#region Notification

[EventsubCommand(EventsubCommandType.Notification, "channel.chat.message")]
internal class ChatMessageNotificationCommand : ChannelChatMessageBody, IRequest;

internal class ChatMessageNotificationCommandHandler(
    ILogger logger,
    IUserRepository userRepository,
    IChannelRepository channelRepository,
    IMediator bus,
    ILifetimeScope lifetimeScope,
    MetricsTracker metricsTracker)
    : IRequestHandler<ChatMessageNotificationCommand>
{
    private readonly ILogger Logger = logger.ForContext<ChatMessageNotificationCommandHandler>();
    private readonly IUserRepository UserRepository = userRepository;
    private readonly IChannelRepository ChannelRepository = channelRepository;
    private readonly IMediator Bus = bus;
    private readonly ILifetimeScope LifetimeScope = lifetimeScope;
    private readonly MetricsTracker MetricsTracker = metricsTracker;

    private static List<string> ParseMessage(string input)
    {
        const char ACTION_DENOTER = '\u0001';

        if (input.Length > 9 && input[0] == ACTION_DENOTER && input[^1] == ACTION_DENOTER)
            input = input[8..^1];

        return [.. input.Split(' ', StringSplitOptions.RemoveEmptyEntries)];
    }

    public async Task Handle(ChatMessageNotificationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var channel = ChannelRepository.GetByID(request.BroadcasterId);
            if (channel is null)
            {
                Logger.Warning("Channel {BroadcasterId} not in database", request.BroadcasterId);
                return;
            }

            var user = await UserRepository.SearchID(request.ChatterId, cancellationToken);

            if (user.TwitchName != request.ChatterName)
            {
                user.TwitchName = request.ChatterName;
                user.UsernameHistory.Add(new(user.TwitchName, DateTime.Now));

                await UserRepository.SaveChanges(cancellationToken);
            }

            var input = ParseMessage(request.Message.Text);

            var isBroadcaster = user.TwitchID == channel.TwitchID;

            MessageReceivedCommand message = new ChatMessage(
                channel,
                user,
                input,
                isBroadcaster,
                request.IsMod,
                LifetimeScope,
                request.MessageId);

            await Bus.Publish(message, cancellationToken);
        }
        finally
        {
            MetricsTracker.TotalMessagesRead.WithLabels(request.BroadcasterName).Inc();
        }
    }
}

#endregion
