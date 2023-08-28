
using Fumo.Interfaces;
using Fumo.Models;

namespace Fumo.Handlers;

public class CooldownHandler : ICooldownHandler
{
    public Task<bool> IsOnCooldownAsync(ChatMessage message, ChatCommand command, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task SetCooldown(ChatMessage message, ChatCommand command, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
