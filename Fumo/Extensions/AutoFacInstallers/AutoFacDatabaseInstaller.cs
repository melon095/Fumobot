using Autofac;
using Fumo.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Fumo.Extensions.AutoFacInstallers;

internal static class AutoFacDatabaseInstaller
{
    public static ContainerBuilder InstallDatabase(this ContainerBuilder builder, IConfiguration config)
    {
        var connectionString = config["Connections:Postgres"];

        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseNpgsql(connectionString)
            .Options;

        builder.Register(x =>
        {
            return new DatabaseContext(options);
        }).AsSelf().InstancePerLifetimeScope();

        return builder;
    }
}
