using System.Text.Json.Serialization;

namespace Fumo.ThirdParty.Emotes.SevenTV.Models;

public record SevenTVBasicEmote(
[property: JsonPropertyName("id")] string Id,
[property: JsonPropertyName("name")] string Name
);
