namespace Fumo.ThirdParty.Emotes.SevenTV;

public interface ISevenTVService
{
    Task<SevenTVUser> GetUserInfo(string twitchID, CancellationToken ct = default!);

    Task<SevenTVRoles> GetGlobalRoles(CancellationToken ct = default!);

    Task<SevenTVEditorEmoteSets> GetEditorEmoteSetsOfUser(string twitchID, CancellationToken ct = default!);

    Task<SevenTVEditors> GetEditors(string twitchID, CancellationToken ct = default!);
}
