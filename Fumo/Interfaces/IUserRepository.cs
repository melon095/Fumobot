using Fumo.Database;

namespace Fumo.Interfaces;

public interface IUserRepository
{
    /// <exception cref="Fumo.Exceptions.UserNotFoundException"></exception>
    public Task<UserDTO> SearchNameAsync(string username, CancellationToken? cancellationToken);

    /// <exception cref="Fumo.Exceptions.UserNotFoundException"></exception>
    public Task<UserDTO> SearchIDAsync(string id, CancellationToken? cancellationToken);

    public static string CleanUsername(string username) => throw new NotImplementedException();
}
