using Fumo.Application.Web.Service;
using Fumo.Database;
using Fumo.Shared.Eventsub;
using Fumo.Shared.Models;
using Fumo.Shared.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Fumo.Application.Startable;

internal class InitialDataStarter(
    Serilog.ILogger logger,
    DescriptionService descriptionService,
    DatabaseContext dbContext,
    IChannelRepository channelRepository,
    IEventsubManager eventsubManager,
    IEventsubCommandRegistry eventsubCommandRegistry,
    IEnumerable<ChatCommand> commands)
        : IAsyncStartable
{
    public async ValueTask Start(CancellationToken ct)
    {
        var log = logger.ForContext<InitialDataStarter>();

        log.Information("Checking for Pending migrations");

        var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync(ct)).ToList();
        if (pendingMigrations.Count > 0)
        {
            foreach (var migration in pendingMigrations)
                log.Information("Applying migration {Migration}", migration);

            await dbContext.Database.MigrateAsync(ct);
        }
        else
            log.Information("No pending migrations found");

        await descriptionService.Prepare(ct);
        await channelRepository.Prepare(ct);

        log.Debug("Commands loaded {Commands}", commands.Select(x => x.NameMatcher).ToArray());

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
