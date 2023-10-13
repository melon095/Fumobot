using Fumo.Database.DTO;

namespace Fumo.Shared.Interfaces;

public interface IChannelRepository
{
    IEnumerable<ChannelDTO> GetAll();

    ChannelDTO? GetByID(string ID);

    ChannelDTO? GetByName(string Name);

    Task Create(ChannelDTO channelDTO, CancellationToken ct = default);

    Task Delete(ChannelDTO channelDTO, CancellationToken ct = default);

    Task Update(ChannelDTO channelDTO, CancellationToken ct = default);
}
