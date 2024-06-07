namespace Fumo.Shared.Eventsub;

public record EventsubSubscription(string Id, string Type, string Version, string Status, int Cost, object Condition, DateTime CreatedAt);

public record MessageTypeVerificationBody(string Challenge, EventsubSubscription Subscription);

public record MessageTypeNotificationBody(EventsubSubscription Subscription, object Event);

public record MessageTypeRevocationBody(EventsubSubscription Subscription);

public record struct EventsubSubscriptionRequest<TCondition>(string UserId, EventsubType<TCondition> Type, TCondition Condition)
    where TCondition : class;