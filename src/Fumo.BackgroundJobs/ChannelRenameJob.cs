using Fumo.Database;
using Fumo.Database.Extensions;
using Fumo.Shared.Repositories;
using Fumo.Shared.ThirdParty.ThreeLetterAPI;
using Fumo.Shared.ThirdParty.ThreeLetterAPI.Instructions;
using Fumo.Shared.ThirdParty.ThreeLetterAPI.Response;
using MiniTwitch.Irc;
using Quartz;
using Serilog;
using Serilog.Context;
using SerilogTracing;

namespace Fumo.BackgroundJobs;

public class ChannelRenameJob : IJob
{
    public readonly ILogger Logger;
    public readonly IChannelRepository ChannelRepository;
    public readonly IThreeLetterAPI ThreeLetterAPI;
    public readonly IrcClient IrcClient;

    public ChannelRenameJob(
        ILogger logger,
        IChannelRepository channelRepository,
        IThreeLetterAPI threeLetterAPI,
        IrcClient ircClient)
    {
        Logger = logger.ForContext<ChannelRenameJob>();
        ChannelRepository = channelRepository;
        ThreeLetterAPI = threeLetterAPI;
        IrcClient = ircClient;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        using var activity = Logger.StartActivity("ChannelRenamerJob");
        var channels = ChannelRepository.GetAll();

        foreach (var channel in channels)
        {
            try
            {
                using var _ = LogContext.PushProperty("ChannelName", channel.TwitchName);

                var tlaUser = await ThreeLetterAPI.Send<BasicUserResponse>(new BasicUserInstruction(id: channel.TwitchID), context.CancellationToken);

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

                if (channel.GetSettingBool(ChannelSettingKey.ConnectedWithEventsub) == false)
                {
                    await IrcClient.PartChannel(oldName, context.CancellationToken);
                    await IrcClient.JoinChannel(channel.TwitchName, context.CancellationToken);
                    await IrcClient.SendMessage(channel.TwitchName, "FeelsDankMan TeaTime", cancellationToken: context.CancellationToken);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to check rename for {ChannelName}", channel.TwitchName);
            }
        }
    }
}
