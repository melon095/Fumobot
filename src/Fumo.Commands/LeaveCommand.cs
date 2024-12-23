using Fumo.Shared.Enums;
using Fumo.Shared.Models;
using Fumo.Shared.Repositories;
using MiniTwitch.Irc;
using Serilog;

namespace Fumo.Commands;

public class LeaveCommand : ChatCommand
{
    protected override ChatCommandMetadata Metadata => new()
    {
        Name = "leave|part",
        Flags = ChatCommandFlags.BroadcasterOnly,
    };

    private readonly ILogger Logger;
    private readonly IChannelRepository ChannelRepository;
    private readonly IrcClient IrcClient;

    public LeaveCommand(ILogger logger, IChannelRepository channelRepository, IrcClient ircClient)
    {
        Logger = logger.ForContext<LeaveCommand>();
        ChannelRepository = channelRepository;
        IrcClient = ircClient;
    }

    public override async ValueTask<CommandResult> Execute(CancellationToken ct)
    {
        try
        {
            await IrcClient.PartChannel(Channel.TwitchName, ct);
            await ChannelRepository.Delete(Channel, ct);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to leave {Channel}", Channel.TwitchName);

            return "An error occured, try again later :)";
        }

        return "👍";
    }

    public override ValueTask BuildHelp(ChatCommandHelpBuilder builder, CancellationToken ct)
        => builder
            .WithCache()
            .WithDisplayName("leave")
            .Finish;
}
