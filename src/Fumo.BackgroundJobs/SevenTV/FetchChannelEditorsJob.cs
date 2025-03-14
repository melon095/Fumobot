using Fumo.Shared.Models;
using Fumo.Shared.ThirdParty.Emotes.SevenTV;
using Fumo.Shared.ThirdParty.Emotes.SevenTV.Models;
using Fumo.Shared.ThirdParty.Exceptions;
using Fumo.Shared.ThirdParty.ThreeLetterAPI;
using Quartz;
using StackExchange.Redis;
using Serilog;
using Fumo.Shared.Repositories;
using SerilogTracing;

namespace Fumo.BackgroundJobs.SevenTV;

public class FetchChannelEditorsJob : IJob
{
    public readonly ILogger Logger;
    public readonly IDatabase Redis;
    public readonly ISevenTVService SevenTV;
    public readonly IUserRepository UserRepository;
    private readonly string BotID;

    public FetchChannelEditorsJob(
        AppSettings settings,
        ILogger logger,
        IDatabase redis,
        ISevenTVService sevenTVService,
        IUserRepository userRepository)
    {
        Logger = logger.ForContext<FetchChannelEditorsJob>();
        Redis = redis;
        SevenTV = sevenTVService;
        UserRepository = userRepository;
        BotID = settings.Twitch.UserID;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        using var activity = Logger.StartActivity("7TV FetchChannelEditorsJob");

        var bot = await SevenTV.GetEditorEmoteSetsOfUser(BotID, context.CancellationToken);

        foreach (var editorEmoteSet in bot.EditorOf)
        {
            try
            {
                var mappedUsers = (await UserRepository.SearchMultipleByID(editorEmoteSet.EditorIDs, context.CancellationToken))
                    .Select(x => x.TwitchID)
                    .ToArray();

                var key = SevenTVService.EditorKey(editorEmoteSet.ID);
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
                Logger.Error(ex, "Failed to get editors of 7TV account {UserId}", editorEmoteSet.ID);
            }
        }
    }
}
