namespace Fumo.Shared.Eventsub;

public interface IEventsubManager
{
    Uri CallbackUrl { get; }

    ValueTask<bool> CheckSubscribeCooldown(string userId, IEventsubType type);

    ValueTask<bool> IsUserEligible(string userId, IEventsubType type, CancellationToken ct);

    ValueTask<bool> Subscribe<TCondition>(EventsubSubscriptionRequest<TCondition> request, CancellationToken ct)
        where TCondition : class;

    ValueTask<bool> IsSubscribed(IEventsubType type, string userId, CancellationToken ct);

    ValueTask<bool> IsSubscribed(IEventsubType type, CancellationToken ct);

    ValueTask<string?> GetConduitID(CancellationToken ct);

    ValueTask CreateConduit(CancellationToken ct);

    ValueTask<string> GetSecret();
}
