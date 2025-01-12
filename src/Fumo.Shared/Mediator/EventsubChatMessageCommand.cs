using Fumo.Shared.Eventsub;
using MediatR;
using MiniTwitch.Helix.Responses;

namespace Fumo.Shared.Mediator;

[EventsubCommand(EventsubCommandType.Subscribed, "channel.chat.message")]
public record ChannelChatMessageSubscribedCommand(CreatedSubscription.Info Info)
    : EventsubVerificationCommand<EventsubBasicCondition>(Info.Condition);

[EventsubCommand(EventsubCommandType.Notification, "channel.chat.message")]
public class ChatMessageNotificationCommand : ChannelChatMessageBody, INotification;
