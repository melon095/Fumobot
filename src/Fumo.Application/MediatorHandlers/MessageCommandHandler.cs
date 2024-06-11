using Fumo.Shared.MediatorCommands;
using MediatR;

namespace Fumo.Shared.Mediator;

public class MessageCommandHandler : INotificationHandler<MessageCommand>
{
    private readonly Serilog.ILogger Logger;

    public MessageCommandHandler(Serilog.ILogger logger)
    {
        Logger = logger.ForContext<MessageCommandHandler>();
    }

    public Task Handle(MessageCommand notification, CancellationToken cancellationToken)
    {
        Logger.Information("Received message: {Message}", notification.Message);

        return Task.CompletedTask;
    }
}