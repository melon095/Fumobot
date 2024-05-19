using System.Text.Json;
using MiniTwitch.Helix.Responses;

namespace Fumo.Shared.Eventsub;

public record EventsubSubscription(string Id, string Type, string Version, string Status, int Cost, object Condition, DateTime CreatedAt);

public record MessageTypeVerificationBody(string Challenge, EventsubSubscription Subscription);

public record MessageTypeNotificationBody(EventsubSubscription Subscription, object Event);

public record MessageTypeRevocationBody(EventsubSubscription Subscription);