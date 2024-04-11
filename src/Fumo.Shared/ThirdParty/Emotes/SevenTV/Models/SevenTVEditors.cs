namespace Fumo.Shared.ThirdParty.Emotes.SevenTV.Models;

public record SevenTVEditorsEditor(string ID, SevenTVEditorsUser User);

public record SevenTVEditorsUser(string Username, IReadOnlyList<SevenTVConnection> Connections);

public record SevenTVEditors(string ID, string Username, IReadOnlyList<SevenTVEditorsEditor> Editors);
