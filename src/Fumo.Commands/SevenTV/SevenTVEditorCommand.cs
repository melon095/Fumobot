using Fumo.Shared.Enums;
using Fumo.Shared.Exceptions;
using Fumo.Shared.Models;
using Fumo.Shared.ThirdParty.Emotes.SevenTV;
using Fumo.Shared.ThirdParty.Exceptions;
using StackExchange.Redis;
using Fumo.Shared.ThirdParty.Emotes.SevenTV.Models;
using Fumo.Shared.Repositories;

namespace Fumo.Commands.SevenTV;

public class SevenTVEditorCommand : ChatCommand
{
    public override ChatCommandMetadata Metadata => new()
    {
        Name = "(7tv)?(?(1)e|editor)",
        Description = "Add and Remove 7TV editors from the channel",
        Flags = ChatCommandFlags.BroadcasterOnly | ChatCommandFlags.Reply,
    };

    private readonly IDatabase Redis;
    private readonly ISevenTVService SevenTV;
    private readonly IUserRepository UserRepository;
    private readonly string BotID;

    public SevenTVEditorCommand(
        AppSettings settings,
        IDatabase redis,
        ISevenTVService sevenTVService,
        IUserRepository userRepository)
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
        if (ex.Message.StartsWith(SevenTVErrors.LackingPrivileges))
            return "I don't have permission to do this 👉 https://7tv.app/settings/editors 👈 'Emote Sets Manage' 'Emotes Manage' and 'User Manage Editors'";

        return ex.Message;
    }

    public override async ValueTask<CommandResult> Execute(CancellationToken ct)
    {
        var (_, UserID) = await SevenTV.EnsureCanModify(Channel, User);

        var userToMutate = await GetUser(ct);

        if (userToMutate.TwitchID == BotID)
        {
            return "FailFish";
        }

        var key = SevenTVService.EditorKey(Channel.TwitchID);
        var isAlreadyEditor = await Redis.SetContainsAsync(key, userToMutate.TwitchID);

        if (isAlreadyEditor)
        {
            try
            {
                await SevenTV.RemoveEditor(UserID, userToMutate.SevenTVID, ct);
            }
            catch (GraphQLException ex)
            {
                return HumanizeError(ex);
            }

            await Redis.SetRemoveAsync(key, userToMutate.TwitchID);

            return $"{userToMutate.Username} is no longer an editor";
        }
        else
        {
            try
            {
                await SevenTV.AddEditor(UserID, userToMutate.SevenTVID, ct);
            }
            catch (GraphQLException ex)
            {
                return HumanizeError(ex);
            }

            await Redis.SetAddAsync(key, userToMutate.TwitchID);

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
