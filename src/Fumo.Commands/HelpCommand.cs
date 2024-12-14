using Fumo.Shared.Enums;
using Fumo.Shared.Models;
using Microsoft.Extensions.Configuration;

namespace Fumo.Commands;

public class HelpCommand : ChatCommand
{
    private readonly string CommandUrl;


    public HelpCommand()
    {
        SetName("help");
        SetFlags(ChatCommandFlags.Reply | ChatCommandFlags.IgnoreBanphrase);
        SetCooldown(TimeSpan.FromSeconds(10));
    }

    public HelpCommand(AppSettings settings) : this()
    {
        CommandUrl = new Uri(settings.Website.PublicURL, "/commands/").ToString();
    }

    public override ValueTask<CommandResult> Execute(CancellationToken ct)
        => new(CommandUrl);

    public override ValueTask BuildHelp(ChatCommandHelpBuilder builder, CancellationToken ct)
        => builder
            .WithCache()
            .WithDisplayName("help")
            .Finish;
}
