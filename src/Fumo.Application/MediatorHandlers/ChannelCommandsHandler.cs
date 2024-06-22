using Fumo.BackgroundJobs.SevenTV;
using Fumo.Shared.Interfaces;
using Fumo.Shared.Mediator;
using MediatR;
using Quartz;

namespace Fumo.Application.MediatorHandlers;

#region Created

public class OnChannelCreatedCommandHandler(ISchedulerFactory schedulerFactory) : INotificationHandler<OnChannelCreatedCommand>
{
    private readonly ISchedulerFactory SchedulerFactory = schedulerFactory;

    public async Task Handle(OnChannelCreatedCommand request, CancellationToken ct)
    {
        var scheduler = await SchedulerFactory.GetScheduler(ct);

        await scheduler.TriggerJob(new(nameof(FetchChannelEditorsJob)), ct);
        await scheduler.TriggerJob(new(nameof(FetchEmoteSetsJob)), ct);
    }
}

#endregion

#region Deleted

public class OnChannelDeletedCommandHandler(IMessageSenderHandler messageSenderHandler) : INotificationHandler<OnChannelDeletedCommand>
{
    public Task Handle(OnChannelDeletedCommand request, CancellationToken ct)
    {
        messageSenderHandler.Cleanup(request.Channel.TwitchID);

        return Task.CompletedTask;
    }
}

#endregion
