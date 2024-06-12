using Autofac;
using Fumo.Database;
using Fumo.Database.DTO;
using Fumo.Database.Extensions;
using Fumo.Shared.Interfaces;
using Fumo.Shared.MediatorCommands;
using Fumo.Shared.Models;
using MediatR;
using MiniTwitch.Irc;
using Serilog;

namespace Fumo.Shared.Eventsub;

#region Verification

[EventsubCommand(EventsubCommandType.Subscribed, "channel.chat.message")]
internal class ChannelChatMessageVerificationCommand : EventsubVerificationCommand<EventsubBasicCondition>;

internal class ChannelChatMessageVerificationCommandHandler : IRequestHandler<ChannelChatMessageVerificationCommand>
{
    private readonly ILogger Logger;
    private readonly IChannelRepository ChannelRepository;
    private readonly IUserRepository UserRepository;
    private readonly IrcClient IrcClient;

    public ChannelChatMessageVerificationCommandHandler(ILogger logger, IChannelRepository channelRepository, IUserRepository userRepository, IrcClient ircClient)
    {
        Logger = logger.ForContext<ChannelChatMessageVerificationCommandHandler>();
        ChannelRepository = channelRepository;
        UserRepository = userRepository;
        IrcClient = ircClient;
    }

    public async Task Handle(ChannelChatMessageVerificationCommand request, CancellationToken ct)
    {
        var user = await UserRepository.SearchID(request.Condition.BroadcasterId, ct);
        if (user is null)
        {
            Logger.Warning("User {BroadcasterId} not in database", request.Condition.BroadcasterId);
            return;
        }

        Logger.Information("Received chat message verification: {UserID} {UserName}", user.TwitchID, user.TwitchName);

        var channel = ChannelRepository.GetByID(user.TwitchID);
        if (channel is not null)
        {
            await IrcClient.PartChannel(channel.TwitchName, ct);
            channel.SetSetting(ChannelSettingKey.ConnectedWithEventsub, true.ToString());

            Logger.Information("Parted previous channel {ChannelName} from IRC", channel.TwitchName);
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
                ]
            };

            channel = await ChannelRepository.Create(newChannel, ct);
            channel.SetSetting(ChannelSettingKey.ConnectedWithEventsub, true.ToString());

            Logger.Information("Created new channel {ChannelName} in database", channel.TwitchName);
        }

        await ChannelRepository.Update(channel, ct);
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

            var input = request.Message.Text
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            var isBroadcaster = user.TwitchID == channel.TwitchID;

            MessageCommand message = new ChatMessage(
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
