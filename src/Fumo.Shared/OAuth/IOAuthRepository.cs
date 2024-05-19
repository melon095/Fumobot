using Fumo.Database.DTO;

namespace Fumo.Shared.OAuth;

public interface IOAuthRepository
{
    ValueTask<UserOauthDTO?> Get(string twitchId, string provider, CancellationToken token = default);

    ValueTask Update(UserOauthDTO userOauth, CancellationToken token = default);
}