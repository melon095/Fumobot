using Fumo.Shared.Enums;
using Fumo.Shared.Exceptions;
using Fumo.Shared.Models;
using Fumo.Shared.ThirdParty.Emotes.SevenTV;
using Fumo.Shared.ThirdParty.Exceptions;

namespace Fumo.Commands.SevenTV;

public class SevenTVAliasCommand : ChatCommand
{
    public override ChatCommandMetadata Metadata => new()
    {
        Name = "(7tv)?alias",
        Description = "Set or Reset the alias of an emote",
        Flags = ChatCommandFlags.Reply,
    };

    private readonly ISevenTVService SevenTVService;

    public SevenTVAliasCommand(ISevenTVService sevenTVService)
    {
        SevenTVService = sevenTVService;
    }

    public override async ValueTask<CommandResult> Execute(CancellationToken ct)
    {
        var (EmoteSet, _) = await SevenTVService.EnsureCanModify(Channel, User);

        var input = Input.ElementAtOrDefault(0)
            ?? throw new InvalidInputException("Missing source emote");

        var srcEmote = (await SevenTVService.GetEnabledEmotes(EmoteSet, ct))
            .Where(x => x.Name == input)
            .SingleOrDefault();

        if (srcEmote is null)
        {
            return $"{input} is not an emote";
        }

        // 1. Given alias
        // 2. Source emote for reset
        var dstEmoteName = Input.ElementAtOrDefault(1)
            ?? srcEmote.Name;

        try
        {
            await SevenTVService.AliasEmote(EmoteSet, srcEmote, dstEmoteName, ct);
        }
        catch (GraphQLException ex)
        {
            if (ex.Message == SevenTVErrors.UpdateAliasNameConflict)
            {
                return "The emote is already enabled with this name.";
            }

            throw;
        }

        if (dstEmoteName is null)
        {
            return $"I reset the alias of {srcEmote.Name}";
        }

        return $"I set the alias of {srcEmote.Name} to {dstEmoteName}";
    }

    public override ValueTask BuildHelp(ChatCommandHelpBuilder builder, CancellationToken ct)
        => builder
            .WithCache()
            .WithDisplayName("alias")
            .WithDescription("Set or reset the alias of an emote")
            .WithUsage((x) => x.Required("current_emote_name").Optional("alias"))
            .WithExample("FloppaL xqcL", "Assigns a new alias")
            .WithExample("FloppaL", "Removes the alias, if it exists")
            .Finish;
}
