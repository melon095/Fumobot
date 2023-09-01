using Fumo.Database;
using Fumo.Models;

namespace Fumo.Interfaces;

public interface ICommandHandler
{
    public Task<CommandResult?> TryExecute(ChatMessage message, string commandName, CancellationToken cancellationToken);
}
