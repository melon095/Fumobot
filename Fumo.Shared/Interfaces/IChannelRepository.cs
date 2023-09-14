using Fumo.Database.DTO;

namespace Fumo.Shared.Interfaces;

public interface IChannelRepository
{
    IAsyncEnumerable<ChannelDTO> GetAll(CancellationToken cancellationToken = default);

    Task<ChannelDTO?> GetByID(string ID, CancellationToken cancellationToken = default);

    Task<ChannelDTO?> GetByName(string Name, CancellationToken cancellationToken = default);

    Task Create(ChannelDTO channelDTO, CancellationToken cancellationToken = default);

    Task Delete(ChannelDTO channelDTO, CancellationToken cancellationToken = default);

    Task Update(ChannelDTO channelDTO, CancellationToken cancellationToken = default);
}
