using Fumo.Shared.Enums;
using Fumo.Shared.Models;
using Fumo.Shared.Interfaces;
using MiniTwitch.Irc;
using Serilog;
using Fumo.Shared.Eventsub;

namespace Fumo.Commands;

public class LeaveCommand : ChatCommand
{
    private readonly ILogger Logger;
    private readonly IChannelRepository ChannelRepository;
    private readonly IEventsubManager EventsubManager;
    private readonly IrcClient IrcClient;

    public LeaveCommand()
    {
        SetName("leave|part");
        SetFlags(ChatCommandFlags.BroadcasterOnly);
    }

    public LeaveCommand(ILogger logger, IChannelRepository channelRepository, IEventsubManager eventsubManager, IrcClient ircClient) : this()
    {
        Logger = logger.ForContext<LeaveCommand>();
        ChannelRepository = channelRepository;
        EventsubManager = eventsubManager;
        IrcClient = ircClient;
    }

    public override async ValueTask<CommandResult> Execute(CancellationToken ct)
    {
        try
        {
            await IrcClient.PartChannel(Channel.TwitchName, ct);
            await EventsubManager.Unsubscribe(Channel.TwitchID, EventsubType.ChannelChatMessage, ct);
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
