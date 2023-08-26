using Fumo.Database;
using Fumo.Models;

namespace Fumo.Interfaces;

public interface ICommandHandler
{
    public Task<CommandResult> TryExecute(ChannelDTO channel, UserDTO user, string commandName, string[] input);
}
