using Fumo.Shared.Mediator;
using Fumo.Shared.Models;
using Fumo.Shared.Repositories;
using MediatR;
using Autofac;
using Serilog.Events;
using SerilogTracing;
using Fumo.Shared;

namespace Fumo.Application.MediatorHandlers;

internal class ChatMessageNotificationCommandHandler(
    Serilog.ILogger logger,
    IUserRepository userRepository,
    IChannelRepository channelRepository,
    IMediator bus,
    MetricsTracker metricsTracker)
    : INotificationHandler<ChatMessageNotificationCommand>
{
    private readonly Serilog.ILogger Logger = logger.ForContext<ChatMessageNotificationCommandHandler>();
    private readonly IUserRepository UserRepository = userRepository;
    private readonly IChannelRepository ChannelRepository = channelRepository;
    private readonly IMediator Bus = bus;
    private readonly MetricsTracker MetricsTracker = metricsTracker;

    private static List<string> ParseMessage(string input)
    {
        const char ACTION_DENOTER = '\u0001';

        if (input.Length > 9 && input[0] == ACTION_DENOTER && input[^1] == ACTION_DENOTER)
            input = input[8..^1];

        return [.. input.Split(' ', StringSplitOptions.RemoveEmptyEntries)];
    }

    public async Task Handle(ChatMessageNotificationCommand request, CancellationToken cancellationToken)
    {
        using var enrich = Logger.PushProperties(
            ("ChannelId", request.BroadcasterId),
            ("ChannelName", request.BroadcasterLogin),
            ("UserId", request.ChatterId),
            ("UserName", request.ChatterLogin)
        );

        using var activity = Logger.StartActivity(LogEventLevel.Verbose, "Eventsub Message for {ChannelName}", request.ChatterLogin);

        try
        {
            var channel = ChannelRepository.GetByID(request.BroadcasterId);
            if (channel is null)
            {
                Logger.Warning("Channel {BroadcasterId} not in database", request.BroadcasterId);
                return;
            }

            var user = await UserRepository.SearchID(request.ChatterId, cancellationToken);

            if (user.TwitchName != request.ChatterLogin)
            {
                user.TwitchName = request.ChatterLogin;
                user.UsernameHistory.Add(new(user.TwitchName, DateTime.Now));

                await UserRepository.SaveChanges(cancellationToken);
            }

            var input = ParseMessage(request.Message.Text);

            var isBroadcaster = user.TwitchID == channel.TwitchID;

            MessageReceivedCommand message = new ChatMessage(
                channel,
                user,
                input,
                isBroadcaster,
                request.IsMod,
                request.MessageId);

            await Bus.Publish(message, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to handle message in {Channel}", request.BroadcasterName);

            activity.Complete(LogEventLevel.Error, ex);
        }
        finally
        {
            MetricsTracker.TotalMessagesRead.WithLabels(request.BroadcasterName).Inc();
        }
    }
}
