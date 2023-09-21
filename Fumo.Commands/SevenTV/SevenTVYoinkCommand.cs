using Fumo.Database;
using Fumo.Database.Extensions;
using Fumo.Shared.Exceptions;
using Fumo.Shared.Extensions;
using Fumo.Shared.Interfaces;
using Fumo.Shared.Models;
using Fumo.Shared.Utils;
using Fumo.ThirdParty.Emotes.SevenTV;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using System.Collections.Immutable;
using Fumo.ThirdParty.Emotes.SevenTV.Enums;
using Fumo.ThirdParty.Emotes.SevenTV.Models;

namespace Fumo.Commands.SevenTV;

public class SevenTVYoinkCommand : ChatCommand
{
    private static readonly char[] ChannelPrefixes = new[] { '@', '#' };

    private readonly ISevenTVService SevenTVService;
    private readonly IDatabase Redis;
    private readonly IMessageSenderHandler MessageSender;
    private readonly IUserRepository UserRepository;

    private string BotID { get; }

    public SevenTVYoinkCommand()
    {
        SetName("(7tv)?yoink|steal");
        SetDescription("Yoink emotes from another channel");

        AddParameter(new(typeof(bool), "alias"));
        AddParameter(new(typeof(bool), "case"));
    }

    public SevenTVYoinkCommand(
        ISevenTVService sevenTVService,
        IDatabase redis,
        IMessageSenderHandler messageSender,
        IConfiguration configuration,
        IUserRepository userRepository) : this()
    {
        SevenTVService = sevenTVService;
        Redis = redis;
        MessageSender = messageSender;
        UserRepository = userRepository;

        BotID = configuration["Twitch:UserID"]!;
    }

    private async Task<string> ConvertToEmoteSet(string channel, CancellationToken ct)
    {
        var user = await UserRepository.SearchNameAsync(channel, ct);

        var seventvUser = await SevenTVService.GetUserInfo(user.TwitchID, ct);

        var con = seventvUser.Connections.GetTwitchConnection();

        if (string.IsNullOrEmpty(con.EmoteSetId)) throw new InvalidInputException("User or Channel does not have a 7TV account");

        return con.EmoteSetId;
    }

    public override async ValueTask<CommandResult> Execute(CancellationToken ct)
    {
        var keepAlias = GetArgument<bool>("alias");
        var isCaseSensitive = GetArgument<bool>("case");

        if (Input.Count <= 0)
        {
            throw new InvalidInputException("Provide emotes to take, if you want them taken from a different channel provide a channel name prefixed with @ or #. e.g @forsen");
        }

        var chanIdx = Input.FindIndex((x => ChannelPrefixes.Contains(x[0])));

        var emotesWant = Input.Select((emote, idx) =>
            {
                if (idx == chanIdx) return null;

                return emote;
            })
            .Where(x => x != null)
            .ToImmutableHashSet();

        string writeChannel, readChannel;

        // No channel was given
        if (chanIdx == -1)
        {
            writeChannel = User.TwitchName;
            readChannel = Channel.TwitchName;
        }
        else
        {
            writeChannel = Channel.TwitchName;
            readChannel = Input.ElementAt(chanIdx)[1..];
        }

        if (writeChannel == readChannel)
        {
            // TODO: use sillE 
            throw new InvalidInputException("You can't steal from yourself silly");
        }

        string? readSet = null, writeSet = null;

        if (Channel.TwitchName == readChannel)
        {
            readSet = Channel.GetSetting(ChannelSettingKey.SevenTV_EmoteSet);

        }
        else if (Channel.TwitchName == writeChannel)
        {
            await SevenTVService.EnsureCanModify(BotID, Redis, Channel, User);

            writeSet = Channel.GetSetting(ChannelSettingKey.SevenTV_EmoteSet);
        }

        readSet ??= await ConvertToEmoteSet(readChannel, ct);
        writeSet ??= await ConvertToEmoteSet(writeChannel, ct);

        var toAdd = (await SevenTVService.GetEnabledEmotes(readSet, ct))
            .Where(x =>
            {
                return isCaseSensitive
                    ? emotesWant.Contains(x.Name)
                    : emotesWant.Contains(x.Name.ToLowerInvariant());
            })
            .ToList();

        if (toAdd.Count <= 0)
        {
            return "Could not find any emotes to add";
        }

        var writeChannelPrompt = Channel.TwitchName == writeChannel
            ? ""
            : $" (in #{UnpingUser.Unping(writeChannel)})";

        // Would have used Parallel.ForEachAsync but that's very slow for few items it seems like.
        foreach (var emote in toAdd)
        {
            try
            {
                var aliasName = keepAlias ? emote.Name : null;

                var name = await SevenTVService.ModifyEmoteSet(writeSet, ListItemAction.Add, emote.Id, aliasName, ct) ?? throw new Exception("Idk what happened");

                MessageSender.ScheduleMessage(Channel.TwitchName, $"👍 Added {name} {writeChannelPrompt}");
            }
            catch (Exception ex)
            {
                var e = emote.Name;
                if (emote.HasAlias)
                {
                    e += $" (alias of {emote.Name})";
                }

                MessageSender.ScheduleMessage(Channel.TwitchName, $"👎 Failed to add {e} {ex.Message} {writeChannelPrompt}");
            }
        }

        return string.Empty;
    }

    public override ValueTask<List<string>> GenerateWebsiteDescription(string prefix, CancellationToken ct)
    {
        List<string> strings = new()
        {
            "Steal emotes from another channel",
            "",
            $"**Usage:**: {prefix} yoink #channel <emote names>",
            $"**Example**: {prefix} yoink #pajlada WideDankCrouching",
            $"**Example**: {prefix} yoink @forsen FloppaDank FloppaL",
            $"**Example**: {prefix} yoink 30Dank @forsen",
            $"**Example**: {prefix} yoink DankG",
            "",
            "The yoink command has the ability to add emote both ways, if you do not include a channel the emotes are taken from the current channel and added to your own channel.",
            "While adding a channel e.g (@forsen) would take emotes from forsen and add them to the current channel.",
            "",
            "Emotes are fetched case insensitive by default",
            "",
            "-a, --alias",
            "%TAB%By default emotes have their aliases removed, -a will retain the alias",
            "",
            "-c, --case",
            "%TAB%Check emotes by case sensitivity",
        };

        return ValueTask.FromResult(strings);
    }
}
