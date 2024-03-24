using Fumo.Shared.Enums;
using Fumo.Shared.Exceptions;
using Fumo.Shared.Extensions;
using Fumo.Shared.Models;
using Fumo.Shared.ThirdParty.Emotes.SevenTV;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using Fumo.Shared.ThirdParty.Emotes.SevenTV.Enums;

namespace Fumo.Commands.SevenTV;

public class SevenTVAliasCommand : ChatCommand
{
    private readonly ISevenTVService SevenTVService;

    public SevenTVAliasCommand()
    {
        SetName("(7tv)?alias");
        SetDescription("Set or Reset the alias of an emote");
        SetFlags(ChatCommandFlags.Reply);
    }

    public SevenTVAliasCommand(ISevenTVService sevenTVService) : this()
    {
        SevenTVService = sevenTVService;
    }

    public override async ValueTask<CommandResult> Execute(CancellationToken ct)
    {
        var (EmoteSet, _) = await SevenTVService.EnsureCanModify(Channel, User);

        var input = Input.ElementAtOrDefault(0) ?? throw new InvalidInputException("Missing source emote");

        var srcEmote = (await SevenTVService.GetEnabledEmotes(EmoteSet, ct))
            .Where(x => x.Name == input)
            .SingleOrDefault();

        if (srcEmote is null)
        {
            return $"{input} is not an emote";
        }

        var dstEmoteName = Input.ElementAtOrDefault(1);

        await SevenTVService.ModifyEmoteSet(EmoteSet, ListItemAction.Update, srcEmote.Id, dstEmoteName, ct);

        if (dstEmoteName is null)
        {
            return $"I reset the alias of {srcEmote.Name}";
        }

        return $"I set the alias of {srcEmote.Name} to {dstEmoteName}";
    }
}
