using Fumo.Models;

namespace Fumo.Shared.Interfaces;

public interface ICommandHandler
{
    public Task<CommandResult?> TryExecute(ChatMessage message, string commandName, CancellationToken cancellationToken);
}
