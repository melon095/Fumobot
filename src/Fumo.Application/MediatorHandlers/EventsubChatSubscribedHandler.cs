using Fumo.Database.DTO;
using Fumo.Database;
using Fumo.Shared.Mediator;
using Fumo.Shared.Models;
using Fumo.Shared.Repositories;
using MediatR;
using MiniTwitch.Irc;
using Fumo.Database.Extensions;

namespace Fumo.Application.MediatorHandlers;

internal class ChannelChatMessageSubscribedCommandHandler(
        Serilog.ILogger logger,
        IChannelRepository channelRepository,
        IUserRepository userRepository,
        IMessageSenderHandler messageSenderHandler,
        IrcClient ircClient)
    : INotificationHandler<ChannelChatMessageSubscribedCommand>
{
    private const string MigratedMessage = ":) 👋 Hi again.";
    private const string JoinMessage = "FeelsDankMan 👋 Hi.";

    private readonly Serilog.ILogger Logger = logger.ForContext<ChannelChatMessageSubscribedCommandHandler>();
    private readonly IChannelRepository ChannelRepository = channelRepository;
    private readonly IUserRepository UserRepository = userRepository;
    private readonly IMessageSenderHandler MessageSenderHandler = messageSenderHandler;
    private readonly IrcClient IrcClient = ircClient;

    public async Task Handle(ChannelChatMessageSubscribedCommand request, CancellationToken ct)
    {
        var user = await UserRepository.SearchID(request.Condition.BroadcasterId, ct);
        if (user is null)
        {
            Logger.Warning("User {BroadcasterId} not in database", request.Condition.BroadcasterId);
            return;
        }

        Logger.Information("Received chat message verification: {UserID} {UserName}", user.TwitchID, user.TwitchName);

        var joinMessage = string.Empty;
        var channel = ChannelRepository.GetByID(user.TwitchID);
        if (channel is not null)
        {
            await IrcClient.PartChannel(channel.TwitchName, ct);
            channel.SetSetting(ChannelSettingKey.ConnectedWithEventsub, true.ToString());

            Logger.Information("Parted previous channel {ChannelName} from IRC", channel.TwitchName);

            joinMessage = MigratedMessage;
        }
        else
        {
            ChannelDTO newChannel = new()
            {
                TwitchID = user.TwitchID,
                TwitchName = user.TwitchName,
                Settings =
                [
                    new() { Key = ChannelSettingKey.ConnectedWithEventsub, Value = true.ToString() }
                ],
                UserTwitchID = user.TwitchID
            };

            channel = await ChannelRepository.Create(newChannel, ct);

            Logger.Information("Created new channel {ChannelName} in database", channel.TwitchName);

            joinMessage = JoinMessage;
        }

        await ChannelRepository.Update(channel, ct);

        MessageSenderHandler.ScheduleMessage(new(joinMessage, new MessageSendMethod.Helix(channel.TwitchID)));
    }
}
