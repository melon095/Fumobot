using Fumo.Database.DTO;
using Fumo.Database;
using Fumo.Shared.Interfaces;
using Fumo.Shared.ThirdParty.ThreeLetterAPI.Instructions;
using Fumo.Shared.ThirdParty.ThreeLetterAPI.Response;
using Fumo.Shared.ThirdParty.ThreeLetterAPI;
using Serilog;
using Microsoft.EntityFrameworkCore;
using Fumo.Shared.Models;

namespace Fumo.Application.Startable;

internal class CreateBotMetadataStarter
{
    private readonly DatabaseContext DbContext;
    private readonly AppSettings Settings;
    private readonly IThreeLetterAPI ThreeLetterAPI;
    private readonly IChannelRepository ChannelRepository;
    private readonly CancellationToken Token;

    public CreateBotMetadataStarter(DatabaseContext db, AppSettings settings, IThreeLetterAPI threeLetterAPI, IChannelRepository channelRepository, CancellationToken token)
    {
        DbContext = db;
        Settings = settings;
        ThreeLetterAPI = threeLetterAPI;
        ChannelRepository = channelRepository;
        Token = token;
    }

    public async ValueTask Start()
    {
        Log.Information("Checking for Pending migrations");

        await DbContext.Database.MigrateAsync(Token);
        await ChannelRepository.Prepare(Token);

        var botID = Settings.Twitch.UserID;
        var botChannel = ChannelRepository.GetByID(botID);

        if (botChannel is null)
        {
            var response = await ThreeLetterAPI.Send<BasicUserResponse>(new BasicUserInstruction(id: botID), Token);

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

            await DbContext.Users.AddAsync(user, Token);
            await DbContext.SaveChangesAsync(Token);

            await ChannelRepository.Create(channel, Token);
        }
    }
}
