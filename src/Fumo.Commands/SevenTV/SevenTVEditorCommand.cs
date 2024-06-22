using Fumo.Shared.Enums;
using Fumo.Shared.Exceptions;
using Fumo.Shared.Models;
using Fumo.Shared.ThirdParty.Emotes.SevenTV;
using Fumo.Shared.ThirdParty.Exceptions;
using StackExchange.Redis;
using Fumo.Shared.ThirdParty.Emotes.SevenTV.Enums;
using Fumo.Shared.ThirdParty.Emotes.SevenTV.Models;
using Fumo.Shared.Repositories;

namespace Fumo.Commands.SevenTV;

public class SevenTVEditorCommand : ChatCommand
{
    private readonly IDatabase Redis;
    private readonly ISevenTVService SevenTV;
    private readonly IUserRepository UserRepository;
    private readonly string BotID;

    public SevenTVEditorCommand()
    {
        // Surely this works
        SetName("(7tv)?(?(1)e|editor)");
        SetDescription("Add and Remove 7TV editors from the channel");

        SetFlags(ChatCommandFlags.BroadcasterOnly | ChatCommandFlags.Reply);
    }

    public SevenTVEditorCommand(
        AppSettings settings,
        IDatabase redis,
        ISevenTVService sevenTVService,
        IUserRepository userRepository) : this()
    {
        Redis = redis;
        SevenTV = sevenTVService;
        UserRepository = userRepository;
        BotID = settings.Twitch.UserID;
    }

    private async ValueTask<SevenTVUser> GetUser(CancellationToken ct)
    {
        var username = Input.ElementAtOrDefault(0) ?? throw new InvalidInputException("Provide a username to add or remove");

        var user = await UserRepository.SearchName(username, ct);

        return await SevenTV.GetUserInfo(user.TwitchID, ct);
    }

    private static string HumanizeError(GraphQLException ex)
    {
        if (ex.Message.StartsWith("70403")) return "I don't have permission to do this";

        return ex.Message;
    }

    public override async ValueTask<CommandResult> Execute(CancellationToken ct)
    {
        var (_, UserID) = await SevenTV.EnsureCanModify(Channel, User);

        var userToMutate = await GetUser(ct);
        var twitchId = userToMutate.Connections.GetTwitchConnection().ID;

        if (twitchId == BotID)
        {
            return "FailFish";
        }

        var key = SevenTVService.EditorKey(Channel.TwitchID);
        var isAlreadyEditor = await Redis.SetContainsAsync(key, twitchId);

        if (isAlreadyEditor)
        {
            try
            {
                await SevenTV.ModifyEditorPermissions(UserID, userToMutate.ID, UserEditorPermissions.None, ct);
            }
            catch (GraphQLException ex)
            {
                return HumanizeError(ex);
            }

            await Redis.SetRemoveAsync(key, twitchId);

            return $"{userToMutate.Username} is no longer an editor";
        }
        else
        {
            try
            {
                await SevenTV.ModifyEditorPermissions(UserID, userToMutate.ID, UserEditorPermissions.Default, ct);
            }
            catch (GraphQLException ex)
            {
                return HumanizeError(ex);
            }

            await Redis.SetAddAsync(key, twitchId);

            return $"{userToMutate.Username} is now an editor";
        }
    }

    public override ValueTask BuildHelp(ChatCommandHelpBuilder builder, CancellationToken ct)
        => builder
            .WithCache()
            .WithDisplayName("editor")
            .WithDescription("Add and Remove 7TV editors from your channel")
            .WithUsage((x) => x.Required("username"))
            .WithExample("forsen")
            .Finish;
}
