using System.Text.Json;
using System.Text.Json.Serialization;
using MediatR;

namespace Fumo.Shared.Eventsub;

public record EventsubSubscription(string Id, string Type, string Version, string Status, int Cost, object Condition, DateTime CreatedAt);

public record MessageTypeVerificationBody(string Challenge, EventsubSubscription Subscription);

public record MessageTypeNotificationBody(EventsubSubscription Subscription, JsonDocument Event);

public record MessageTypeRevocationBody(EventsubSubscription Subscription);

public record struct EventsubSubscriptionRequest<TCondition>(string UserId, EventsubType<TCondition> Type, TCondition Condition)
    where TCondition : class;

public class EventsubVerificationCommand<TCondition> : IRequest
{
    public TCondition Condition { get; set; }
}

#region channel.chat.message

public class ChannelChatMessageBody
{
    [JsonPropertyName("broadcaster_user_id")]
    public string BroadcasterId { get; set; }

    [JsonPropertyName("broadcaster_user_login")]
    public string BroadcasterLogin { get; set; }

    [JsonPropertyName("broadcaster_user_name")]
    public string BroadcasterName { get; set; }

    [JsonPropertyName("chatter_user_id")]
    public string ChatterId { get; set; }

    [JsonPropertyName("chatter_user_login")]
    public string ChatterLogin { get; set; }

    [JsonPropertyName("chatter_user_name")]
    public string ChatterName { get; set; }

    public string MessageId { get; set; }

    public ChatMessage Message { get; set; }

    public string Color { get; set; }

    public List<Badge> Badges { get; set; } = [];

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ChatMessageType MessageType { get; set; }

    public ChatReply? Reply { get; set; }

    public string? ChannelPointsCustomRewardId { get; set; }

    public string? ChannelPointsAnimationId { get; set; }

    public record Badge(string SetId, string Id, string Info);

    public record ChatReply(
        string ParentMessageId,
        string ParentMessageBody,
        string ParentUserId,
        string ParentUserName,
        string ParentUserLogin,
        string ThreadMessageId,
        string ThreadUserId,
        string ThreadUserName,
        string ThreadUserLogin
    );

    public enum ChatMessageType
    {
        Text,
        ChannelPointsHighlighted,
        ChannelPointsSubOnly,
        UserIntro,
        Animated,
        GigantifiedEmote
    }

    public record ChatMessage(string Text);

    #region Helpers

    public bool IsBroadcaster => Badges.Any(x => x.SetId == "broadcaster");

    public bool IsMod => Badges.Any(x => x.SetId == "moderator");

    #endregion
}

#endregion
