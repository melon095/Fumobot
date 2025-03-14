using System.Collections.Immutable;
using Fumo.Database.DTO;
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

    ValueTask<SevenTVBotEditors> GetEditorEmoteSetsOfUser(string twitchID, CancellationToken ct = default!);

    ValueTask<SevenTVBasicEmote?> SearchEmoteByID(string Id, CancellationToken ct = default!);

    ValueTask<SevenTVEmoteByName> SearchEmotesByName(string name, bool exact = false, CancellationToken ct = default!);

    ValueTask<string?> AddEmote(string setID, string emoteID, string? alias = null, CancellationToken ct = default!);

    ValueTask RemoveEmote(string setID, SevenTVBasicEmote emote, CancellationToken ct = default!);

    ValueTask AliasEmote(string setID, SevenTVBasicEmote emote, string newName, CancellationToken ct = default!);

    ValueTask<IImmutableList<SevenTVBasicEmote>> GetEnabledEmotes(string emoteSet, CancellationToken ct = default!);

    ValueTask RemoveEditor(string channelId, string editorId, CancellationToken ct = default!);

    ValueTask AddEditor(string channelId, string editorId, CancellationToken ct = default!);
}
