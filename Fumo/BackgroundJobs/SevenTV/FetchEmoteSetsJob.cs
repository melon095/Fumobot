using Fumo.Database;
using Fumo.Database.Extensions;
using Fumo.Repository;
using Fumo.ThirdParty.Emotes.SevenTV;
using Fumo.ThirdParty.Exceptions;
using Quartz;
using Serilog;
using System.Runtime.InteropServices;

namespace Fumo.BackgroundJobs.SevenTV;

internal class FetchEmoteSetsJob : IJob
{
    public ILogger Logger { get; }

    public ISevenTVService SevenTVService { get; }

    public IChannelRepository ChannelRepository { get; }

    public FetchEmoteSetsJob(ILogger logger, ISevenTVService sevenTVService, IChannelRepository channelRepository)
    {
        Logger = logger.ForContext<FetchEmoteSetsJob>();
        SevenTVService = sevenTVService;
        ChannelRepository = channelRepository;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var channels = ChannelRepository.GetAll(context.CancellationToken);

        await foreach (var channel in channels)
        {
            try
            {
                var currentEmoteSetId = channel.GetSetting(ChannelSettingKey.SevenTV_EmoteSet);

                var sevenTvUser = await SevenTVService.GetUserInfo(channel.TwitchID, context.CancellationToken);

                if (!sevenTvUser.TryDefaultEmoteSet(out var emoteSet))
                {
                    continue;
                }

                if (currentEmoteSetId == emoteSet.Id) continue;

                channel.SetSetting(ChannelSettingKey.SevenTV_EmoteSet, emoteSet.Id);
                channel.SetSetting(ChannelSettingKey.SevenTV_UserID, sevenTvUser.Id);

                await ChannelRepository.Update(channel, context.CancellationToken);

                Logger.Information("Channel {ChannelName} Emote Set update {EmoteSet}", channel.TwitchName, emoteSet.Id);
            }
            catch (GraphQLException ex) when (ex.StatusCode != System.Net.HttpStatusCode.OK)
            {
                continue;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to get 7TV emote set for {ChannelName}", channel.TwitchName);
            }
        }
    }
}
