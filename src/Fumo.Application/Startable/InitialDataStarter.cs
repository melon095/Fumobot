using Fumo.Application.Web.Service;
using Fumo.Database;
using Fumo.Shared.Eventsub;
using Fumo.Shared.Interfaces;
using Fumo.Shared.Repositories;
using Microsoft.EntityFrameworkCore;
using MiniTwitch.Helix.Models;

namespace Fumo.Application.Startable;

internal class InitialDataStarter(
        Serilog.ILogger Logger,
        DescriptionService DescriptionService,
        CommandRepository CommandRepository,
        DatabaseContext DbContext,
        IChannelRepository channelRepository,
        IEventsubManager eventsubManager,
        IEventsubCommandRegistry eventsubCommandRegistry) : IAsyncStartable
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


        var isHttps = eventsubManager.CallbackUrl.Scheme == "https";
        if (!isHttps)
            log.Warning("Can't subscribe to Conduit without https :(");
        else
        {
            log.Information("Checking for Conduit status");

            var conduit = await eventsubManager.GetConduitID(ct);
            if (conduit is null)
            {
                log.Information("Missing Conduit. Creating");
                await eventsubManager.CreateConduit(ct);
                log.Information("Conduit has been made :)");
            }
            else
            {
                log.Information("Conduit found. ID: {ConduitID}", conduit);
            }
        }

        eventsubCommandRegistry.RegisterAll();
    }
}
