using Fumo.Application.Web.Service;
using Fumo.Database;
using Fumo.Shared.Interfaces;
using Fumo.Shared.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Fumo.Application.Startable;

internal class InitialDataStarter(
        Serilog.ILogger Logger,
        DescriptionService DescriptionService,
        CommandRepository CommandRepository,
        DatabaseContext DbContext,
        IChannelRepository channelRepository) : IAsyncStartable
{
    public async ValueTask Start(CancellationToken ct)
    {
        var log = Logger.ForContext<InitialDataStarter>();

        log.Information("Checking for Pending migrations");

        var pendingMigrations = (await DbContext.Database.GetPendingMigrationsAsync(ct)).ToList();
        if (pendingMigrations.Count > 0)
        {
            foreach (var migration in pendingMigrations)
                log.Information("Applying migration {Migration}", migration);

            await DbContext.Database.MigrateAsync(ct);
        }
        else
            log.Information("No pending migrations found");

        CommandRepository.LoadAssemblyCommands();
        await DescriptionService.Prepare(ct);
        await channelRepository.Prepare(ct);
    }
}
