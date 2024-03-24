using Fumo.Database.DTO;

namespace Fumo.Shared.Interfaces;

public interface IUserRepository
{
    /// <exception cref="Fumo.Shared.Exceptions.UserNotFoundException"></exception>
    public ValueTask<UserDTO> SearchName(string username, CancellationToken cancellationToken = default);

    /// <exception cref="Fumo.Shared.Exceptions.UserNotFoundException"></exception>
    public ValueTask<UserDTO> SearchID(string id, CancellationToken cancellationToken = default);

    public ValueTask<List<UserDTO>> SearchMultipleByID(IEnumerable<string> ids, CancellationToken cancellation = default);

    public ValueTask SaveChanges(CancellationToken cancellationToken = default);
}
