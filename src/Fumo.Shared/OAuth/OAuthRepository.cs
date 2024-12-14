using Fumo.Database;
using Fumo.Database.DTO;

namespace Fumo.Shared.OAuth;

public class OAuthRepository : IOAuthRepository
{
    private readonly DatabaseContext Context;

    public OAuthRepository(DatabaseContext context)
    {
        Context = context;
    }

    public async ValueTask<UserOauthDTO?> Get(string twitchId, string provider, CancellationToken token = default)
        => await Context.UserOauth.FindAsync([twitchId, provider], token);

    public async ValueTask Update(UserOauthDTO userOauth, CancellationToken token = default)
    {
        var exists = await Get(userOauth.TwitchID, userOauth.Provider, token);

        if (exists is not null)
            Context.UserOauth.Update(userOauth);
        else
            Context.UserOauth.Add(userOauth);

        await Context.SaveChangesAsync(token);
    }
}
