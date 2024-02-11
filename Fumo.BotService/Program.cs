using Autofac;
using Fumo.BackgroundJobs;
using Fumo.Database;
using Fumo.Database.DTO;
using Fumo.Extensions.AutoFacInstallers;
using Fumo.Shared.Interfaces;
using Fumo.Shared.Models;
using Fumo.Shared.Repositories;
using Fumo.Shared.Extensions;
using Fumo.Shared.ThirdParty.ThreeLetterAPI;
using Fumo.Shared.ThirdParty.ThreeLetterAPI.Instructions;
using Fumo.Shared.ThirdParty.ThreeLetterAPI.Response;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Quartz;
using Serilog;

namespace Fumo;

internal class Program
{
    static async Task Main(string[] args)
    {
        var config = SetupInstaller.PrepareConfig(args);

        var container = new ContainerBuilder()
            .InstallGlobalCancellationToken()
            .InstallAppSettings(config, out var settings)
            .InstallShared(settings)
            .InstallScoped()
            .InstallSingletons(settings)
            .InstallQuartz(settings)
            .Build();

        Log.Information("Starting up");

        using (var scope = container.BeginLifetimeScope())
        {
            var commandRepo = scope.Resolve<CommandRepository>();
            commandRepo.LoadAssemblyCommands();

            // The simplest way of handling the bot's channel/user is just initializing it here.
            var tlp = scope.Resolve<IThreeLetterAPI>();
            var db = scope.Resolve<DatabaseContext>();
            var ctoken = scope.Resolve<CancellationTokenSource>().Token;
            var channelRepo = scope.Resolve<IChannelRepository>();

            Log.Information("Checking for Pending migrations");
            await db.Database.MigrateAsync(ctoken);

            var scheduler = scope.Resolve<IScheduler>();

            var botID = settings.Twitch.UserID;
            var botChannel = channelRepo.GetByID(botID);

            if (botChannel is null)
            {
                var response = await tlp.Send<BasicUserResponse>(new BasicUserInstruction(id: botID), ctoken);

                UserDTO user = new()
                {
                    TwitchID = response.User.ID,
                    TwitchName = response.User.Login,
                    Permissions = ["default", "bot"]
                };

                ChannelDTO channel = new()
                {
                    TwitchID = response.User.ID,
                    TwitchName = response.User.Login,
                    UserTwitchID = response.User.ID,
                };

                // add to database
                await channelRepo.Create(channel, ctoken);
                db.Users.Add(user);

                await db.SaveChangesAsync(ctoken);
            }

            Log.Information("Registering Quartz jobs");
            await JobRegister.RegisterJobs(scheduler, ctoken);

            // Start up some singletons
            _ = scope.Resolve<ICommandHandler>();
            _ = scope.Resolve<ICooldownHandler>();
            _ = scope.Resolve<IMessageSenderHandler>();
            scope.Resolve<MetricsTracker>().Start();

            await scheduler.Start(ctoken);

            await scope.Resolve<Application>().Start();
        }

        var token = container.Resolve<CancellationTokenSource>();

        while (!token.IsCancellationRequested)
        {
            // Idk, Console.ReadLine doesn't work as a systemctl service
            await Task.Delay(100);
        }

        await container.Resolve<IScheduler>().Shutdown(token.Token);
        await container.DisposeAsync();
    }
}
