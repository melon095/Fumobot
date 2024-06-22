using Fumo.Database;
using Fumo.Database.Extensions;
using Fumo.Shared.Exceptions;
using Fumo.Shared.Interfaces;
using Fumo.Shared.Models;
using Fumo.Shared.Utils;
using Fumo.Shared.ThirdParty.Emotes.SevenTV;
using System.Collections.Immutable;
using Fumo.Shared.ThirdParty.Emotes.SevenTV.Enums;
using Fumo.Shared.ThirdParty.Emotes.SevenTV.Models;

namespace Fumo.Commands.SevenTV;

public class SevenTVYoinkCommand : ChatCommand
{
    private static readonly char[] ChannelPrefixes = ['@', '#'];

    private readonly ISevenTVService SevenTVService;
    private readonly IMessageSenderHandler MessageSender;
    private readonly IUserRepository UserRepository;

    public SevenTVYoinkCommand()
    {
        SetName("(7tv)?yoink|steal");
        SetDescription("Yoink emotes from another channel");

        AddParameter(new(typeof(bool), "alias"));
        AddParameter(new(typeof(bool), "case"));
    }

    public SevenTVYoinkCommand(
        ISevenTVService sevenTVService,
        IMessageSenderHandler messageSender,
        IUserRepository userRepository) : this()
    {
        SevenTVService = sevenTVService;
        MessageSender = messageSender;
        UserRepository = userRepository;
    }

    private async ValueTask<string> ConvertToEmoteSet(string channel, CancellationToken ct)
    {
        var user = await UserRepository.SearchName(channel, ct);

        var seventvUser = await SevenTVService.GetUserInfo(user.TwitchID, ct);

        var con = seventvUser.Connections.GetTwitchConnection();

        if (string.IsNullOrEmpty(con.EmoteSetId)) throw new InvalidInputException("User or Channel does not have a 7TV account");

        return con.EmoteSetId;
    }

    public override async ValueTask<CommandResult> Execute(CancellationToken ct)
    {
        var keepAlias = GetArgument<bool>("alias");
        var isCaseSensitive = GetArgument<bool>("case");

        var stringComparer = isCaseSensitive
            ? StringComparer.Ordinal
            : StringComparer.OrdinalIgnoreCase;

        if (Input.Count <= 0)
        {
            throw new InvalidInputException("Provide emotes to take, if you want them taken from a different channel provide a channel name prefixed with @ or #. e.g @forsen");
        }

        var chanIdx = Input.FindIndex((x => ChannelPrefixes.Contains(x[0])));

        HashSet<string> emotesWant = [];

        for (var i = 0; i < Input.Count; i++)
        {
            if (i == chanIdx) continue;

            var input = Input.ElementAt(i);

            emotesWant.Add(input);
        }

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
            await SevenTVService.EnsureCanModify(Channel, User);

            writeSet = Channel.GetSetting(ChannelSettingKey.SevenTV_EmoteSet);
        }

        readSet ??= await ConvertToEmoteSet(readChannel, ct);
        writeSet ??= await ConvertToEmoteSet(writeChannel, ct);

        var toAdd = (await SevenTVService.GetEnabledEmotes(readSet, ct))
            .Where(x => emotesWant.Contains(x.Name, stringComparer))
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

                var name = await SevenTVService.ModifyEmoteSet(writeSet, ListItemAction.Add, emote.ID, aliasName, ct) ?? throw new Exception("Idk what happened");

                MessageSender.ScheduleMessage(new(Channel.TwitchName, $"👍 Added {name} {writeChannelPrompt}"));
            }
            catch (Exception ex)
            {
                var e = emote.Name;
                if (emote.HasAlias)
                {
                    e += $" (alias of {emote.Name})";
                }

                MessageSender.ScheduleMessage(new(Channel.TwitchName, $"👎 Failed to add {e} {ex.Message} {writeChannelPrompt}"));
            }
        }

        return string.Empty;
    }

    public override ValueTask BuildHelp(ChatCommandHelpBuilder builder, CancellationToken ct)
        => builder
            .WithCache()
            .WithDisplayName("yoink")
            .WithDescription(@"
Steal emotes from another channel

The 'yoink' command offers bidirectional emote addition. 
Without specifying a channel, it transfers emotes from the current channel to your own. 
Conversely, specifying a channel (e.g., @forsen) retrieves emotes from 'forsen' and adds them to the current channel.")
            .WithUsage(x => x.Required("#channel").Required("emote_names..."))
            .WithExample("#forsen DankG")
            .WithExample("FloppaDank FloppaL #forsen")
            .WithExample("peepoDank", "Copies the emotes provided in the current channel into your own channel.")
            .WithArgument("alias", x =>
            {
                x.Description = "Preserve the original emote name as an alias";
            })
            .WithArgument("case", x =>
            {
                x.Description = "Takes emotes with case sensitivity in mind";
            })
            .Finish;
}
