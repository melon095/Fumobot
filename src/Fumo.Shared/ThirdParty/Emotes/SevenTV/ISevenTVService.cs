using Fumo.Database.DTO;
using Fumo.Shared.ThirdParty.Emotes.SevenTV.Enums;
using Fumo.Shared.ThirdParty.Emotes.SevenTV.Models;


namespace Fumo.Shared.ThirdParty.Emotes.SevenTV;

public record struct SevenTVPermissionCheckResult(string EmoteSet, string UserID);

public interface ISevenTVService
{
    /// <summary>
    /// Ensures the current user is allowed to change emotes in the channel
    /// </summary>
    ValueTask<SevenTVPermissionCheckResult> EnsureCanModify(ChannelDTO channel, UserDTO invoker);

    ValueTask<SevenTVUser> GetUserInfo(string twitchID, CancellationToken ct = default!);

    ValueTask<SevenTVRoles> GetGlobalRoles(CancellationToken ct = default!);

    ValueTask<SevenTVEditorEmoteSets> GetEditorEmoteSetsOfUser(string twitchID, CancellationToken ct = default!);

    ValueTask<SevenTVEditors> GetEditors(string twitchID, CancellationToken ct = default!);

    ValueTask<SevenTVBasicEmote?> SearchEmoteByID(string Id, CancellationToken ct = default!);

    ValueTask<SevenTVEmoteByName> SearchEmotesByName(string name, bool exact = false, CancellationToken ct = default!);

    ValueTask<string?> ModifyEmoteSet(string emoteSet, ListItemAction action, string emoteID, string? name = null, CancellationToken ct = default!);

    ValueTask<List<SevenTVEnabledEmote>> GetEnabledEmotes(string emoteSet, CancellationToken ct = default!);

    ValueTask ModifyEditorPermissions(string channelId, string userId, UserEditorPermissions permissions, CancellationToken ct = default!);
}
