using Fumo.Shared.Exceptions;
using Fumo.Shared.Interfaces;
using Fumo.Shared.Models;
using Fumo.Shared.Regexes;
using Fumo.Shared.ThirdParty.Emotes.SevenTV;
using Fumo.Shared.ThirdParty.Exceptions;
using System.Text;
using Fumo.Shared.ThirdParty.Emotes.SevenTV.Models;

namespace Fumo.Commands.SevenTV;

public class SevenTVSearchCommand : ChatCommand
{
    private readonly int MaxEmoteOutput = 5;

    private readonly ISevenTVService SevenTV;
    private readonly IUserRepository UserRepository;

    public SevenTVSearchCommand()
    {
        SetName("7tv$|search");
        SetDescription("Search 7TV emotes");

        AddParameter(new(typeof(string), "uploader"));
        AddParameter(new(typeof(bool), "exact"));
    }

    public SevenTVSearchCommand(ISevenTVService sevenTV, IUserRepository userRepository) : this()
    {
        SevenTV = sevenTV;
        UserRepository = userRepository;
    }

    private async ValueTask<CommandResult> GetEmoteFromName(string searchTerm, CancellationToken ct)
    {
        var exact = GetArgument<bool>("exact");

        var emotes = await SevenTV.SearchEmotesByName(searchTerm, exact, ct);

        if (Check() is string result)
        {
            return result;
        }

        {
            if (TryGetArgument<string>("uploader", out var uploader))
            {
                await FilterByUploader(emotes.Items, UsernameCleanerRegex.CleanUsername(uploader), ct);
            }

            if (Check() is string result2)
            {
                return result2;
            }
        }

        {
            if (!exact) FilterTags(searchTerm, emotes.Items);

            if (Check() is string result2)
            {
                return result2;
            }
        }

        StringBuilder builder = new();

        var emotesToDisplay = emotes.Items.Take(MaxEmoteOutput);

        foreach (var emote in emotesToDisplay)
        {
            builder.Append($"{emote.Name} - https://7tv.app/emotes/{emote.ID}");

            if (emote != emotesToDisplay.Last())
            {
                builder.Append(" | ");
            }
        }

        return builder.ToString();

        string? Check()
            => (emotes.Items.Count) switch
            {
                1 => $"{emotes.Items[0].Name} - https://7tv.app/emotes/{emotes.Items[0].ID}",
                0 => "No emotes found",
                _ => null,
            };
    }

    private async ValueTask<CommandResult> GetEmoteFromID(string id, CancellationToken ct)
    {
        try
        {
            var emote = await SevenTV.SearchEmoteByID(id, ct);

            if (emote is null)
            {
                return $"No emote with the ID of {id} found";
            }

            return $"{emote.Name} - https://7tv.app/emotes/{emote.ID}";
        }
        catch (GraphQLException ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    private async ValueTask FilterByUploader(List<SevenTVEmoteByNameItem> emotes, string uploader, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(uploader))
            return;

        var user = await UserRepository.SearchName(uploader, ct);
        var seventvUser = await SevenTV.GetUserInfo(user.TwitchID, ct);

        emotes.RemoveAll(x => x.Owner.ID != seventvUser.ID);
    }

    private static void FilterTags(string searchTerm, List<SevenTVEmoteByNameItem> emotes)
    {
        // FIXME: This should be removed, once 7TV has a propery search.
        // Filter the "emotes" list such that items that do not have the "searchTerm" in their tags are placed on the top of the list
        emotes.Sort((x, y) =>
        {
            var xTags = x.Tags;
            var yTags = y.Tags;

            bool xContains = xTags.Contains(searchTerm);
            bool yContains = yTags.Contains(searchTerm);

            if (xContains && !yContains)
            {
                return 1;
            }
            else if (!xContains && yContains)
            {
                return -1;
            }

            return 0;
        });
    }

    public override async ValueTask<CommandResult> Execute(CancellationToken ct)
    {
        var searchTerm = Input.ElementAtOrDefault(0) ?? throw new InvalidInputException("Missing a search term");

        var potentialID = ExtractSevenTVIDRegex.Extract(searchTerm);

        return potentialID switch
        {
            string id => await GetEmoteFromID(id, ct),
            null => await GetEmoteFromName(searchTerm, ct),
        };
    }

    public override ValueTask BuildHelp(ChatCommandHelpBuilder builder, CancellationToken ct)
        => builder
            .WithCache()
            .WithDisplayName("search")
            .WithDescription("Search 7TV emotes")
            .WithUsage((x) => x.Required("search_term"))
            .WithExample("Apu")
            .WithExample("60aeab8df6a2c3b332d21139")
            .WithArgument("exact", (x) =>
            {
                x.Description = SevenTVConstants.Description.ExactFlag;
            })
            .WithArgument("uploader", (x) =>
            {
                x.Description = "Search based on uploader. Uses the current Twitch username!";
                x.Required("twitch_name");
            })
            .Finish;
}
