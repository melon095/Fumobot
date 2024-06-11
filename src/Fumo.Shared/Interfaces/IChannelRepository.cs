using Fumo.Database.DTO;

namespace Fumo.Shared.Interfaces;

public interface IChannelRepository
{
    ValueTask Prepare(CancellationToken ct = default);

    IEnumerable<ChannelDTO> GetAll();

    ChannelDTO? GetByID(string ID);

    ChannelDTO? GetByName(string Name);

    ValueTask<ChannelDTO> Create(ChannelDTO channelDTO, CancellationToken ct = default);

    ValueTask Delete(ChannelDTO channelDTO, CancellationToken ct = default);

    ValueTask Update(ChannelDTO channelDTO, CancellationToken ct = default);
}
