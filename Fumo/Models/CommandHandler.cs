using Fumo.Database;
using Fumo.Interfaces;

namespace Fumo.Models;

public class CommandHandler : ICommandHandler
{
    public Task<CommandResult> TryExecute(ChannelDTO channel, UserDTO user, string commandName, string[] input)
    {
        throw new NotImplementedException();
    }
}
