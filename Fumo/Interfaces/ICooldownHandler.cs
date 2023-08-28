using Fumo.Models;

namespace Fumo.Interfaces;

public interface ICooldownHandler
{
    public Task<bool> IsOnCooldownAsync(ChatMessage message, ChatCommand command, CancellationToken ct);

    public Task SetCooldown(ChatMessage message, ChatCommand command, CancellationToken ct);
}
