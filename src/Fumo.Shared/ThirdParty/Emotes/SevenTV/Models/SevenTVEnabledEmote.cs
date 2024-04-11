namespace Fumo.Shared.ThirdParty.Emotes.SevenTV.Models;

/// <param name="Name">
/// If the HasAlias method is true, Name will be the alias
/// </param>
/// <param name="Data">
/// Will always be the original name
/// </param>
public record SevenTVEnabledEmote(string ID, string Name, SevenTVEnabledEmoteData Data)
{
    public bool HasAlias => !(Name == Data.Name);
}

public record SevenTVEnabledEmoteData(string Name);
