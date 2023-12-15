using Fumo.Database.DTO;

namespace Fumo.Shared.Interfaces;

public interface IUserRepository
{
    /// <exception cref="Fumo.Shared.Exceptions.UserNotFoundException"></exception>
    public ValueTask<UserDTO> SearchNameAsync(string username, CancellationToken cancellationToken = default);

    /// <exception cref="Fumo.Shared.Exceptions.UserNotFoundException"></exception>
    public ValueTask<UserDTO> SearchIDAsync(string id, CancellationToken cancellationToken = default);

    public ValueTask<List<UserDTO>> SearchMultipleByIDAsync(IEnumerable<string> ids, CancellationToken cancellation = default);

    public ValueTask SaveChanges(CancellationToken cancellationToken = default);
}
