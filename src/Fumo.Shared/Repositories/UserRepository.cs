using Fumo.Database;
using Fumo.Database.DTO;
using Fumo.Shared.Exceptions;
using Fumo.Shared.Regexes;
using Fumo.Shared.ThirdParty.ThreeLetterAPI;
using Fumo.Shared.ThirdParty.ThreeLetterAPI.Instructions;
using Fumo.Shared.ThirdParty.ThreeLetterAPI.Response;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Context;
using SerilogTracing;

namespace Fumo.Shared.Repositories;

public interface IUserRepository
{
    /// <exception cref="UserNotFoundException"></exception>
    public ValueTask<UserDTO> SearchName(string username, CancellationToken cancellationToken = default);

    /// <exception cref="UserNotFoundException"></exception>
    public ValueTask<UserDTO> SearchID(string id, CancellationToken cancellationToken = default);

    public ValueTask<List<UserDTO>> SearchMultipleByID(IEnumerable<string> ids, CancellationToken cancellation = default);

    public ValueTask SaveChanges(CancellationToken cancellationToken = default);
}

public class UserRepository : IUserRepository
{
    // FIXME: In memory caching
    private readonly ILogger Logger;
    private readonly DatabaseContext Database;
    private readonly IThreeLetterAPI ThreeLetterAPI;


    public UserRepository(ILogger logger, DatabaseContext database, IThreeLetterAPI threeLetterAPI)
    {
        Logger = logger.ForContext<UserRepository>();
        Database = database;
        ThreeLetterAPI = threeLetterAPI;
    }

    private async ValueTask<UserDTO?> SearchWithThreeLetterAPI(string? id = null, string? login = null, CancellationToken cancellationToken = default)
    {
        using var activity = Logger.StartActivity("Searching for user {UserIdentifier}");

        var tlaUser = await ThreeLetterAPI.Send<BasicUserResponse>(new BasicUserInstruction(id, login), cancellationToken);

        if (tlaUser is null || tlaUser.User is null)
        {
            return null;
        }

        UserDTO user = new()
        {
            TwitchID = tlaUser.User.ID,
            TwitchName = tlaUser.User.Login,
        };

        Database.Entry(user).State = !(await Database.Users.AnyAsync(x => x.TwitchID.Equals(user.TwitchID), cancellationToken))
            ? EntityState.Added
            : EntityState.Modified;

        await Database.Users.AddAsync(user, cancellationToken);
        await Database.SaveChangesAsync(cancellationToken);

        return user;
    }

    /// <inheritdoc/>
    public async ValueTask<UserDTO> SearchID(string id, CancellationToken cancellationToken = default)
    {
        var dbUser = await Database
            .Users
            .Where(x => x.TwitchID.Equals(id))
            .SingleOrDefaultAsync(cancellationToken);

        if (dbUser is not null)
        {
            return dbUser;
        }

        using var _ = LogContext.PushProperty("UserIdentifier", id);
        return await SearchWithThreeLetterAPI(id, cancellationToken: cancellationToken) switch
        {
            null => throw new UserNotFoundException($"The user with the id {id} does not exist"),
            var user => user,
        };
    }

    /// <inheritdoc/>
    public async ValueTask<UserDTO> SearchName(string username, CancellationToken cancellationToken = default)
    {
        var cleanedUsername = UsernameCleanerRegex.CleanUsername(username);

        var dbUser = await Database.Users
        .Where(x => x.TwitchName.Equals(cleanedUsername))
        .SingleOrDefaultAsync(cancellationToken);

        if (dbUser is not null)
        {
            return dbUser;
        }

        using var _ = LogContext.PushProperty("UserIdentifier", cleanedUsername);
        return await SearchWithThreeLetterAPI(login: cleanedUsername, cancellationToken: cancellationToken) switch
        {
            null => throw new UserNotFoundException($"The user with the name {cleanedUsername} does not exist"),
            var user => user,
        };
    }

    public async ValueTask<List<UserDTO>> SearchMultipleByID(IEnumerable<string> ids, CancellationToken cancellation = default)
    {
        var dbUsers = await Database.Users
            .Where(x => ids.Contains(x.TwitchID))
            .ToListAsync(cancellation);

        var missing = ids.Except(dbUsers.Select(x => x.TwitchID)).ToArray();

        if (missing.Length <= 0)
        {
            return dbUsers;
        }

        using var activity = Logger.StartActivity("Searching for multiple users {UserIdentifiers}", missing);

        BasicBatchUserInstruction request = new(missing);

        var response = await ThreeLetterAPI.Send<BasicBatchUserResponse>(request, cancellation);

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

    public async ValueTask SaveChanges(CancellationToken cancellationToken = default)
    {
        await Database.SaveChangesAsync(cancellationToken);
    }
}
