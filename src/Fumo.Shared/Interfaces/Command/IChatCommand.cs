using Fumo.Shared.Models;
using System.Collections.ObjectModel;

namespace Fumo.Shared.Interfaces.Command;

public interface IChatCommand
{
    /// <summary>
    /// Code that is executed when the command is ran
    /// </summary>
    /// <returns>
    /// A string that is outputted in chat
    /// </returns>
    public ValueTask<CommandResult> Execute(CancellationToken ct);

    public ValueTask BuildHelp(ChatCommandHelpBuilder builder, CancellationToken ct);
}
