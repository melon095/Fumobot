using Fumo.Database.DTO;

namespace Fumo.Shared.Interfaces;

public interface IUserRepository
{
    /// <exception cref="Fumo.Shared.Exceptions.UserNotFoundException"></exception>
    public Task<UserDTO> SearchNameAsync(string username, CancellationToken cancellationToken = default);

    /// <exception cref="Fumo.Shared.Exceptions.UserNotFoundException"></exception>
    public Task<UserDTO> SearchIDAsync(string id, CancellationToken cancellationToken = default);

    public Task<List<UserDTO>> SearchMultipleByIDAsync(IEnumerable<string> ids, CancellationToken cancellation = default);

    public Task SaveChanges(CancellationToken cancellationToken = default);
}
