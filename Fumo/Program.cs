using Autofac;
using Fumo.Database;
using Fumo.Database.DTO;
using Fumo.Extensions.AutoFacInstallers;
using Fumo.Handlers;
using Fumo.Interfaces;
using Fumo.Models;
using Fumo.ThirdParty.ThreeLetterAPI;
using Fumo.ThirdParty.ThreeLetterAPI.Instructions;
using Fumo.ThirdParty.ThreeLetterAPI.Response;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Fumo;

internal class Program
{
    static async Task Main(string[] args)
    {
        var cwd = Directory.GetCurrentDirectory();
        var configPath = args.Length > 0 ? args[0] : "config.json";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(cwd)
            .AddJsonFile(configPath, optional: false, reloadOnChange: true)
            .Build();

        var container = new ContainerBuilder()
            .InstallGlobalCancellationToken(configuration)
            .InstallConfig(configuration)
            .InstallSerilog(configuration)
            .InstallDatabase(configuration)
            .InstallSingletons(configuration)
            .InstallScoped(configuration)
            .Build();

        Log.Logger.Information("Starting up");

        using (var scope = container.BeginLifetimeScope())
        {
            var commandRepo = scope.Resolve<CommandRepository>();
            commandRepo.LoadAssemblyCommands();

            // The simplest way of handling the bot's channel/user is just initializing it here.
            var config = scope.Resolve<IConfiguration>();
            var tlp = scope.Resolve<IThreeLetterAPI>();
            var db = scope.Resolve<DatabaseContext>();
            var ctoken = scope.Resolve<CancellationTokenSource>().Token;

            var botChannel = await db.Channels
                .Where(x => x.UserTwitchID.Equals(config["Twitch:UserID"]))
                .SingleOrDefaultAsync();

            if (botChannel is null)
            {
                var response = await tlp.SendAsync<BasicUserResponse>(new BasicUserInstruction(), new { id = config["Twitch:UserID"] }, ctoken);

                UserDTO user = new()
                {
                    TwitchID = response.User.ID,
                    TwitchName = response.User.Login,
                    Permissions = new() { "default", "bot" }
                };

                ChannelDTO channel = new()
                {
                    TwitchID = response.User.ID,
                    TwitchName = response.User.Login,
                    UserTwitchID = response.User.ID,
                };

                // add to database
                db.Channels.Add(channel);
                db.Users.Add(user);

                await db.SaveChangesAsync();
            }


            Log.Information("Checking for Pending migrations");
            await db.Database.MigrateAsync(ctoken);

            // Start up some singletons
            _ = scope.Resolve<ICommandHandler>();
            _ = scope.Resolve<ICooldownHandler>();
            _ = scope.Resolve<IMessageSenderHandler>();

            await scope.Resolve<Application>().StartAsync();
        }




        Console.ReadLine();
    }
}
