using Fumo.Shared.Enums;
using Fumo.Shared.Models;
using Microsoft.Extensions.Configuration;

namespace Fumo.Commands;

public class HelpCommand : ChatCommand
{
    private readonly string PublicURL;

    public HelpCommand()
    {
        SetName("help");
        SetFlags(ChatCommandFlags.Reply | ChatCommandFlags.IgnoreBanphrase);
        SetCooldown(TimeSpan.FromSeconds(10));
    }

    public HelpCommand(AppSettings settings) : this()
    {
        PublicURL = settings.Website.PublicURL;
    }

    public override ValueTask<CommandResult> Execute(CancellationToken ct)
    {
        var url = new Uri(new Uri(PublicURL), "/commands/");

        return ValueTask.FromResult(new CommandResult
        {
            Message = url.ToString()
        });
    }

    public override ValueTask BuildHelp(ChatCommandHelpBuilder builder, CancellationToken ct)
        => builder
            .WithCache()
            .WithDisplayName("help")
            .Finish;
}
