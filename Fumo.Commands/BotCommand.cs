using Fumo.Database;
using Fumo.Database.Extensions;
using Fumo.Shared.Enums;
using Fumo.Shared.Exceptions;
using Fumo.Shared.Interfaces;
using Fumo.Shared.Models;
using Fumo.ThirdParty.Pajbot1;
using System.ComponentModel;
using System.Reflection.Metadata.Ecma335;

namespace Fumo.Commands;

public class BotCommand : ChatCommand
{
    private readonly IChannelRepository ChannelRepository;

    public BotCommand()
    {
        SetName("bot");
        SetDescription("Set various data related to you or your channel within the bot");
        SetFlags(ChatCommandFlags.Reply);

        AddParameter(new(typeof(bool), "remove"));
    }

    public BotCommand(IChannelRepository channelRepository) : this()
    {
        ChannelRepository = channelRepository;
    }

    private void AssertBroadcaster()
    {
        var isBroadcaster = Channel.TwitchID == User.TwitchID;

        if (!isBroadcaster)
        {
            throw new InvalidInputException("Only the broadcaster can do this");
        }
    }

    #region Methods

    private async Task<string> RemovePrefix(CancellationToken ct)
    {
        AssertBroadcaster();

        Channel.RemoveSetting(ChannelSettingKey.Prefix);

        await ChannelRepository.Update(Channel, ct);

        return "Removed the channel-wide prefix";
    }

    private async Task<string> SetPrefix(CancellationToken ct)
    {
        AssertBroadcaster();

        var prefix = Input.ElementAtOrDefault(1)
            ?? throw new InvalidInputException("Provide a prefix to use in this channel");

        if (prefix.Length <= 1)
        {
            throw new InvalidInputException("The prefix should be longer than one character");
        }


        Channel.SetSetting(ChannelSettingKey.Prefix, prefix);

        await ChannelRepository.Update(Channel, ct);

        return $"Set the prefix to {prefix}. (The global prefix won't work now)";
    }

    private async Task<string> RemovePajbot(CancellationToken ct)
    {
        AssertBroadcaster();

        Channel.RemoveSetting(ChannelSettingKey.Pajbot1);

        await ChannelRepository.Update(Channel, ct);

        return "Disabled checking against pajbot";
    }

    private async Task<string> SetPajbot(CancellationToken ct)
    {
        AssertBroadcaster();

        var pajbotUrl = Input.ElementAtOrDefault(1)
            ?? throw new InvalidInputException("Specify a instance");

        pajbotUrl = PajbotClient.NormalizeDomain(pajbotUrl);

        // TODO: Should this be DI injected.
        var pajbot = new PajbotClient();
        var exists = await pajbot.ValidateDomain(pajbotUrl, ct);

        if (!exists)
        {
            return "Could not find a pajbot. Either it doesn't exist or the website is not using TLS.";
        }

        Channel.SetSetting(ChannelSettingKey.Pajbot1, pajbotUrl);

        await ChannelRepository.Update(Channel, ct);

        return "Done :)";
    }

    #endregion

    public override async ValueTask<CommandResult> Execute(CancellationToken ct)
    {
        // TODO: Better error
        var command = Input.ElementAtOrDefault(0)
            ?? throw new InvalidInputException("Provide a command");

        var shouldRemove = GetArgument<bool>("remove");

#pragma warning disable format

        return (command, shouldRemove) switch
        {
            ("prefix", true )   => await RemovePrefix(ct), 
            ("prefix", false)   => await SetPrefix(ct),

            ("pajbot", true )   => await RemovePajbot(ct),
            ("pajbot", false)   => await SetPajbot(ct),
            _                   => "Not a command"
        };

#pragma warning restore format
    }
}
