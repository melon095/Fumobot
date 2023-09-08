namespace Fumo.ThirdParty.Emotes.SevenTV;

public interface ISevenTVService
{
    Task<SevenTVUser> GetUserInfo(string twitchID, CancellationToken ct = default!);

    Task<SevenTVRoles> GetGlobalRoles(CancellationToken ct = default!);

    Task<SevenTVEditorEmoteSets> GetEditorEmoteSetsOfUser(string twitchID, CancellationToken ct = default!);

    Task<SevenTVEditors> GetEditors(string twitchID, CancellationToken ct = default!);

    Task<SevenTVBasicEmote> SearchEmoteByID(string Id, CancellationToken ct = default!);

    Task<SevenTVEmoteByName> SearchEmotesByName(string name, bool exact = false, CancellationToken ct = default!);

    Task<string?> ModifyEmoteSet(string emoteSet, ListItemAction action, string emoteID, string? name = null, CancellationToken ct = default!);

    Task<List<SevenTVEnabledEmote>> GetEnabledEmotes(string emoteSet, CancellationToken ct = default!);

    Task ModifyEditorPermissions(string channelId, string userId, UserEditorPermissions permissions, CancellationToken ct = default!);
}
