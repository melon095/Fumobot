using Fumo.Shared.Models;

namespace Fumo.Shared.Interfaces;

public interface ICooldownHandler
{
    public Task<bool> IsOnCooldownAsync(ChatMessage message, ChatCommand command);

    public Task SetCooldownAsync(ChatMessage message, ChatCommand command);
}
