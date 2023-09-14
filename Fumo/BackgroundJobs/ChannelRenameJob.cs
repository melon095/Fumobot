using Fumo.Database;
using Fumo.Shared.Interfaces;
using Fumo.ThirdParty.ThreeLetterAPI;
using Fumo.ThirdParty.ThreeLetterAPI.Instructions;
using Fumo.ThirdParty.ThreeLetterAPI.Response;
using Microsoft.EntityFrameworkCore;
using MiniTwitch.Irc;
using Quartz;
using Serilog;

namespace Fumo.BackgroundJobs;

internal class ChannelRenameJob : IJob
{
    public readonly ILogger Logger;
    public readonly IChannelRepository ChannelRepository;
    public readonly IThreeLetterAPI ThreeLetterAPI;
    public readonly IrcClient IrcClient;
    public readonly DatabaseContext DatabaseContext;

    public ChannelRenameJob(
        ILogger logger,
        IChannelRepository channelRepository,
        IThreeLetterAPI threeLetterAPI,
        IrcClient ircClient,
        DatabaseContext databaseContext)
    {
        Logger = logger.ForContext<ChannelRenameJob>();
        ChannelRepository = channelRepository;
        ThreeLetterAPI = threeLetterAPI;
        IrcClient = ircClient;
        DatabaseContext = databaseContext;
    }


    public async Task Execute(IJobExecutionContext context)
    {
        var channels = ChannelRepository.GetAll(context.CancellationToken);

        await foreach (var channel in channels)
        {
            try
            {
                var tlaUser = await ThreeLetterAPI.SendAsync<BasicUserResponse>(new BasicUserInstruction(id: channel.TwitchID), context.CancellationToken);

                if (tlaUser.User.Login == channel.TwitchName)
                {
                    continue;
                }

                Logger.Information("Channel {ChannelName} renamed, updating to {NewChannelName}", channel.TwitchName, tlaUser.User.Login);

                var oldName = channel.TwitchName;

                channel.TwitchName = tlaUser.User.Login;
                channel.User.TwitchName = tlaUser.User.Login;
                channel.User.UsernameHistory.Add(new(oldName, DateTime.Now));

                await ChannelRepository.Update(channel, context.CancellationToken);

                // TODO: This might not be needed
                DatabaseContext.Entry(channel.User).State = EntityState.Modified;
                await DatabaseContext.SaveChangesAsync(context.CancellationToken);

                await IrcClient.PartChannel(oldName, context.CancellationToken);
                await IrcClient.JoinChannel(channel.TwitchName, context.CancellationToken);
                await IrcClient.SendMessage(channel.TwitchName, "FeelsDankMan TeaTime", cancellationToken: context.CancellationToken);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to check rename for {ChannelName}", channel.TwitchName);
            }
        }
    }
}
