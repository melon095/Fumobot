using Fumo.Shared.Exceptions;
using Fumo.Shared.Models;
using Fumo.Shared.ThirdParty.Emotes.SevenTV;
using Fumo.Shared.ThirdParty.Exceptions;
using Serilog;
using System.Text;
using Fumo.Shared.ThirdParty.Emotes.SevenTV.Enums;
using Fumo.Shared.ThirdParty.Emotes.SevenTV.Models;

namespace Fumo.Commands.SevenTV;

public class SevenTVRemoveCommand : ChatCommand
{
    private readonly ILogger Logger;
    private readonly ISevenTVService SevenTVService;
    private readonly IMessageSenderHandler MessageSender;

    public SevenTVRemoveCommand()
    {
        SetName("(7tv)?remove");
        SetDescription("Remove 7TV emotes");
    }

    public SevenTVRemoveCommand(
        ILogger logger,
        ISevenTVService sevenTVService,
        IMessageSenderHandler messageSender) : this()
    {
        Logger = logger.ForContext<SevenTVRemoveCommand>();
        SevenTVService = sevenTVService;
        MessageSender = messageSender;
    }

    public override async ValueTask<CommandResult> Execute(CancellationToken ct)
    {
        StringBuilder output = new();

        var aaaa = await SevenTVService.EnsureCanModify(Channel, User);

        if (Input.Count <= 0)
        {
            throw new InvalidInputException("Provide emotes to remove");
        }

        List<SevenTVEnabledEmote> emotesToRemove = new();
        var currentEmotes = await SevenTVService.GetEnabledEmotes(aaaa.EmoteSet, ct);

        foreach (var emote in currentEmotes)
        {
            if (!Input.Remove(emote.Name)) continue;

            emotesToRemove.Add(emote);

            if (Input.Count <= 0) break;
        }

        // Some emotes could not be found
        if (Input.Count > 0)
        {
            output.Append($"Could not find the following emote{(Input.Count > 1 ? "s" : "")}");
            Input.ForEach(x => output.Append($" {x}"));

            MessageSender.ScheduleMessageWithBanphraseCheck(
                new(Channel.TwitchID, output.ToString()), Channel);

            output.Clear();

            if (emotesToRemove.Count <= 0)
            {
                return string.Empty;
            }
        }

        List<string> failedToRemove = new();

        foreach (var emote in emotesToRemove)
        {
            try
            {
                await SevenTVService.ModifyEmoteSet(aaaa.EmoteSet, ListItemAction.Remove, emote.ID, ct: ct);
            }
            catch (GraphQLException ex)
            {
                Logger.Warning(ex, "Failed to remove emote {Emote} in {Channel}", emote.Name, Channel.TwitchName);

                failedToRemove.Add(emote.Name);
            }
        }

        if (failedToRemove.Count > 0)
        {
            output.Append($"\u2022 Failed to remove the following emotes:");

            failedToRemove.ForEach(x => output.Append($" {x}"));
        }
        else
        {
            output.Append("All specified emotes were successfully removed");
        }

        return output.ToString();
    }

    public override ValueTask BuildHelp(ChatCommandHelpBuilder builder, CancellationToken ct)
        => builder
            .WithCache()
            .WithDisplayName("remove")
            .WithDescription("Remove 7TV emotes")
            .WithUsage((x) => x.Required("emotes..."))
            .Finish;
}
