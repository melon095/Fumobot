using Fumo.Database;
using Fumo.Database.DTO;
using Fumo.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using MiniTwitch.Common.Extensions;
using System.Collections.Concurrent;
using System.Data;

namespace Fumo.Shared.Repositories;

public class ChannelRepository : IChannelRepository
{
    // TODO: Look into using IDbContextFactory
    private readonly DatabaseContext Database;

    public event Func<ChannelDTO, ValueTask> OnChannelCreated = default!;
    public event Func<ChannelDTO, ValueTask> OnChannelDeleted = default!;

    private static ConcurrentDictionary<string, ChannelDTO> Channels { get; set; }

    public ChannelRepository(DatabaseContext database)
    {
        Database = database;

        Fill(default).Wait();
    }

    private async Task Fill(CancellationToken cancellationToken)
    {
        if (Channels is not null)
            return;

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

    public async ValueTask Create(ChannelDTO channelDTO, CancellationToken cancellationToken = default)
    {
        using (var transaction = await Database.Database.BeginTransactionAsync(cancellationToken))
        {
            await Database.Channels.AddAsync(channelDTO, cancellationToken);
            await Database.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }

        // SMH if this shit is null.
        ChannelDTO newlyAdded = await Database.Channels.Where(x => x.TwitchID == channelDTO.TwitchID).SingleAsync(cancellationToken)!;

        Channels[newlyAdded.TwitchName] = newlyAdded;

        OnChannelCreated
            .Invoke(newlyAdded)
            .StepOver();
    }

    public async ValueTask Delete(ChannelDTO channelDTO, CancellationToken cancellationToken = default)
    {
        using var transaction = await Database.Database.BeginTransactionAsync(cancellationToken);

        channelDTO.SetForDeletion = true;
        await Database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        Channels.TryRemove(channelDTO.TwitchName, out _);

        OnChannelDeleted
            .Invoke(channelDTO)
            .StepOver();
    }

    public async ValueTask Update(ChannelDTO channelDTO, CancellationToken cancellationToken = default)
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
