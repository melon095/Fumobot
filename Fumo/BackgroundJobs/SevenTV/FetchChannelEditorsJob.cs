using Fumo.Database;
using Fumo.Database.DTO;
using Fumo.Interfaces;
using Fumo.Repository;
using Fumo.ThirdParty.Emotes.SevenTV;
using Fumo.ThirdParty.ThreeLetterAPI;
using Fumo.ThirdParty.ThreeLetterAPI.Instructions;
using Fumo.ThirdParty.ThreeLetterAPI.Response;
using Microsoft.Extensions.Configuration;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal.Mapping;
using Quartz;
using Serilog;
using StackExchange.Redis;
using System.Runtime.InteropServices;

namespace Fumo.BackgroundJobs.SevenTV;

internal class FetchChannelEditorsJob : IJob
{
    public ILogger Logger { get; }

    public IDatabase Redis { get; }

    public ISevenTVService SevenTVService { get; }

    public IChannelRepository ChannelRepository { get; }

    public IThreeLetterAPI ThreeLetterAPI { get; }

    public IUserRepository UserRepository { get; }

    private readonly string BotID;

    public FetchChannelEditorsJob(
        ILogger logger,
        IDatabase redis,
        ISevenTVService sevenTVService,
        IConfiguration configuration,
        IChannelRepository channelRepository,
        IUserRepository userRepository,
        IThreeLetterAPI threeLetterAPI)
    {
        Logger = logger.ForContext<FetchChannelEditorsJob>();
        Redis = redis;
        SevenTVService = sevenTVService;
        ChannelRepository = channelRepository;
        UserRepository = userRepository;
        ThreeLetterAPI = threeLetterAPI;

        BotID = configuration["Twitch:UserID"]!;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var channels = await ChannelRepository.GetAll(context.CancellationToken).ToListAsync(context.CancellationToken);

        var botEmoteSets = (await SevenTVService.GetEditorEmoteSetsOfUser(BotID, context.CancellationToken))
            .EditorOf
            .Where(x => x.User.Connections.GetTwitchConnection() is not null);

        foreach (var editorEmoteSet in botEmoteSets)
        {
            try
            {
                // FIXME: Make this not do a million "To" casting.

                var twitchConnection = editorEmoteSet.User.Connections.GetTwitchConnection();

                var current7TVEditors = await SevenTVService.GetEditors(twitchConnection.Id, context.CancellationToken);

                var idsToMap = current7TVEditors.Editors.Select(x => x.User.Connections.GetTwitchConnection().Id).ToArray();

                var mappedUsers = (await (UserRepository.SearchMultipleByIDAsync(idsToMap, context.CancellationToken)))
                    .Select(x => x.TwitchID)
                    .ToArray();

                var key = $"seventv:{twitchConnection.Id}:editors";
                RedisValue[] items = Array.ConvertAll(mappedUsers, value => new RedisValue(value));

                await Redis.KeyDeleteAsync(key);
                await Redis.SetAddAsync(key, items);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to get editors of 7TV account {SevenTVName}", editorEmoteSet.User.Username);
            }
        }
    }
}
