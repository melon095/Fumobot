using Fumo.Database;
using Fumo.Database.DTO;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Data;
using System.Runtime.CompilerServices;

namespace Fumo.Repository;

internal class ChannelRepository : IChannelRepository
{
    private DatabaseContext Database { get; set; }

    private ConcurrentDictionary<string, ChannelDTO> Channels { get; set; }

    public ChannelRepository(DatabaseContext database)
    {
        Database = database;
    }

    private async Task FIllIfNeeded(CancellationToken cancellationToken)
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
                this.Channels[channel.TwitchName] = channel;
            }
        }
    }

    public async IAsyncEnumerable<ChannelDTO> GetAll([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await FIllIfNeeded(cancellationToken);

        foreach (var channel in this.Channels)
        {
            // TODO: Idk if this works
            if (cancellationToken.IsCancellationRequested)
                yield break;

            yield return channel.Value;
        }
    }

    public async Task<ChannelDTO?> GetByID(string ID, CancellationToken cancellationToken = default)
    {
        await FIllIfNeeded(cancellationToken);

        return Channels.Values.FirstOrDefault(x => x.TwitchID == ID);
    }

    public async Task<ChannelDTO?> GetByName(string Name, CancellationToken cancellationToken = default)
    {
        await FIllIfNeeded(cancellationToken);

        return Channels[Name];
    }

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
        await FIllIfNeeded(cancellationToken);

        using var transaction = await Database.Database.BeginTransactionAsync(cancellationToken);

        channelDTO.SetForDeletion = true;
        await Database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        Channels.TryRemove(channelDTO.TwitchName, out _);
    }

    public async Task Update(ChannelDTO channelDTO, CancellationToken cancellationToken = default)
    {
        await FIllIfNeeded(cancellationToken);

        // Very ugly xd
        // Issue is that EF Core can't update the key so just gotta do it this way.
        if (!Channels.ContainsKey(channelDTO.TwitchName))
        {
            var channel = Channels.Values.FirstOrDefault(x => x.TwitchID == channelDTO.TwitchID);
            if (channel is null)
            {
                // TODO: Make this cleaner
                throw new DataException("Channel not found");
            }

            Channels.TryRemove(channel.TwitchName, out _);
            Channels[channelDTO.TwitchName] = channelDTO;
        }

        Database.Channels.Update(channelDTO);
        await Database.SaveChangesAsync(cancellationToken);
    }
}
