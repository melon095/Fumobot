using Fumo.Database.DTO;
using Fumo.Database;
using Fumo.Shared.ThirdParty.ThreeLetterAPI.Instructions;
using Fumo.Shared.ThirdParty.ThreeLetterAPI.Response;
using Fumo.Shared.ThirdParty.ThreeLetterAPI;
using Fumo.Shared.Models;
using Fumo.Shared.Repositories;

namespace Fumo.Application.Startable;

internal class CreateBotMetadataStarter : IAsyncStartable
{
    private readonly DatabaseContext DbContext;
    private readonly AppSettings Settings;
    private readonly IThreeLetterAPI ThreeLetterAPI;
    private readonly IChannelRepository ChannelRepository;

    public CreateBotMetadataStarter(DatabaseContext db, AppSettings settings, IThreeLetterAPI threeLetterAPI, IChannelRepository channelRepository)
    {
        DbContext = db;
        Settings = settings;
        ThreeLetterAPI = threeLetterAPI;
        ChannelRepository = channelRepository;
    }

    public async ValueTask Start(CancellationToken ct)
    {
        var botID = Settings.Twitch.UserID;
        var botChannel = ChannelRepository.GetByID(botID);

        if (botChannel is null)
        {
            var response = await ThreeLetterAPI.Send<BasicUserResponse>(new BasicUserInstruction(id: botID), ct);

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

            await DbContext.Users.AddAsync(user, ct);
            await DbContext.SaveChangesAsync(ct);

            await ChannelRepository.Create(channel, ct);
        }
    }
}
