
using MiniTwitch.Helix.Responses;

namespace Fumo.Shared.Eventsub;

public interface IEventsubManager
{
    Uri CallbackUrl { get; }

    ValueTask<bool> CheckSubscribeCooldown(string userId, EventsubType type);

    ValueTask<bool> IsUserEligible(string userId, EventsubType type, CancellationToken ct);

    ValueTask Subscribe(string userId, EventsubType type, CancellationToken ct);

    ValueTask<Conduits.Conduit?> GetConduit(CancellationToken ct);

    ValueTask CreateConduit(CancellationToken ct);

    ValueTask<string> GetSecret();

    ValueTask<bool> CheckSignature(string message, string signature);

    ValueTask HandleMessage(MessageTypeRevocationBody message, CancellationToken ct);

    ValueTask HandleMessage(MessageTypeNotificationBody message, CancellationToken ct);
}
