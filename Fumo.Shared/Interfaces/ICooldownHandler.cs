using Fumo.Models;

namespace Fumo.Interfaces;

public interface ICooldownHandler
{
    public Task<bool> IsOnCooldownAsync(ChatMessage message, ChatCommand command);

    public Task SetCooldownAsync(ChatMessage message, ChatCommand command);
}
