using Fumo.Shared.Extensions;
using Fumo.Shared.Interfaces;
using Fumo.Shared.Models;
using Fumo.Shared.ThirdParty.Emotes.SevenTV;
using Fumo.Shared.ThirdParty.Emotes.SevenTV.Models;
using Fumo.Shared.ThirdParty.Exceptions;
using Fumo.Shared.ThirdParty.ThreeLetterAPI;
using Microsoft.Extensions.Configuration;
using Quartz;
using Serilog;
using StackExchange.Redis;

namespace Fumo.BackgroundJobs.SevenTV;

internal class FetchChannelEditorsJob : IJob
{
    public readonly ILogger Logger;
    public readonly IDatabase Redis;
    public readonly ISevenTVService SevenTV;
    public readonly IChannelRepository ChannelRepository;
    public readonly IThreeLetterAPI ThreeLetterAPI;
    public readonly IUserRepository UserRepository;
    private readonly string BotID;

    public FetchChannelEditorsJob(
        ILogger logger,
        IDatabase redis,
        ISevenTVService sevenTVService,
        AppSettings settings,
        IChannelRepository channelRepository,
        IUserRepository userRepository,
        IThreeLetterAPI threeLetterAPI)
    {
        Logger = logger.ForContext<FetchChannelEditorsJob>();
        Redis = redis;
        SevenTV = sevenTVService;
        ChannelRepository = channelRepository;
        UserRepository = userRepository;
        ThreeLetterAPI = threeLetterAPI;
        BotID = settings.Twitch.UserID;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var botEmoteSets = (await SevenTV.GetEditorEmoteSetsOfUser(BotID, context.CancellationToken))
            .EditorOf
            .Where(x => x.User.Connections.GetTwitchConnection() is not null);

        foreach (var editorEmoteSet in botEmoteSets)
        {
            try
            {
                // FIXME: Make this not do a million "To" casting.

                var twitchConnection = editorEmoteSet.User.Connections.GetTwitchConnection();

                var current7TVEditors = await SevenTV.GetEditors(twitchConnection.Id, context.CancellationToken);

                var idsToMap = current7TVEditors.Editors
                    .Select(x => x.User.Connections.GetTwitchConnection())
                    .Where(x => x is not null)
                    .Select(x => x!.Id)
                    .ToArray();

                var mappedUsers = (await (UserRepository.SearchMultipleByID(idsToMap, context.CancellationToken)))
                    .Select(x => x.TwitchID)
                    .ToArray();

                var key = SevenTVService.EditorKey(twitchConnection.Id);
                RedisValue[] items = Array.ConvertAll(mappedUsers, value => new RedisValue(value));

                await Redis.KeyDeleteAsync(key);
                await Redis.SetAddAsync(key, items);
            }
            catch (GraphQLException ex) when (ex.StatusCode != System.Net.HttpStatusCode.OK)
            {
                continue;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to get editors of 7TV account {SevenTVName}", editorEmoteSet.User.Username);
            }
        }
    }
}
