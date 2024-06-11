using Fumo.BackgroundJobs.SevenTV;
using Fumo.Shared.Interfaces;
using MediatR;
using Quartz;

namespace Fumo.Shared.Mediator;

#region Created

public class OnChannelCreatedCommandHandler(IChannelRepository channelRepository, ISchedulerFactory schedulerFactory) : INotificationHandler<OnChannelCreatedCommand>
{
    private readonly IChannelRepository ChannelRepository = channelRepository;
    private readonly ISchedulerFactory SchedulerFactory = schedulerFactory;

    public async Task Handle(OnChannelCreatedCommand request, CancellationToken ct)
    {
        var scheduler = await SchedulerFactory.GetScheduler(ct);

        await scheduler.TriggerJob(new(nameof(FetchChannelEditorsJob)), ct);
        await scheduler.TriggerJob(new(nameof(FetchEmoteSetsJob)), ct);
    }

}

#endregion
