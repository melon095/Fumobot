using Fumo.Shared.Exceptions;
using Fumo.Shared.Extensions;
using Fumo.Shared.Models;
using Fumo.Shared.Regexes;
using Fumo.Shared.ThirdParty.Emotes.SevenTV;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using Fumo.Shared.ThirdParty.Exceptions;
using Fumo.Shared.ThirdParty.Emotes.SevenTV.Enums;
using Fumo.Shared.ThirdParty.Emotes.SevenTV.Models;

namespace Fumo.Commands.SevenTV;

public class SevenTVAddCommand : ChatCommand
{
    private readonly ISevenTVService SevenTVService;

    public SevenTVAddCommand()
    {
        SetName("(7tv)?add");
        SetDescription("Adds a 7TV emote to the channel.");

        AddParameter(new(typeof(string), "alias"));
        AddParameter(new(typeof(bool), "exact"));
    }

    public SevenTVAddCommand(ISevenTVService sevenTVService) : this()
    {
        SevenTVService = sevenTVService;
    }

    private async ValueTask<SevenTVBasicEmote> ResolveEmote(string search, CancellationToken ct)
    {
        var id = ExtractSevenTVIDRegex.Extract(search);

        return id switch
        {
            string => await SevenTVService.SearchEmoteByID(id, ct),
            null => await GetEmoteFromName(search, ct)
        };
    }

    private async ValueTask<SevenTVBasicEmote> GetEmoteFromName(string search, CancellationToken ct)
    {
        var emotes = await SevenTVService.SearchEmotesByName(search, ct);

        if (emotes.Items.Count <= 0) throw new InvalidInputException("No emote found");

        return emotes.Items.ElementAt(0).AsBasicEmote();
    }

    public override async ValueTask<CommandResult> Execute(CancellationToken ct)
    {
        var aaaaa = await SevenTVService.EnsureCanModify(Channel, User);

        if (Input.Count <= 0) throw new InvalidInputException("You need to specify an emote to add");

        var search = Input[0].ToLowerInvariant();

        var emote = await ResolveEmote(search, ct);

        var emoteName = emote.Name;

        if (TryGetArgument<string>("alias", out var alias))
        {
            emoteName = alias;
        }

        try
        {
            var newEmote = await SevenTVService.ModifyEmoteSet(aaaaa.EmoteSet, ListItemAction.Add, emote.Id, emoteName, ct);
            return $"Added emote {newEmote}";
        }
        catch (GraphQLException ex)
        {
            if (SevenTVErrorMapper.TryErrorCodeFromGQL(ex, out var errorCode))
            {
                if (errorCode == SevenTVErrorCode.EmoteAlreadyEnabled)
                {
                    return $"Emote {emote.Name} is already enabled";
                }
            }

            return ex.Message;
        }
    }
}
