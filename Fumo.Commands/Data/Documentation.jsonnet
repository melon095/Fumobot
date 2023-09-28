local Namespace = "Fumo.Commands.";
local C(class) = Namespace + class;
local S7TV(class) = Namespace + "SevenTV." + class;

{
    "d7456d3b-e819-4e57-9d4e-2050799b19f1": {
        "Class": C("PingCommand")
    },
    "8bdfbb6c-6591-40e2-afc2-03c6354ea8d7": {
        "Class": C("LeaveCommand")
    },
    "fa4b9629-7339-44f9-a3c1-273e129382c8": {
        "Class": C("JoinCommand")
    },
    "edb41ac3-6a0e-4be3-98eb-1a8e3f8ec505": {
        "Class": C("HelpCommand")
    },
    "e0395240-d1c5-4331-b73c-b89a04b2124a": {
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
    "c6d33008-6daa-474a-84f9-3de65a5a099e": {
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
    "95278982-014e-4994-b93c-dc422a73c264": {
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
    "80c14b78-b66c-446c-899a-07fc54c40e90": {
        "Class": S7TV("SevenTVEditorCommand"),
        "Doc": |||
            This command allows the broadcaster to add and remove users as 7TV editors
            
            **Usage**: %PREFIX% editor <username>
            **Example**: %PREFIX% editor forsen
            
            **Required 7TV Flags**
            Manage Editors
        |||
    },
    "172698b3-50b7-4baf-8943-6712df480955": {
        "Class": S7TV("SevenTVRemoveCommand"),
        "Doc": |||
            Removes 7TV emotes from your emote set
            Usage: %PREFIX% remove <emote names>
            
            **Required 7TV Permissions**
            Manage Emotes
        |||
    },
    "2b7f8558-3fa1-45c8-ab84-24665fc04da2": {
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
    "811f1a71-0f31-42c3-9c94-0abe1fea5f73": {
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
    "95db80b4-06b5-49cc-ba10-8f4d04374a6e": {
        "Class": S7TV("SevenTVUserCommand")
    }
}
