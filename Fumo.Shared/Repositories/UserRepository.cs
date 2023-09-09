using Autofac.Core;
using Fumo.Database;
using Fumo.Database.DTO;
using Fumo.Exceptions;
using Fumo.Interfaces;
using Fumo.Shared.Regexes;
using Fumo.ThirdParty.ThreeLetterAPI;
using Fumo.ThirdParty.ThreeLetterAPI.Instructions;
using Fumo.ThirdParty.ThreeLetterAPI.Response;
using Microsoft.EntityFrameworkCore;

namespace Fumo.Shared.Repositories;

public class UserRepository : IUserRepository
{

    public DatabaseContext Database { get; }

    // FIXME: In memory caching

    public IThreeLetterAPI ThreeLetterAPI { get; }

    // FIXME: This is to prevent concurrently writing to the database, and no it's not a pretty way of doing it.
    public SemaphoreSlim Semaphore { get; } = new(1, 1);

    public UserRepository(DatabaseContext database, IThreeLetterAPI threeLetterAPI)
    {
        Database = database;
        ThreeLetterAPI = threeLetterAPI;
    }

    private async Task<UserDTO?> SearchWithThreeLetterAPI(string? id = null, string? login = null, CancellationToken cancellationToken = default)
    {
        var tlaUser = await ThreeLetterAPI.SendAsync<BasicUserResponse>(new BasicUserInstruction(id, login), cancellationToken);

        if (tlaUser is null || tlaUser.User is null)
        {
            return null;
        }

        UserDTO user = new()
        {
            TwitchID = tlaUser.User.ID,
            TwitchName = tlaUser.User.Login,
        };

        try
        {
            await Semaphore.WaitAsync(TimeSpan.FromSeconds(5));

            await Database.Users.AddAsync(user, cancellationToken);
            await Database.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            Semaphore.Release();
        }

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

    public async Task<List<UserDTO>> SearchMultipleByIDAsync(IEnumerable<string> ids, CancellationToken cancellation = default)
    {
        var dbUsers = await Database.Users
            .Where(x => ids.Contains(x.TwitchID))
            .ToListAsync(cancellation);

        var missing = ids.Except(dbUsers.Select(x => x.TwitchID)).ToArray();

        if (missing.Length <= 0)
        {
            return dbUsers;
        }

        BasicBatchUserInstruction request = new(missing);

        var response = await ThreeLetterAPI.SendAsync<BasicBatchUserResponse>(request, cancellation);

        // create dto objects from every object in response
        foreach (var twitchUser in response.Users)
        {
            UserDTO user = new()
            {
                TwitchID = twitchUser.ID,
                TwitchName = twitchUser.Login
            };

            await Database.Users.AddAsync(user, cancellation);
            dbUsers.Add(user);
        }

        await Database.SaveChangesAsync(cancellation);

        return dbUsers;
    }
}
