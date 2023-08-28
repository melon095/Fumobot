using Fumo.Database;
using Fumo.Interfaces;
using Fumo.Models;

namespace Fumo.Handlers;

public class CommandHandler : ICommandHandler
{
    public async Task<CommandResult> TryExecute(ChannelDTO channel, UserDTO user, string commandName, string[] input)
    {
        throw new NotImplementedException();
    }
}
