using System.Text.Json.Serialization;

namespace Fumo.Shared.ThirdParty.Emotes.SevenTV.Models;

public record OuterSevenTVUser(SevenTVUser UserByConnection);

public record SevenTVUserEmote(string ID);

public record SevenTVUserEmoteSet(string ID, IReadOnlyList<SevenTVUserEmote> Emotes, int Capacity);

public record SevenTVUser(
    string ID,
    string Type,
    string Username,
    IReadOnlyList<string> Roles,
    [property: JsonPropertyName("created_at")] DateTime CreatedAt,
    IReadOnlyList<SevenTVConnection> Connections,
    [property: JsonPropertyName("emote_sets")] IReadOnlyList<SevenTVUserEmoteSet> EmoteSets
)
{
    public SevenTVUserEmoteSet? DefaultEmoteSet()
    {
        var id = Connections.First(x => x.Platform == "TWITCH").EmoteSetId;

        return EmoteSets.FirstOrDefault(x => x.ID == id);
    }

    public bool TryDefaultEmoteSet(out SevenTVUserEmoteSet result)
    {
        var id = Connections.FirstOrDefault(x => x.Platform == "TWITCH")?.EmoteSetId;
        if (id is null)
        {
            result = null!;
            return false;
        }

        var emoteSet = EmoteSets.FirstOrDefault(x => x.ID == id);
        if (emoteSet is null)
        {
            result = null!;
            return false;
        }

        result = emoteSet;
        return true;
    }

    public bool IsDeletedUser()
        => Username == "*DeletedUser" || ID.All(x => x == '0');
}
