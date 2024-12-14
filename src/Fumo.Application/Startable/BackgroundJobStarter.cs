using Autofac.Extras.Quartz;
using Fumo.BackgroundJobs;
using Fumo.BackgroundJobs.SevenTV;
using Quartz;

namespace Fumo.Application.Startable;

internal class BackgroundJobStarter : IAsyncStartable
{
    private static readonly IReadOnlyList<JobData> Jobs =
    [
        CreateJob<ChannelRemoverJob>(EveryThirthyMinute),
        CreateJob<ChannelRenameJob>(EveryThirthyMinute),
        CreateJob<FetchRolesJob>(EveryHour),
        CreateJob<FetchEmoteSetsJob>(EveryMinute(2)),
        CreateJob<FetchChannelEditorsJob>(EveryMinute(5)),
    ];

    private readonly Serilog.ILogger Logger;
    private readonly IScheduler Scheduler;

    public BackgroundJobStarter(Serilog.ILogger logger, IScheduler scheduler)
    {
        Logger = logger.ForContext<BackgroundJobStarter>();
        Scheduler = scheduler;
    }

    public async ValueTask Start(CancellationToken ct)
    {
        Logger.Information("Registering Quartz jobs");

        foreach (var job in Jobs)
        {
            await Scheduler.ScheduleJob(job.JobDetail, job.Triggers, replace: true, cancellationToken: ct);

            Logger.Information("Scheduled job {JobName}", job.JobDetail.Key.Name);
        }

        await Scheduler.Start(ct);
    }

    private static JobData CreateJob<TJob>(IScheduleBuilder schedule) where TJob : IJob
    {
        var className = typeof(TJob).Name;

        var job = JobBuilder
            .Create<TJob>()
            .WithIdentity(className)
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity(className)
            .WithSchedule(schedule)
            .Build();

        return new(job, [trigger]);
    }

    private static SimpleScheduleBuilder EveryMinute(int minutes) => SimpleScheduleBuilder.RepeatMinutelyForever(minutes);
    private static SimpleScheduleBuilder EveryHour => SimpleScheduleBuilder.RepeatHourlyForever();
    private static SimpleScheduleBuilder EveryThirthyMinute => SimpleScheduleBuilder.RepeatMinutelyForever(30);

    record struct JobData(IJobDetail JobDetail, IReadOnlyList<ITrigger> Triggers);
}
