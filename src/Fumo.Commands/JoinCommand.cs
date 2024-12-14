using Fumo.Shared.Models;
using Fumo.Shared.Enums;

namespace Fumo.Commands;

public class JoinCommand : ChatCommand
{
    private readonly Uri JoinURL;
    private readonly string BotID;

    public JoinCommand()
    {
        SetName("join");
        SetFlags(ChatCommandFlags.Reply);
    }

    public JoinCommand(AppSettings settings) : this()
    {
        JoinURL = new Uri(settings.Website.PublicURL, "/ask-join/");
        BotID = settings.Twitch.UserID;
    }

    public override ValueTask<CommandResult> Execute(CancellationToken ct)
    {
        if (Channel.TwitchID != BotID)
            return new(string.Empty);

        if (Input.Count > 0)
            return new($"Tell your friend to click here :) -> {JoinURL}");

        return new($"Click here :) -> {JoinURL}");
    }

    public override ValueTask BuildHelp(ChatCommandHelpBuilder builder, CancellationToken ct)
        => builder
            .WithCache()
            .WithDisplayName("join")
            .Finish;
}
