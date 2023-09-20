using Fumo.Commands;
using Fumo.Commands.SevenTV;
using Fumo.Shared.Models;
using Fumo.Shared.Repositories;
using System.Collections.Concurrent;

namespace Fumo.WebService.Service;

public class DescriptionService
{
    private delegate string DescriptionDelegate();

    private readonly ConcurrentDictionary<Type, DescriptionDelegate> delegates = new();

    public DescriptionService()
    {
        Add<SevenTVYoinkCommand>(SVTYoinkDelegate);
        Add<SevenTVSearchCommand>(SVTSearchDelegate);
        Add<SevenTVRemoveCommand>(SVTRemoveDelegate);
        Add<SevenTVAddCommand>(SVTAddDelegate);
        Add<SevenTVAliasCommand>(SVTAliasDelegate);
        Add<SevenTVEditorCommand>(SVTEditorDelegate);
    }

    #region Delegates

    private string SVTAddDelegate()
    => """
        "Add a 7TV emote"
        $"**Usage**: %PREFIX%add <emote>"
        $"**Usage**: %PREFIX%add FloppaL"
        ""
        "You can also add emotes by ID or URL"
        $"**Example**: %PREFIX%add 60aeab8df6a2c3b332d21139"
        $"**Example**: %PREFIX%add https://7tv.app/emotes/60aeab8df6a2c3b332d21139"
        ""
        "-a, --alias <alias>"
        "%TAB%Set an alias for the emote"
        ""
        "-e, --exact"
        "%TAB%Search for an exact match"
        ""
        ""
        "**Required 7TV Permissions**"
        "Modify Emotes"
        """;

    private string SVTAliasDelegate()
    => """
        "Set or Reset the alias of an emote",
        "",
        $"**Usage**: %PREFIX%alias <emote> [alias]",
        $"**Example**: %PREFIX%alias Floppal xqcL",
        $"**Example**: %PREFIX%alias FloppaL",
        "%TAB%Removes the alias from the FloppaL emote",
        "",
        "**Required 7TV Flags**",
        "Modify Emotes"
        """;

    private string SVTEditorDelegate()
    => """
        "This command allows the broadcaster to add and remove users as 7TV editors",
        "",
        $"**Usage**: %PREFIX%editor <username>",
        $"**Example**: %PREFIX%editor forsen",
        "",
        "",
        "Required 7TV Flags",
        "Manage Editors",
        """;

    private string SVTRemoveDelegate()
    => """
        "Removes 7TV emotes from your emote set",
        $"Usage: %PREFIX%remove <emote names>",
        "",
        "**Required 7TV Permissions**",
        "Manage Emotes",
        """;

    private string SVTSearchDelegate()
    => """
        "Search up 7TV emotes in chat"
        $"**Usage**: %PREFIX%7tv <search term>"
        $"**Example**: %PREFIX%7tv Apu"
        ""
        "-e, --exact"
        "%TAB%Search for an exact match"
        ""
        "-u, --uploader <name>"
        "%TAB%Search for emotes by a specific uploader"
        "%TAB%Requires their current Twitch username"
        """;


    private string SVTYoinkDelegate()
    => """
        "Steal emotes from another channel"
        ""
        $"**Usage:**: %PREFIX% yoink #channel <emote names>"
        $"**Example**: %PREFIX% yoink #pajlada WideDankCrouching"
        $"**Example**: %PREFIX% yoink @forsen FloppaDank FloppaL"
        $"**Example**: %PREFIX% yoink 30Dank @forsen"
        $"**Example**: %PREFIX% yoink DankG"
        ""
        "The yoink command has the ability to add emote both ways, if you do not include a channel the emotes are taken from the current channel and added to your own channel."
        "While adding a channel e.g (@forsen) would take emotes from forsen and add them to the current channel."
        ""
        "-a, --alias"
        "%TAB%By default emotes have their aliases removed, -a will retain the alias"
        """;

    #endregion

    private void Add<TCommand>(DescriptionDelegate @delegate) where TCommand : ChatCommand
        => this.delegates.TryAdd(typeof(TCommand), @delegate);

    public string? CreateDescription(ChatCommand command)
    {
        if (delegates.TryGetValue(command.GetType(), out var @delegate))
        {
            return @delegate();
        }

        return null;
    }
}
