using System.Text.Json.Serialization;

namespace Fumo.Shared.ThirdParty.Emotes.SevenTV.Models;

public record SevenTVEditorEditorOf(string ID, SevenTVEditorUser User);

public record SevenTVEditorUser(string Username, IReadOnlyList<SevenTVConnection> Connections);

public record SevenTVEditorEmoteSets(
    string ID,
    string Username,
    IReadOnlyList<SevenTVConnection> Connections,
    [property: JsonPropertyName("editor_of")] IReadOnlyList<SevenTVEditorEditorOf> EditorOf
);
