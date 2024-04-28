namespace Fumo.Shared.ThirdParty.Emotes.SevenTV.Models;

public record SevenTVEmoteByName(List<SevenTVEmoteByNameItem> Items);

public class SevenTVEmoteByNameItem : SevenTVBaseTag
{
    public string ID { get; init; }
    public string Name { get; init; }
    public SevenTVEmoteByNameOwner Owner { get; init; }

    public SevenTVBasicEmote AsBasicEmote() => new(ID, Name);
}

public record SevenTVEmoteByNameOwner(string Username, string ID);

public class SevenTVBaseTag
{
    public List<string> Tags { get; init; }
}