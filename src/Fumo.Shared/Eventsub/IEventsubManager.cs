namespace Fumo.Shared.Eventsub;

public interface IEventsubManager
{
    Uri CallbackUrl { get; }

    ValueTask<bool> CheckSubscribeCooldown(string userId, IEventsubType type);

    ValueTask<bool> IsUserEligible(string userId, IEventsubType type, CancellationToken ct);

    ValueTask<bool> Subscribe<TCondition>(EventsubSubscriptionRequest<TCondition> request, CancellationToken ct)
        where TCondition : class;

    ValueTask<string?> GetConduitID(CancellationToken ct);

    ValueTask CreateConduit(CancellationToken ct);

    ValueTask<string> GetSecret();

    ValueTask<bool> CheckSignature(string message, string signature);

    ValueTask HandleMessage(MessageTypeRevocationBody message, CancellationToken ct);

    ValueTask HandleMessage(MessageTypeNotificationBody message, CancellationToken ct);
}
