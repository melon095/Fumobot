using Fumo.Database;
using Fumo.Database.DTO;
using Fumo.Exceptions;
using Fumo.Interfaces;
using Fumo.Shared.Regexes;
using Fumo.ThirdParty.ThreeLetterAPI;
using Fumo.ThirdParty.ThreeLetterAPI.Instructions;
using Fumo.ThirdParty.ThreeLetterAPI.Response;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Fumo.Shared.Repositories;

public class UserRepository : IUserRepository
{

    public DatabaseContext Database { get; }

    // FIXME: In memory caching

    public IThreeLetterAPI ThreeLetterAPI { get; }

    public UserRepository(DatabaseContext database, IThreeLetterAPI threeLetterAPI)
    {
        Database = database;
        ThreeLetterAPI = threeLetterAPI;
    }

    private async Task<UserDTO?> SearchWithThreeLetterAPI(string? id = null, string? login = null, CancellationToken cancellationToken = default)
    {
        var tlaUser = await ThreeLetterAPI.SendAsync<BasicUserResponse>(new BasicUserInstruction(id, login), cancellationToken);

        if (tlaUser is null)
        {
            return null;

        }

        UserDTO user = new()
        {
            TwitchID = tlaUser.User.ID,
            TwitchName = tlaUser.User.Login,
        };

        await Database.Users.AddAsync(user, cancellationToken);
        await Database.SaveChangesAsync(cancellationToken);

        return user;
    }

    /// <inheritdoc/>
    public async Task<UserDTO> SearchIDAsync(string id, CancellationToken cancellationToken = default)
    {
        var dbUser = await Database
            .Users
            .Where(x => x.TwitchID.Equals(id))
            .SingleOrDefaultAsync(cancellationToken);

        if (dbUser is not null)
        {
            return dbUser;
        }

        return await SearchWithThreeLetterAPI(id, cancellationToken: cancellationToken) switch
        {
            null => throw new UserNotFoundException($"The user with the id {id} does not exist"),
            var user => user,
        };
    }

    /// <inheritdoc/>
    public async Task<UserDTO> SearchNameAsync(string username, CancellationToken cancellationToken = default)
    {
        var cleanedUsername = UsernameCleanerRegex.CleanUsername(username);

        var dbUser = await Database.Users
            .Where(x => x.TwitchName.Equals(cleanedUsername))
            .SingleOrDefaultAsync(cancellationToken);

        if (dbUser is not null)
        {
            return dbUser;
        }

        return await SearchWithThreeLetterAPI(login: cleanedUsername, cancellationToken: cancellationToken) switch
        {
            null => throw new UserNotFoundException($"The user with the name {cleanedUsername} does not exist"),
            var user => user,
        };
    }
}
