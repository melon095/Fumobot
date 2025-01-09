using Fumo.Database;
using Fumo.Database.Extensions;
using Fumo.Shared.Enums;
using Fumo.Shared.Exceptions;
using Fumo.Shared.Models;
using Fumo.Shared.Repositories;
using Fumo.Shared.ThirdParty.Pajbot1;

namespace Fumo.Commands;

public class BotCommand : ChatCommand
{
    protected override List<Parameter> Parameters =>
    [
        MakeParameter<bool>("remove")
    ];

    public override ChatCommandMetadata Metadata => new()
    {
        Name = "bot",
        Description = "Set various data related to you or your channel within the bot",
        Flags = ChatCommandFlags.Reply,
    };

    private readonly IChannelRepository ChannelRepository;

    public BotCommand(IChannelRepository channelRepository)
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

    private async ValueTask<string> RemovePrefix(CancellationToken ct)
    {
        AssertBroadcaster();

        Channel.RemoveSetting(ChannelSettingKey.Prefix);

        await ChannelRepository.Update(Channel, ct);

        return "Removed the channel-wide prefix";
    }

    private async ValueTask<string> SetPrefix(CancellationToken ct)
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

    private async ValueTask<string> RemovePajbot(CancellationToken ct)
    {
        AssertBroadcaster();

        Channel.RemoveSetting(ChannelSettingKey.Pajbot1);

        await ChannelRepository.Update(Channel, ct);

        return "Disabled checking against pajbot";
    }

    private async ValueTask<string> SetPajbot(CancellationToken ct)
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

    public override ValueTask BuildHelp(ChatCommandHelpBuilder builder, CancellationToken ct)
        => builder
            .WithCache()
            .WithDisplayName("bot")
            .WithDescription("Set various data related to you or your channel within the bot")
            .WithUsage((x) => x.Required("subcommand").Required("args"))
            .WithArgument("remove", (x) => x.Description = "Add this flag to remove the data, rather than inserting")
            .WithSubcommand("prefix", (x) =>
            {
                x.WithUsage((y) => y.Required("prefix"));
                x.WithDescription($"Set the prefix used in the channel. The global prefix will {MarkdownHelper.Bold("NOT")} work during this.");
            })
            .WithSubcommand("pajbot", (x) =>
            {
                x.WithUsage((y) => y.Required("domain"));
                x.WithDescription($@"
Tell the bot to check against your pajbot instance.
The input should be the {MarkdownHelper.Italic("Domain name")}. E.g. `pajbot.example.com`.
The remote server has to be served with TLS.");
            })
            .Finish;
}
