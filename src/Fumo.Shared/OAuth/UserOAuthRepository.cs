using Fumo.Database;
using Fumo.Database.DTO;
using Fumo.Shared.Interfaces;

namespace Fumo.Shared.OAuth;

public class UserOAuthRepository : IUserOAuthRepository
{
    private readonly DatabaseContext Context;

    public UserOAuthRepository(DatabaseContext context)
    {
        Context = context;
    }

    public async ValueTask<UserOauthDTO?> Get(string twitchId, string provider, CancellationToken token = default)
        => await Context.UserOauth.FindAsync([twitchId, provider], token);

    public async ValueTask CreateOrUpdate(UserOauthDTO userOauth, CancellationToken token = default)
    {
        var exists = await Context.UserOauth.FindAsync([userOauth.TwitchID, userOauth.Provider], token);

        if (exists is not null)
            Context.UserOauth.Update(userOauth);
        else
            Context.UserOauth.Add(userOauth);

        await Context.SaveChangesAsync(token);
    }
}
