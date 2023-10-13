using Fumo.Database;
using Fumo.Database.DTO;
using Fumo.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Data;

namespace Fumo.Shared.Repositories;

public class ChannelRepository : IChannelRepository
{
    // TODO: Look into using IDbContextFactory
    private readonly DatabaseContext Database;

    private static ConcurrentDictionary<string, ChannelDTO> Channels { get; set; }

    public ChannelRepository(DatabaseContext database)
    {
        Database = database;

        _ = Fill(default);
    }

    private async Task Fill(CancellationToken cancellationToken)
    {
        if (Channels is null)
        {
            Channels = new();

            var channels = await this.Database.Channels
                .Where(x => !x.SetForDeletion)
                .Include(x => x.User)
                .ToListAsync(cancellationToken);

            foreach (var channel in channels)
            {
                Channels[channel.TwitchName] = channel;
            }
        }
    }

    public IEnumerable<ChannelDTO> GetAll()
    {
        foreach (var channel in Channels)
        {
            yield return channel.Value;
        }
    }

    public ChannelDTO? GetByID(string ID)
        => Channels.Where(x => x.Value.TwitchID == ID).Select(x => x.Value).FirstOrDefault();

    public ChannelDTO? GetByName(string Name)
        => Channels[Name];

    public async Task Create(ChannelDTO channelDTO, CancellationToken cancellationToken = default)
    {
        using (var transaction = await Database.Database.BeginTransactionAsync(cancellationToken))
        {
            await Database.Channels.AddAsync(channelDTO, cancellationToken);
            await Database.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }

        ChannelDTO newlyAdded = await Database.Channels.Where(x => x.TwitchID == channelDTO.TwitchID).SingleAsync(cancellationToken);

        if (newlyAdded != null)
        {
            Channels[newlyAdded.TwitchName] = newlyAdded;
        }
    }

    public async Task Delete(ChannelDTO channelDTO, CancellationToken cancellationToken = default)
    {
        using var transaction = await Database.Database.BeginTransactionAsync(cancellationToken);

        channelDTO.SetForDeletion = true;
        await Database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        Channels.TryRemove(channelDTO.TwitchName, out _);
    }

    public async Task Update(ChannelDTO channelDTO, CancellationToken cancellationToken = default)
    {
        // Very ugly xd
        // Issue is that EF Core can't update the key so just gotta do it this way.
        if (!Channels.ContainsKey(channelDTO.TwitchName))
        {
            var channel = Channels.Values.FirstOrDefault(x => x.TwitchID == channelDTO.TwitchID) ?? throw new DataException("Channel not found");

            Channels.TryRemove(channel.TwitchName, out _);
            Channels[channelDTO.TwitchName] = channelDTO;
        }

        Database.Channels.Update(channelDTO);
        await Database.SaveChangesAsync(cancellationToken);
    }
}
