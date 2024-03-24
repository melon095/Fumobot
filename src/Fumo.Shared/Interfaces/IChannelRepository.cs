using Fumo.Database.DTO;

namespace Fumo.Shared.Interfaces;

public interface IChannelRepository
{
    event Func<ChannelDTO, ValueTask> OnChannelCreated;
    event Func<ChannelDTO, ValueTask> OnChannelDeleted;

    IEnumerable<ChannelDTO> GetAll();

    ChannelDTO? GetByID(string ID);

    ChannelDTO? GetByName(string Name);

    ValueTask Create(ChannelDTO channelDTO, CancellationToken ct = default);

    ValueTask Delete(ChannelDTO channelDTO, CancellationToken ct = default);

    ValueTask Update(ChannelDTO channelDTO, CancellationToken ct = default);
}
