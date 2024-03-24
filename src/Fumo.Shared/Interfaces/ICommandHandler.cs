using Fumo.Shared.Models;

namespace Fumo.Shared.Interfaces;

public interface ICommandHandler
{
    public ValueTask<CommandResult?> TryExecute(ChatMessage message, string commandName,
        CancellationToken cancellationToken);
}
