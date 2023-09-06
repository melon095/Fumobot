using Fumo.Database.DTO;

namespace Fumo.Interfaces;

public interface IUserRepository
{
    /// <exception cref="Fumo.Exceptions.UserNotFoundException"></exception>
    public Task<UserDTO> SearchNameAsync(string username, CancellationToken cancellationToken = default);

    /// <exception cref="Fumo.Exceptions.UserNotFoundException"></exception>
    public Task<UserDTO> SearchIDAsync(string id, CancellationToken cancellationToken = default);

    public Task<List<UserDTO>> SearchMultipleByIDAsync(IEnumerable<string> ids, CancellationToken cancellation = default);


    public static string CleanUsername(string username) => throw new NotImplementedException();
}
