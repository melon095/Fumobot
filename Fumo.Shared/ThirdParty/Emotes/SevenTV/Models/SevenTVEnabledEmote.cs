using System.Text.Json.Serialization;

namespace Fumo.Shared.ThirdParty.Emotes.SevenTV.Models;

/// <param name="Name">
/// If the HasAlias method is true, Name will be the alias
/// </param>
/// <param name="Data">
/// Will always be the original name
/// </param>
public record SevenTVEnabledEmote(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("data")] SevenTVEnabledEmoteData Data
    )
{
    public bool HasAlias => !(Name == Data.Name);
}

public record SevenTVEnabledEmoteData(
    [property: JsonPropertyName("name")] string Name
    );
