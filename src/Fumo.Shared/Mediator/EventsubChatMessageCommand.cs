using Autofac;
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
using Serilog.Events;
using SerilogTracing;

namespace Fumo.Shared.Mediator;

[EventsubCommand(EventsubCommandType.Subscribed, "channel.chat.message")]
public record ChannelChatMessageSubscribedCommand(CreatedSubscription.Info Info)
    : EventsubVerificationCommand<EventsubBasicCondition>(Info.Condition);

[EventsubCommand(EventsubCommandType.Notification, "channel.chat.message")]
public class ChatMessageNotificationCommand : ChannelChatMessageBody, INotification;
