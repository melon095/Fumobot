local Namespace = "Fumo.Commands.";
local C(class) = Namespace + class;
local S7TV(class) = Namespace + "SevenTV." + class;

{
    "ping": {
        "Class": C("PingCommand")
    },
    "leave": {
        "Class": C("LeaveCommand")
    },
    "join": {
        "Class": C("JoinCommand")
    },
    "help": {
        "Class": C("HelpCommand")
    },
    "bot": {
        "Class": C("BotCommand"),
        "Doc": |||
            Set various data related to you or your channel.

            **Usage**: %PREFIX% bot <subcommand> <args>

            **Subcommands**:
            
            %PREFIX% bot prefix <prefix>
                Set the prefix used in your channel. The global prefix will not work when a channel prefix is set.

            %PREFIX% bot pajbot
                If you have a pajbot instance running in your channel, you tell the bot to check it's messages against the pajbot banphrases.

            **Arguments**:
            
            -r, --remove
                Add this flag to remove the data instead of setting it.
        |||
    },
    "add": {
        "Class": S7TV("SevenTVAddCommand"),
        "Doc": |||
            Add a 7TV emote
            **Usage**: %PREFIX% add <emote>
            **Usage**: %PREFIX% add FloppaL
            
            You can also add emotes by ID or URL
            **Example**: %PREFIX% add 60aeab8df6a2c3b332d21139
            **Example**: %PREFIX% add https://7tv.app/emotes/60aeab8df6a2c3b332d21139
            
            **Arguments**

            -a, --alias <alias>
                Set an alias for the emote
            
            -e, --exact
                Search for an exact match
            
            **Required 7TV Permissions**
            Modify Emotes
        |||
    },
    "alias": {
        "Class": S7TV("SevenTVAliasCommand"),
        "Doc": |||
            Set or Reset the alias of an emote"
            
            **Usage**: %PREFIX% alias <emote> [alias]
            **Example**: %PREFIX% alias Floppal xqcL
            **Example**: %PREFIX% alias FloppaL
                Removes the alias from the FloppaL emote
            
            **Required 7TV Flags**
            Modify Emotes
        |||
    },
    "editor": {
        "Class": S7TV("SevenTVEditorCommand"),
        "Doc": |||
            This command allows the broadcaster to add and remove users as 7TV editors
            
            **Usage**: %PREFIX% editor <username>
            **Example**: %PREFIX% editor forsen
            
            **Required 7TV Flags**
            Manage Editors
        |||
    },
    "remove": {
        "Class": S7TV("SevenTVRemoveCommand"),
        "Doc": |||
            Removes 7TV emotes from your emote set
            Usage: %PREFIX% remove <emote names>
            
            **Required 7TV Permissions**
            Manage Emotes
        |||
    },
    "7tv": {
        "Class": S7TV("SevenTVSearchCommand"),
        "Doc": |||
            Search up 7TV emotes in chat
            **Usage**: %PREFIX% 7tv <search term>
            **Example**: %PREFIX% 7tv Apu
            
            **Arguments**
            
            -e, --exact
                Search for an exact match
            
            -u, --uploader <name>
                Search for emotes by a specific uploader
                Requires their current Twitch username
        |||
    },
    "yoink": {
        "Class": S7TV("SevenTVYoinkCommand"),
        "Doc": |||
            Steal emotes from another channel
            
            **Usage:**: %PREFIX% yoink #channel <emote names>
            **Example**: %PREFIX% yoink #pajlada WideDankCrouching
            **Example**: %PREFIX% yoink @forsen FloppaDank FloppaL
            **Example**: %PREFIX% yoink 30Dank @forsen
            **Example**: %PREFIX% yoink DankG
            
            The yoink command has the ability to add emote both ways, if you do not include a channel the emotes are taken from the current channel and added to your own channel.
            While adding a channel e.g (@forsen) would take emotes from forsen and add them to the current channel.
            
            **Arguments**:

            -a, --alias
                By default emotes have their aliases removed, -a will retain the alias,

            -c, --case
                Check emotes by case sensitivity
        |||
    },
    "7tvu": {
        "Class": S7TV("SevenTVUserCommand")
    }
}
