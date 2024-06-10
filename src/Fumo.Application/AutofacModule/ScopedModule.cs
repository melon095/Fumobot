using Autofac;
using Fumo.Database;
using Fumo.Shared.Eventsub;
using Fumo.Shared.Interfaces;
using Fumo.Shared.Models;
using Fumo.Shared.OAuth;
using Fumo.Shared.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Serilog;

namespace Fumo.Application.AutofacModule;

internal class ScopedModule(AppSettings settings) : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        var dsb = new NpgsqlDataSourceBuilder(settings.Connections.Postgres);
        dsb.EnableDynamicJson();

        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseNpgsql(dsb.Build())
            .Options;

        builder.Register(x =>
        {
            return new DatabaseContext(options);
        }).AsSelf().InstancePerLifetimeScope();

        builder
            .RegisterType<UserRepository>()
            .As<IUserRepository>()
            .InstancePerLifetimeScope();

        builder
            .RegisterType<OAuthRepository>()
            .As<IOAuthRepository>()
            .InstancePerLifetimeScope();

        builder
            .RegisterType<EventsubCommandFactory>()
            .As<IEventsubCommandFactory>()
            .InstancePerLifetimeScope();
    }
}
