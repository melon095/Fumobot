using Fumo.Application.BackgroundJobs;
using Fumo.Application.BackgroundJobs.SevenTV;
using Quartz;
using System.Collections.ObjectModel;

namespace Fumo.Application.Startable;

internal class BackgroundJobStarter
{
    private static readonly ReadOnlyCollection<Func<(IJobDetail, List<ITrigger>)>> _jobFactories = new(new Func<(IJobDetail, List<ITrigger>)>[]
    {
        CreateChannelRemover,
        CreateChannelRename,

        CreateSevenTVRoles,
        CreateSevenTVEmoteSet,
        CreateSevenTVEditors,
    });

    private readonly Serilog.ILogger Logger;
    private readonly IScheduler Scheduler;
    private readonly CancellationToken CToken;

    public BackgroundJobStarter(Serilog.ILogger logger, IScheduler scheduler, CancellationToken cancellationToken)
    {
        Logger = logger;
        Scheduler = scheduler;
        CToken = cancellationToken;
    }

    public async ValueTask RegisterJobs()
    {
        Logger.Information("Registering Quartz jobs");

        foreach (var jobFactory in _jobFactories)
        {
            var (job, triggers) = jobFactory();
            await Scheduler.ScheduleJob(job, triggers, replace: true, cancellationToken: CToken);
        }
    }

    private static (IJobDetail, List<ITrigger>) CreateChannelRemover()
    {
        var job = JobBuilder
            .Create<ChannelRemoverJob>()
            .WithIdentity(nameof(ChannelRemoverJob))
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity(nameof(ChannelRemoverJob))
            .StartNow()
            .WithSchedule(SimpleScheduleBuilder.RepeatMinutelyForever(30))
            .Build();

        return (job, new() { trigger });
    }

    private static (IJobDetail, List<ITrigger>) CreateChannelRename()
    {
        var job = JobBuilder
            .Create<ChannelRenameJob>()
            .WithIdentity(nameof(ChannelRenameJob))
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity(nameof(ChannelRenameJob))
            .WithSchedule(SimpleScheduleBuilder.RepeatMinutelyForever(30))
            .Build();

        return (job, new() { trigger });
    }

    private static (IJobDetail, List<ITrigger>) CreateSevenTVRoles()
    {
        var job = JobBuilder
            .Create<FetchRolesJob>()
            .WithIdentity(nameof(FetchRolesJob))
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity(nameof(FetchRolesJob))
            .StartNow()
            .WithSchedule(SimpleScheduleBuilder.RepeatHourlyForever())
            .Build();

        return (job, new() { trigger });
    }


    private static (IJobDetail, List<ITrigger>) CreateSevenTVEmoteSet()
    {
        var job = JobBuilder
            .Create<FetchEmoteSetsJob>()
            .WithIdentity(nameof(FetchEmoteSetsJob))
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity(nameof(FetchEmoteSetsJob))
            .WithSchedule(SimpleScheduleBuilder.RepeatMinutelyForever(2))
            .Build();

        return (job, new() { trigger });
    }

    private static (IJobDetail, List<ITrigger>) CreateSevenTVEditors()
    {
        var job = JobBuilder
            .Create<FetchChannelEditorsJob>()
            .WithIdentity(nameof(FetchChannelEditorsJob))
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity(nameof(FetchChannelEditorsJob))
            .WithSchedule(SimpleScheduleBuilder.RepeatMinutelyForever(5))
            .Build();

        return (job, new() { trigger });
    }
}
