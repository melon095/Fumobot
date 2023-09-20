using Fumo.Shared.Enums;
using Fumo.Shared.Models;
using Fumo.Shared.Interfaces;
using MiniTwitch.Irc;
using Serilog;

namespace Fumo.Commands;

public class LeaveCommand : ChatCommand
{
    private readonly ILogger Logger;
    private readonly IChannelRepository ChannelRepository;
    private readonly IrcClient IrcClient;

    public LeaveCommand()
    {
        SetGUID("8bdfbb6c-6591-40e2-afc2-03c6354ea8d7");
        SetName("leave|part");
        SetFlags(ChatCommandFlags.BroadcasterOnly);
    }

    public LeaveCommand(ILogger logger, IChannelRepository channelRepository, IrcClient ircClient) : this()
    {
        Logger = logger.ForContext<LeaveCommand>();
        ChannelRepository = channelRepository;
        IrcClient = ircClient;
    }

    public override async ValueTask<CommandResult> Execute(CancellationToken ct)
    {
        try
        {
            await ChannelRepository.Delete(Channel, ct);

            await this.IrcClient.PartChannel(Channel.TwitchName, ct);
        }
        catch (Exception ex)
        {
            this.Logger.Error(ex, "Failed to leave {Channel}", Channel.TwitchName);
            return "An error occured, try again later";
        }

        return "👍";
    }
}
