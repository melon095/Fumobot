using Fumo.BackgroundJobs.SevenTV;
using Quartz;
using System.Collections.ObjectModel;

namespace Fumo.BackgroundJobs;

internal class JobRegister
{
    private static readonly ReadOnlyCollection<Func<(IJobDetail, List<ITrigger>)>> _jobFactories = new(new Func<(IJobDetail, List<ITrigger>)>[]
    {
        CreateChannelRemover,
        CreateChannelRename,

        CreateSevenTVRoles,
        CreateSevenTVEmoteSet,

    });

    public static async Task RegisterJobs(IScheduler scheduler, CancellationToken cancellationToken)
    {
        foreach (var jobFactory in _jobFactories)
        {
            var (job, triggers) = jobFactory();
            await scheduler.ScheduleJob(job, triggers, replace: true, cancellationToken: cancellationToken);
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
            .WithSchedule(SimpleScheduleBuilder.RepeatHourlyForever())
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

}
