using Fumo.Database;
using Fumo.Database.DTO;
using Fumo.Shared.Eventsub;
using Microsoft.EntityFrameworkCore;
using Quartz;
using Serilog;

namespace Fumo.BackgroundJobs;

public class ChannelRemoverJob : IJob
{
    private readonly ILogger Logger;
    private readonly DatabaseContext Database;
    private readonly IEventsubManager EventsubManager;

    public ChannelRemoverJob(ILogger logger, DatabaseContext database, IEventsubManager eventsubManager)
    {
        Logger = logger.ForContext<ChannelRemoverJob>();
        Database = database;
        EventsubManager = eventsubManager;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        Logger.Information("Running Cron");

        try
        {
            using var transaction = await Database.Database.BeginTransactionAsync(context.CancellationToken);
            ChannelDTO[] channelsToRemove = await Database.Channels
                .Where(x => x.SetForDeletion == true)
                .ToArrayAsync(context.CancellationToken);

            foreach (var channel in channelsToRemove)
            {
                Logger.Information("Removing channel {ChannelName} from the database", channel.TwitchName);

                await EventsubManager.Unsubscribe(channel.TwitchID, EventsubType.ChannelChatMessage, context.CancellationToken);

                Database.Channels.Remove(channel);
            }

            await Database.SaveChangesAsync(context.CancellationToken);

            await transaction.CommitAsync(context.CancellationToken);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to remove channels set for deletion");
        }
    }
}
