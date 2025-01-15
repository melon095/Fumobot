using Fumo.BackgroundJobs.SevenTV;
using Fumo.Database;
using Fumo.Database.Extensions;
using Fumo.Shared.Mediator;
using Fumo.Shared.Models;
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

public class OnChannelDeletedCommandHandler(Serilog.ILogger logger, IMessageSenderHandler messageSenderHandler)
    : INotificationHandler<OnChannelDeletedCommand>
{
    public Task Handle(OnChannelDeletedCommand request, CancellationToken ct)
    {
        var log = logger.ForContext<OnChannelDeletedCommandHandler>();

        log.Information("Channel {ChannelName} deleted", request.Channel.TwitchName);

        MessageSendMethod method = request.Channel.GetSettingBool(ChannelSettingKey.ConnectedWithEventsub) switch
        {
            true => new MessageSendMethod.Helix(request.Channel.TwitchID),
            false => new MessageSendMethod.Irc(request.Channel.TwitchName)
        };

        messageSenderHandler.Cleanup(method);

        return Task.CompletedTask;
    }
}

#endregion
