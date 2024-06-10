using MediatR;
using Serilog;

namespace Fumo.Shared.Eventsub.Commands;

[EventsubCommand(EventsubCommandType.Verification, "channel.chat.message")]
internal class ChannelChatMessageVerificationCommand : IRequest
{
}

internal class ChannelChatMessageVerificationCommandHandler(ILogger logger) : IRequestHandler<ChannelChatMessageVerificationCommand>
{
    private readonly ILogger Logger = logger.ForContext<ChannelChatMessageVerificationCommandHandler>();

    public Task Handle(ChannelChatMessageVerificationCommand request, CancellationToken cancellationToken)
    {
        Logger.Information("Received chat message verification: {Request}", request);

        return Task.CompletedTask;
    }
}

[EventsubCommand(EventsubCommandType.Notification, "channel.chat.message")]
internal class ChannelChatMessageNotificationCommand : IRequest
{
}

internal class ChannelChatMessageNotificationCommandHandler(ILogger logger) : IRequestHandler<ChannelChatMessageNotificationCommand>
{
    private readonly ILogger Logger = logger.ForContext<ChannelChatMessageNotificationCommandHandler>();

    public Task Handle(ChannelChatMessageNotificationCommand request, CancellationToken cancellationToken)
    {
        Logger.Information("Received chat message: {Request}", request);

        return Task.CompletedTask;
    }
}
