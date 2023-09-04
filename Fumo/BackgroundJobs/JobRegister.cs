using Quartz;
using System.Collections.ObjectModel;

namespace Fumo.BackgroundJobs;

internal class JobRegister
{
    private static readonly ReadOnlyCollection<Func<(IJobDetail, List<ITrigger>)>> _jobFactories = new(new Func<(IJobDetail, List<ITrigger>)>[]
    {
        CreateChannelRemover,
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
}
