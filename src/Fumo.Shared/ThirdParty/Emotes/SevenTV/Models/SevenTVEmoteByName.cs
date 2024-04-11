namespace Fumo.Shared.ThirdParty.Emotes.SevenTV.Models;

public record SevenTVEmoteByName(List<SevenTVEmoteByNameItem> Items);

public record SevenTVEmoteByNameItem(
    string ID,
    string Name,
    SevenTVEmoteByNameOwner Owner,
    List<string> Tags
)
{
    public SevenTVBasicEmote AsBasicEmote() => new(ID, Name);
}

public record SevenTVEmoteByNameOwner(string Username, string ID);
