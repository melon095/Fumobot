using Fumo.Database;
using Fumo.Database.DTO;
using Fumo.Database.Extensions;
using Fumo.Shared.Eventsub;
using Microsoft.EntityFrameworkCore;
using MiniTwitch.Irc;
using Quartz;
using Serilog;
using SerilogTracing;

namespace Fumo.BackgroundJobs;

public class ChannelRemoverJob : IJob
{
    private readonly ILogger Logger;
    private readonly DatabaseContext Database;
    private readonly IEventsubManager EventsubManager;
    private readonly IrcClient IrcClient;

    public ChannelRemoverJob(ILogger logger, DatabaseContext database, IEventsubManager eventsubManager, IrcClient ircClient)
    {
        Logger = logger.ForContext<ChannelRemoverJob>();
        Database = database;
        EventsubManager = eventsubManager;
        IrcClient = ircClient;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        using var activity = Logger.StartActivity("ChannelRemoverJob");

        try
        {
            using var transaction = await Database.Database.BeginTransactionAsync(context.CancellationToken);
            ChannelDTO[] channelsToRemove = await Database.Channels
                .Where(x => x.SetForDeletion == true)
                .ToArrayAsync(context.CancellationToken);

            foreach (var channel in channelsToRemove)
            {
                Logger.Information("Removing channel {ChannelName} ({ChannelId}) from the database", channel.TwitchName, channel.TwitchID);

                if (channel.GetSettingBool(ChannelSettingKey.ConnectedWithEventsub) == true)
                    await EventsubManager.Unsubscribe(channel.TwitchID, EventsubType.ChannelChatMessage, context.CancellationToken);
                else
                    await IrcClient.PartChannel(channel.TwitchName, context.CancellationToken);

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
