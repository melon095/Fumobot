using Fumo.Shared.Enums;
using Fumo.Shared.Models;
using Fumo.Shared.Regexes;
using Fumo.Shared.ThirdParty.Emotes.SevenTV;
using Fumo.Shared.Utils;
using StackExchange.Redis;
using System.Text.RegularExpressions;
using System.Text;
using Fumo.Shared.Repositories;

namespace Fumo.Commands.SevenTV;

public partial class SevenTVUserCommand : ChatCommand
{
    public override ChatCommandMetadata Metadata => new()
    {
        Name = "7tvu(ser)?",
        Description = "Display information about you or another 7TV user",
        Flags = ChatCommandFlags.Reply,
        Cooldown = TimeSpan.FromSeconds(10)
    };

    [GeneratedRegex("\\B(?=(\\d{3})+(?!\\d))", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex MaxSlotsRegex();

    private readonly IUserRepository UserRepository;
    private readonly ISevenTVService SevenTV;
    private readonly IDatabase Redis;

    public SevenTVUserCommand(IUserRepository userRepository, ISevenTVService sevenTV, IDatabase redis)
    {
        UserRepository = userRepository;
        SevenTV = sevenTV;
        Redis = redis;
    }

    public override async ValueTask<CommandResult> Execute(CancellationToken ct)
    {
        var user = User;

        if (Input.Count > 0)
        {
            var username = UsernameCleanerRegex.CleanUsername(Input[0].ToLower());
            user = await UserRepository.SearchName(username, ct);
        }

        var seventvUser = await SevenTV.GetUserInfo(user.TwitchID, ct);
        if (seventvUser is null)
            return "User not found";

        var emoteSet = seventvUser.EmoteSet;

        var roles = string.Join(", ", seventvUser.Roles);

        var slots = emoteSet?.Emotes?.Count ?? 0;
        var maxSlots = emoteSet?.Capacity ?? slots;

        var joinOffset = (int)(DateTimeOffset.Now.ToUnixTimeSeconds() - ((DateTimeOffset)seventvUser.CreatedAt).ToUnixTimeSeconds());
        var joinTime = new SecondsFormatter().SecondsFmt(joinOffset, limit: 4);

        return new StringBuilder()
            .Append($"{seventvUser.Username} ({user.TwitchID}) | ")
            .Append($"https://7tv.app/users/{seventvUser.SevenTVID} | ")
            .Append(string.IsNullOrEmpty(roles) ? "(No roles) | " : $"{roles} | ")
            .Append($"Joined {joinTime} ago | ")
            .Append($"Slots {slots} / {MaxSlotsRegex().Replace(maxSlots.ToString(), "_")}")
            .ToString();
    }

    public override ValueTask BuildHelp(ChatCommandHelpBuilder builder, CancellationToken ct)
        => builder
            .WithCache()
            .WithDisplayName("user")
            .WithDescription("Display information about you or another 7TV user")
            .WithUsage((x) => x.Optional("username"))
            .Finish;
}
