using Fumo.Shared.Exceptions;
using Fumo.Shared.Models;
using Fumo.Shared.Regexes;
using Fumo.Shared.ThirdParty.Emotes.SevenTV;
using Fumo.Shared.ThirdParty.Exceptions;
using Fumo.Shared.ThirdParty.Emotes.SevenTV.Models;

namespace Fumo.Commands.SevenTV;

public class SevenTVAddCommand : ChatCommand
{
    protected override List<Parameter> Parameters =>
    [
        new(typeof(string), "alias"),
    ];

    public override ChatCommandMetadata Metadata => new()
    {
        Name = "(7tv)?add",
        Description = "Adds a 7TV emote to the channel.",
    };

    private readonly ISevenTVService SevenTVService;

    public SevenTVAddCommand(ISevenTVService sevenTVService)
    {
        SevenTVService = sevenTVService;
    }

    private async ValueTask<SevenTVBasicEmote> ResolveEmote(string search, CancellationToken ct)
    {
        var id = ExtractSevenTVIDRegex.Extract(search);

        if (id is null)
            return await GetEmoteFromName(search, ct);

        var emote = await SevenTVService.SearchEmoteByID(id, ct);

        return emote is null
            ? throw new InvalidInputException("No emote found")
            : emote;
    }

    private async ValueTask<SevenTVBasicEmote> GetEmoteFromName(string search, CancellationToken ct)
    {
        var emotes = await SevenTVService.SearchEmotesByName(search, ct: ct);

        if (emotes.Items.Count <= 0)
            throw new InvalidInputException("No emote found");

        return emotes.Items.ElementAt(0).AsBasicEmote();
    }

    public override async ValueTask<CommandResult> Execute(CancellationToken ct)
    {
        var aaaaa = await SevenTVService.EnsureCanModify(Channel, User);

        if (Input.Count <= 0) throw new InvalidInputException("You need to specify an emote to add");

        var search = Input[0];

        var emote = await ResolveEmote(search, ct);

        var emoteName = emote.Name;

        if (TryGetArgument<string>("alias", out var alias))
        {
            emoteName = alias;
        }

        try
        {
            var newEmote = await SevenTVService.AddEmote(aaaaa.EmoteSet, emote.ID, emoteName, ct);
            return $"Added emote {newEmote}";
        }
        catch (GraphQLException ex)
        {
            if (ex.Message == SevenTVErrors.AddEmoteNameConflict)
            {
                return $"The emote is already enabled with this name.";
            }

            return ex.Message;
        }
    }

    public override ValueTask BuildHelp(ChatCommandHelpBuilder builder, CancellationToken ct)
        => builder
            .WithCache()
            .WithDisplayName("add")
            .WithDescription("Add a 7TV emote")
            .WithUsage((x) => x.Required("emote"))
            .WithExample("FloppaL", $"Finds the most popular emote called FloppaL")
            .WithExample("60aeab8df6a2c3b332d21139", "Emotes can be added with ID")
            .WithExample("https://7tv.app/emotes/60aeab8df6a2c3b332d21139", "They can also be added by URL")
            .WithArgument("alias", (x) =>
            {
                x.Description = "Assign an alias to this emote";
                x.Required("alias");
            })
            .Finish;
}
