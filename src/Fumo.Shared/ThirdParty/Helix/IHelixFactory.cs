using MiniTwitch.Helix;

namespace Fumo.Shared.ThirdParty.Helix;

public interface IHelixFactory
{
    ValueTask<HelixWrapper> Create(CancellationToken ct);
}
