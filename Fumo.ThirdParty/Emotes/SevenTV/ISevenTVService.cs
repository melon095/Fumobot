using Fumo.ThirdParty.Emotes.SevenTV.Enums;
using Fumo.ThirdParty.Emotes.SevenTV.Models;

namespace Fumo.ThirdParty.Emotes.SevenTV;

public interface ISevenTVService
{
    ValueTask<SevenTVUser> GetUserInfo(string twitchID, CancellationToken ct = default!);

    ValueTask<SevenTVRoles> GetGlobalRoles(CancellationToken ct = default!);

    ValueTask<SevenTVEditorEmoteSets> GetEditorEmoteSetsOfUser(string twitchID, CancellationToken ct = default!);

    ValueTask<SevenTVEditors> GetEditors(string twitchID, CancellationToken ct = default!);

    ValueTask<SevenTVBasicEmote> SearchEmoteByID(string Id, CancellationToken ct = default!);

    ValueTask<SevenTVEmoteByName> SearchEmotesByName(string name, bool exact = false, CancellationToken ct = default!);

    ValueTask<string?> ModifyEmoteSet(string emoteSet, ListItemAction action, string emoteID, string? name = null, CancellationToken ct = default!);

    ValueTask<List<SevenTVEnabledEmote>> GetEnabledEmotes(string emoteSet, CancellationToken ct = default!);

    ValueTask ModifyEditorPermissions(string channelId, string userId, UserEditorPermissions permissions, CancellationToken ct = default!);
}
