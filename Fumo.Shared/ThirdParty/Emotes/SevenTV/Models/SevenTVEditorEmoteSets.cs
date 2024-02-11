using System.Text.Json.Serialization;

namespace Fumo.Shared.ThirdParty.Emotes.SevenTV.Models;

public record SevenTVEditorEditorOf(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("user")] SevenTVEditorUser User
);

public record SevenTVEditorUser(
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("connections")] IReadOnlyList<SevenTVConnection> Connections
);

public record SevenTVEditorEmoteSets(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("connections")] IReadOnlyList<SevenTVConnection> Connections,
    [property: JsonPropertyName("editor_of")] IReadOnlyList<SevenTVEditorEditorOf> EditorOf
);
