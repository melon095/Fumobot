using Fumo.Shared.Models;

namespace Fumo.Shared.Interfaces;

public interface ICooldownHandler
{
    public ValueTask<bool> IsOnCooldown(ChatMessage message, ChatCommand command);

    public ValueTask SetCooldown(ChatMessage message, ChatCommand command);
}
