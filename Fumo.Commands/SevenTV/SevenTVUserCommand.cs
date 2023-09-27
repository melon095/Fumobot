using Fumo.Shared.Enums;
using Fumo.Shared.Interfaces;
using Fumo.Shared.Models;
using Fumo.Shared.Regexes;
using Fumo.ThirdParty.Emotes.SevenTV;
using Fumo.Shared.Utils;
using StackExchange.Redis;
using System.Text.Json;
using System.Text.RegularExpressions;
using Fumo.ThirdParty.Emotes.SevenTV.Models;
using System.Text;

namespace Fumo.Commands.SevenTV;

public partial class SevenTVUserCommand : ChatCommand
{
    [GeneratedRegex("\\B(?=(\\d{3})+(?!\\d))", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex MaxSlotsRegex();

    private readonly IUserRepository UserRepository;
    private readonly ISevenTVService SevenTV;
    private readonly IDatabase Redis;

    public SevenTVUserCommand()
    {
        SetName("7tvu(ser)?");
        SetDescription("Display information about you or another 7TV user");
        SetFlags(ChatCommandFlags.Reply);
        SetCooldown(TimeSpan.FromSeconds(10));
    }

    public SevenTVUserCommand(IUserRepository userRepository, ISevenTVService sevenTV, IDatabase redis) : this()
    {
        UserRepository = userRepository;
        SevenTV = sevenTV;
        Redis = redis;
    }

    private async Task<IEnumerable<string>> GetRoles(IEnumerable<string> userRoles)
    {
        var roles = await Redis.StringGetAsync("seventv:roles");

        return JsonSerializer.Deserialize<SevenTVRoles>(roles!)!
            .Roles
            .Where(x => userRoles.Contains(x.Name) && x.Name != "Default")
            .Select(x => x.Name);
    }

    public override async ValueTask<CommandResult> Execute(CancellationToken ct)
    {
        var user = User;

        if (Input.Count > 0)
        {
            var username = UsernameCleanerRegex.CleanUsername(Input[0].ToLower());
            user = await UserRepository.SearchNameAsync(username, ct);
        }

        SevenTVUser seventvUser = await SevenTV.GetUserInfo(user.TwitchID, ct);

        var roles = string.Join(", ", await GetRoles(seventvUser.Roles));

        var emoteSet = seventvUser.DefaultEmoteSet();

        var slots = emoteSet?.Emotes?.Count ?? 0;
        var maxSlots = emoteSet?.Capacity ?? slots;

        var joinOffset = (int)(DateTimeOffset.Now.ToUnixTimeSeconds() - ((DateTimeOffset)seventvUser.CreatedAt).ToUnixTimeSeconds());
        var joinTime = new SecondsFormatter().SecondsFmt(joinOffset, limit: 4);

        return new StringBuilder()
            .Append($"{seventvUser.Username} ({user.TwitchID}) | ")
            .Append($"https://7tv.app/users/{seventvUser.Id} | ")
            .Append(string.IsNullOrEmpty(roles) ? "(No roles) | " : $"{roles} | ")
            .Append($"Joined {joinTime} ago | ")
            .Append($"Slots {slots} / {MaxSlotsRegex().Replace(maxSlots.ToString(), "_")}")
            .ToString();
    }
}
