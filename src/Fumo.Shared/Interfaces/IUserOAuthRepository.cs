using Fumo.Database.DTO;

namespace Fumo.Shared.Interfaces;

public interface IUserOAuthRepository
{
    ValueTask<UserOauthDTO?> Get(string twitchId, string provider, CancellationToken token = default);

    ValueTask CreateOrUpdate(UserOauthDTO userOauth, CancellationToken token = default);
}