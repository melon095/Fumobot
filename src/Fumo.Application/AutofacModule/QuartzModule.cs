using System.Collections.Specialized;
using System.Reflection;
using Autofac;
using Autofac.Extras.Quartz;
using Fumo.BackgroundJobs;
using Fumo.Shared.Models;

namespace Fumo.Application.AutofacModule;

internal class QuartzModule(AppSettings settings) : Autofac.Module
{
    private static readonly Assembly JobAssembly = typeof(ChannelRenameJob).Assembly;

    protected override void Load(ContainerBuilder builder)
    {
        var schedulerConfig = new NameValueCollection
        {
            { "quartz.threadPool.threadCount", "3" },
            { "quartz.scheduler.threadName", "Quartz Scheduler" },
            { "quartz.jobStore.type", "Quartz.Impl.AdoJobStore.JobStoreTX, Quartz" },
            { "quartz.jobStore.driverDelegateType", "Quartz.Impl.AdoJobStore.PostgreSQLDelegate, Quartz" },
            { "quartz.jobStore.dataSource", "psql" },
            { "quartz.jobStore.tablePrefix", "quartz.qrtz_" },
            { "quartz.dataSource.psql.provider", "Npgsql" },
            { "quartz.dataSource.psql.connectionString", settings.Connections.Postgres },
            { "quartz.serializer.type", "binary" }
        };

        builder.RegisterModule(new QuartzAutofacFactoryModule
        {
            ConfigurationProvider = _ => schedulerConfig,
        });

        builder.RegisterModule(new QuartzAutofacJobsModule(JobAssembly));
    }
}
