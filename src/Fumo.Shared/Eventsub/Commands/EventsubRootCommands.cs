using System.Text.Json;
using MediatR;
using Serilog;

namespace Fumo.Shared.Eventsub.Commands;

#region Verification

public record EventsubVerificationCommand(EventsubSubscription Subscription) : IRequest;

public class EventsubVerificationCommandHandler(ILogger logger) : IRequestHandler<EventsubVerificationCommand>
{
    private readonly ILogger Logger = logger.ForContext<EventsubVerificationCommandHandler>();

    public Task Handle(EventsubVerificationCommand request, CancellationToken cancellationToken)
    {
        Logger.Information("Received Eventsub Verification: {Subscription}", request.Subscription);

        return Task.CompletedTask;
    }
}

#endregion

#region Revocation

public record EventsubRevocationCommand(EventsubSubscription Subscription) : IRequest;

public class EventsubRevocationCommandHandler(ILogger logger) : IRequestHandler<EventsubRevocationCommand>
{
    private readonly ILogger Logger = logger.ForContext<EventsubRevocationCommandHandler>();

    public Task Handle(EventsubRevocationCommand request, CancellationToken cancellationToken)
    {
        Logger.Information("Received Eventsub Revocation: {Subscription}", request.Subscription);

        return Task.CompletedTask;
    }
}

#endregion

#region Notification

public record EventsubNotificationCommand(EventsubSubscription Subscription, JsonDocument Event) : IRequest;

public class EventsubNotificationCommandHandler(ILogger logger, IMediator mediator) : IRequestHandler<EventsubNotificationCommand>
{
    private readonly ILogger Logger = logger.ForContext<EventsubNotificationCommandHandler>();
    private readonly IMediator Mediator = mediator;

    public Task Handle(EventsubNotificationCommand request, CancellationToken cancellationToken)
    {
        Logger.Information("Received Eventsub Notification: {Subscription}\n{Event}", request.Subscription, request.Event);

        return Task.CompletedTask;
    }
}

#endregion
