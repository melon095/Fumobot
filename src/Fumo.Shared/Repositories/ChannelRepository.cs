using Autofac;
using Fumo.Database;
using Fumo.Database.DTO;
using Fumo.Shared.Mediator;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Data;

namespace Fumo.Shared.Repositories;

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

public class ChannelRepository : IChannelRepository
{
    // TODO: Look into using IDbContextFactory
    private readonly DatabaseContext Database;
    // TODO: Keeping a reference to the lifetime scope is a bad idea.
    private readonly ILifetimeScope LifetimeScope;

    private static ConcurrentDictionary<string, ChannelDTO> Channels { get; set; }

    public ChannelRepository(DatabaseContext database, ILifetimeScope lifetimeScope)
    {
        Database = database;
        LifetimeScope = lifetimeScope;
    }

    public async ValueTask Prepare(CancellationToken ct = default)
    {
        if (Channels is not null)
            return;

        Channels = new();

        var channels = await Database.Channels
            .Where(x => !x.SetForDeletion)
            .Include(x => x.User)
            .ToListAsync(ct);

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
        => Channels.Values.FirstOrDefault(x => x.TwitchID == ID);

    public ChannelDTO? GetByName(string Name)
        => Channels[Name];

    public async ValueTask<ChannelDTO> Create(ChannelDTO channelDTO, CancellationToken cancellationToken = default)
    {
        using var scope = LifetimeScope.BeginLifetimeScope();
        var bus = scope.Resolve<IMediator>();

        using (var transaction = await Database.Database.BeginTransactionAsync(cancellationToken))
        {
            await Database.Channels.AddAsync(channelDTO, cancellationToken);
            await Database.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }

        // SMH if this shit is null.
        ChannelDTO newlyAdded = await Database.Channels.Where(x => x.TwitchID == channelDTO.TwitchID).SingleAsync(cancellationToken)!;

        Channels[newlyAdded.TwitchName] = newlyAdded;

        await bus.Publish(new OnChannelCreatedCommand(newlyAdded), cancellationToken);

        return newlyAdded;
    }

    public async ValueTask Delete(ChannelDTO channelDTO, CancellationToken cancellationToken = default)
    {
        using var scope = LifetimeScope.BeginLifetimeScope();
        var bus = scope.Resolve<IMediator>();

        using (var transaction = await Database.Database.BeginTransactionAsync(cancellationToken))
        {
            channelDTO.SetForDeletion = true;
            await Database.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        Channels.TryRemove(channelDTO.TwitchName, out _);

        await bus.Publish(new OnChannelDeletedCommand(channelDTO), cancellationToken);
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
