using System.Text.Json.Serialization;

namespace Fumo.Shared.ThirdParty.Emotes.SevenTV.Models;

public record SevenTVEmoteByName(
    [property: JsonPropertyName("items")] List<SevenTVEmoteByNameItem> Items
);

public record SevenTVEmoteByNameItem(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("owner")] SevenTVEmoteByNameOwner Owner
)
{
    public SevenTVBasicEmote AsBasicEmote() => new(Id, Name);
}

public record SevenTVEmoteByNameOwner(
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("id")] string Id
);
