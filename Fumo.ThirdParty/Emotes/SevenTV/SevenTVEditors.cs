using System.Text.Json.Serialization;

namespace Fumo.ThirdParty.Emotes.SevenTV;

public record SevenTVEditorsEditor(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("user")] SevenTVEditorsUser User
);

public record SevenTVEditorsRoot(
    [property: JsonPropertyName("userByConnection")] SevenTVEditors UserByConnection
);

public record SevenTVEditorsUser(
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("connections")] IReadOnlyList<SevenTVConnection> Connections
);

public record SevenTVEditors(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("editors")] IReadOnlyList<SevenTVEditorsEditor> Editors
);
