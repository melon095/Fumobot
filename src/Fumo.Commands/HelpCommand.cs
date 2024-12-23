using Fumo.Shared.Enums;
using Fumo.Shared.Models;
using Microsoft.Extensions.Configuration;

namespace Fumo.Commands;

public class HelpCommand : ChatCommand
{
    protected override ChatCommandMetadata Metadata => new()
    {
        Name = "help",
        Description = "Get a list of all available commands",
        Flags = ChatCommandFlags.Reply | ChatCommandFlags.IgnoreBanphrase,
        Cooldown = TimeSpan.FromSeconds(10)
    };

    private readonly string CommandUrl;

    public HelpCommand(AppSettings settings)
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
