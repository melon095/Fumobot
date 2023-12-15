using Fumo.Shared.Models;

namespace Fumo.Shared.Interfaces;

public interface ICooldownHandler
{
    public Task<bool> IsOnCooldown(ChatMessage message, ChatCommand command);

    public Task SetCooldown(ChatMessage message, ChatCommand command);
}
