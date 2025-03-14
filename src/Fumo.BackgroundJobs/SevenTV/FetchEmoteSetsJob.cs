using System.Net;
using Fumo.Database;
using Fumo.Database.Extensions;
using Fumo.Shared.Repositories;
using Fumo.Shared.ThirdParty.Emotes.SevenTV;
using Fumo.Shared.ThirdParty.Exceptions;
using Quartz;
using Serilog;
using Serilog.Context;
using Serilog.Events;
using SerilogTracing;

namespace Fumo.BackgroundJobs.SevenTV;

public class FetchEmoteSetsJob : IJob
{
    public readonly ILogger Logger;
    public readonly ISevenTVService SevenTVService;
    public readonly IChannelRepository ChannelRepository;

    public FetchEmoteSetsJob(ILogger logger, ISevenTVService sevenTVService, IChannelRepository channelRepository)
    {
        Logger = logger.ForContext<FetchEmoteSetsJob>();
        SevenTVService = sevenTVService;
        ChannelRepository = channelRepository;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        using var activity = Logger.StartActivity("7TV FetchEmoteSetsJob");
        var channels = ChannelRepository.GetAll();

        foreach (var channel in channels)
        {
            try
            {
                using var _ = LogContext.PushProperty("ChannelId", channel.TwitchID);

                var currentEmoteSetId = channel.GetSetting(ChannelSettingKey.SevenTV_EmoteSet);

                var sevenTvUser = await SevenTVService.GetUserInfo(channel.TwitchID, context.CancellationToken);
                if (sevenTvUser.EmoteSet is null) continue;

                var emoteSetId = sevenTvUser.EmoteSet.ID;

                if (currentEmoteSetId == emoteSetId) continue;

                channel.SetSetting(ChannelSettingKey.SevenTV_EmoteSet, emoteSetId);
                channel.SetSetting(ChannelSettingKey.SevenTV_UserID, sevenTvUser.SevenTVID);

                await ChannelRepository.Update(channel, context.CancellationToken);

                Logger.Information("Channel {ChannelName} Emote Set update {EmoteSet}", channel.TwitchName, emoteSetId);
            }
            catch (GraphQLException ex) when (ex.StatusCode != HttpStatusCode.OK)
            {
                continue;
            }
            catch (TaskCanceledException)
            {
                // HTTP Timeout, cause the remote server is slow
                continue;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to get 7TV emote set for {ChannelName}", channel.TwitchName);
            }
        }
    }
}
