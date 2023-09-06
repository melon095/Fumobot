
using System.Text.Json.Serialization;

namespace Fumo.ThirdParty.Emotes.SevenTV;

public record SevenTVEmoteByName(
    [property: JsonPropertyName("items")] List<SevenTVEmoteByNameItem> Items
);

public record SevenTVEmoteByNameItem(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("owner")] SevenTVEmoteByNameOwner Owner
);

public record SevenTVEmoteByNameOwner(
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("id")] string Id
);
