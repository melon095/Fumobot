using Autofac;
using Autofac.Extras.Quartz;
using Microsoft.Extensions.Configuration;
using Quartz;
using System.Collections.Specialized;

namespace Fumo.Extensions.AutoFacInstallers;

internal static class AutoFacQuartzInstaller
{
    public static ContainerBuilder InstallQuartz(this ContainerBuilder builder, IConfiguration config)
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
            { "quartz.dataSource.psql.connectionString", config["Connections:Postgres"] },
            {"quartz.serializer.type", "binary" }
        };

        builder.RegisterModule(new QuartzAutofacFactoryModule
        {
            ConfigurationProvider = _ => schedulerConfig,
        });

        builder.RegisterModule(new QuartzAutofacJobsModule(typeof(Program).Assembly));



        return builder;
    }
}
