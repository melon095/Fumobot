using Fumo.Database.DTO;
using Fumo.Database.Extensions;
using Fumo.Shared.Exceptions;
using Fumo.Shared.Models;
using Fumo.Shared.Interfaces;
using Fumo.Shared.Regexes;
using Fumo.ThirdParty.ThreeLetterAPI;
using Fumo.ThirdParty.ThreeLetterAPI.Instructions;
using Fumo.ThirdParty.ThreeLetterAPI.Response;
using Microsoft.Extensions.Configuration;
using MiniTwitch.Irc;
using Serilog;

namespace Fumo.Commands;

public class JoinCommand : ChatCommand
{
    private readonly ILogger Logger;
    private readonly IrcClient Irc;
    private readonly IChannelRepository ChannelRepository;
    private readonly IThreeLetterAPI ThreeLetterAPI;
    private readonly IUserRepository UserRepository;
    private readonly string BotID;

    public JoinCommand()
    {
        SetName("(re)?join");
        SetDescription("Allow the bot to join you or a channel you mod");
        SetCooldown(TimeSpan.FromMinutes(1));
    }

    public JoinCommand(
        ILogger logger,
        IConfiguration config,
        IrcClient irc,
        IChannelRepository channelRepository,
        IThreeLetterAPI threeLetterAPI,
        IUserRepository userRepository) : this()
    {
        Logger = logger.ForContext<JoinCommand>();
        Irc = irc;
        ChannelRepository = channelRepository;
        ThreeLetterAPI = threeLetterAPI;
        UserRepository = userRepository;
        BotID = config["Twitch:UserID"] ?? throw new ArgumentException("missing Twitch:UserID config");
    }

    private async Task<bool> IsMod(UserDTO user, string channelName, CancellationToken ct)
    {
        var list = await this.ThreeLetterAPI.PaginatedQuery<ChannelModsResponse>(resp =>
        {
            if (resp is null)
            {
                return new ChannelModsInstruction(login: channelName);
            }
            if (resp.User.Mods.PageInfo.HasNextPage)
            {
                var latestCursor = resp.User.Mods.Edges[resp.User.Mods.Edges.Count - 1].Cursor;

                return new ChannelModsInstruction(login: channelName, cursor: latestCursor);
            }

            return null;

        }, ct);

        // Sure it might be slow on a big channel but who cares
        var isMod = list.Any(x => x.User.Mods.Edges.Any(x => x.Node.Id == user.TwitchID));

        return isMod;
    }

    public override async ValueTask<CommandResult> Execute(CancellationToken ct)
    {
        if (Channel.TwitchID != this.BotID)
        {
            return string.Empty;
        }

        var other = false;
        var userToJoin = User;
        var otherUser = Input.ElementAtOrDefault(0) ?? "";
        var otherUsersUsername = UsernameCleanerRegex.CleanUsername(otherUser);


        if (!string.IsNullOrEmpty(otherUser) && User.TwitchName != otherUsersUsername)
        {
            var isMod = await this.IsMod(User, otherUsersUsername, ct) || (User.HasPermission("admin.*"));

            if (!isMod)
            {
                return "You're not a mod in that channel 🤨";
            }

            try
            {
                userToJoin = await this.UserRepository.SearchName(otherUsersUsername, ct);
                other = true;
            }
            catch (UserNotFoundException ex)
            {
                return ex.Message;
            }
            catch (Exception ex)
            {
                this.Logger.Error(ex, string.Empty);

                return "Something went wrong";
            }
        }


        if (ChannelRepository.GetByID(userToJoin.TwitchID) is ChannelDTO channel)
        {
            var possesivePronoun = other ? "their" : "your";

            if (channel.SetForDeletion)
            {
                return "That channel was recently removed, please wait an hour or so before trying to 'join' again";
            }

            await this.Irc.PartChannel(userToJoin.TwitchName, ct);
            await this.Irc.JoinChannel(userToJoin.TwitchName, ct);

            return $"I tried to rejoin {possesivePronoun} channel.";
        }

        var pronoun = other ? $"{userToJoin.TwitchName}'s" : "your";

        // TODO: Add website or smth here.
        var response = $"Joining {pronoun} channel :) ";
        var newChatReply = "FeelsDankMan 👋 Hi.";
        if (other)
        {
            newChatReply += $" I was added by {User.TwitchName}";
        }

        try
        {
            ChannelDTO newChannel = new()
            {
                TwitchID = userToJoin.TwitchID,
                TwitchName = userToJoin.TwitchName,
                UserTwitchID = userToJoin.TwitchID,
            };

            await ChannelRepository.Create(newChannel, ct);

            await this.Irc.JoinChannel(newChannel.TwitchName, ct);
            await this.Irc.SendMessage(newChannel.TwitchName, newChatReply, cancellationToken: ct);

            return response;
        }
        catch (Exception ex)
        {
            this.Logger.Error(ex, "Failed to join {Channel}", userToJoin.TwitchID);

            return "An error occured joining that channel";
        }
    }
}
