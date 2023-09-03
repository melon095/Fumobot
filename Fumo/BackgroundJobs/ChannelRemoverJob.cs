using Fumo.Database;
using Fumo.Database.DTO;
using Microsoft.EntityFrameworkCore;
using Quartz;
using Serilog;

namespace Fumo.BackgroundJobs;

internal class ChannelRemoverJob : IJob
{
    public ILogger Logger { get; }
    public DatabaseContext Database { get; }


    public ChannelRemoverJob(ILogger logger, DatabaseContext database)
    {
        Logger = logger.ForContext<ChannelRemoverJob>();
        Database = database;
    }
    public async Task Execute(IJobExecutionContext context)
    {
        this.Logger.Information("Running Cron");

        try
        {
            using var transaction = await Database.Database.BeginTransactionAsync(context.CancellationToken);
            ChannelDTO[] channelsToRemove = await this.Database.Channels
                .Where(x => x.SetForDeletion == true)
                .ToArrayAsync(context.CancellationToken);

            foreach (var channel in channelsToRemove)
            {
                Logger.Information("Removing channel {ChannelName} from the database", channel.TwitchName);
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
