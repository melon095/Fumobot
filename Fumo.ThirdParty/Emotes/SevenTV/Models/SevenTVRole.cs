using System.Text.Json.Serialization;

namespace Fumo.ThirdParty.Emotes.SevenTV.Models;

public record SevenTVRole(
    [property: JsonPropertyName("id")] string ID,
    [property: JsonPropertyName("name")] string Name);

public record SevenTVRoles(
    [property: JsonPropertyName("roles")] IReadOnlyList<SevenTVRole> Roles);