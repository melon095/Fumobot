namespace Fumo.ThirdParty.Emotes.SevenTV;

public interface ISevenTVService
{
    Task<SevenTVUser> GetUserInfo(string twitchID, CancellationToken ct = default!);

    Task<SevenTVRoles> GetGlobalRoles(CancellationToken ct = default!);
}
