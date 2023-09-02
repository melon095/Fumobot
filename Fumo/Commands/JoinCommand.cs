using Autofac;
using Fumo.Database;
using Fumo.Database.DTO;
using Fumo.Exceptions;
using Fumo.Interfaces;
using Fumo.Models;
using Fumo.Shared.Regexes;
using Fumo.Shared.Repositories;
using Fumo.ThirdParty.ThreeLetterAPI;
using Fumo.ThirdParty.ThreeLetterAPI.Instructions;
using Fumo.ThirdParty.ThreeLetterAPI.Response;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniTwitch.Irc;
using Serilog;

namespace Fumo.Commands;

internal class JoinCommand : ChatCommand
{
    public ILogger Logger { get; }

    public DatabaseContext Database { get; }

    public IrcClient Irc { get; }
    public Application Application { get; }
    public IThreeLetterAPI ThreeLetterAPI { get; }
    public IUserRepository UserRepository { get; }
    public ILifetimeScope LifetimeScope { get; }

    private readonly string BotID;

    public JoinCommand()
    {
        SetName("join");
        SetDescription("Allow the bot to join you or a channel you mod");
        SetCooldown(TimeSpan.FromMinutes(1));
    }

    public JoinCommand(
        ILogger logger,
        DatabaseContext database,
        IConfiguration config,
        IrcClient irc,
        Application application,
        IThreeLetterAPI threeLetterAPI,
        IUserRepository userRepository,
        ILifetimeScope lifetimeScope) : this()
    {
        Logger = logger.ForContext<JoinCommand>();
        Database = database;
        Irc = irc;
        Application = application;
        ThreeLetterAPI = threeLetterAPI;
        UserRepository = userRepository;
        LifetimeScope = lifetimeScope;
        BotID = config["Twitch:UserID"] ?? throw new ArgumentException("missing Twitch:UserID config");
    }

    private async Task<bool> IsMod(UserDTO user, string channelName, CancellationToken ct)
    {
        var list = await this.ThreeLetterAPI.PaginatedQueryAsync<ChannelModsResponse>(resp =>
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
        var otherUser = Input.ElementAtOrDefault(0);

        if (otherUser is not null)
        {
            var username = UsernameCleanerRegex.CleanUsername(otherUser);

            var isMod = await this.IsMod(User, username, ct);

            if (!isMod)
            {
                return "You're not a mod in that channel 🤨";
            }

            try
            {
                userToJoin = await this.UserRepository.SearchNameAsync(username, ct);
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

        if (await this.Database.Channels.AnyAsync(x => x.TwitchID == userToJoin.TwitchID, ct))
        {
            var p = other ? "their" : "your";

            return $"I am already in {p} channel.";
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
            using var scope = this.LifetimeScope.BeginLifetimeScope();
            using var context = scope.Resolve<DatabaseContext>();
            using (var transaction = await context.Database.BeginTransactionAsync(ct))
            {
                ChannelDTO newChannel = new()
                {
                    TwitchID = userToJoin.TwitchID,
                    TwitchName = userToJoin.TwitchName,
                    UserTwitchID = userToJoin.TwitchID,
                };

                await this.Irc.JoinChannel(newChannel.TwitchName, ct);
                await this.Irc.SendMessage(newChannel.TwitchName, newChatReply, cancellationToken: ct);

                await this.Database.Channels.AddAsync(newChannel, ct);
                await this.Database.SaveChangesAsync(ct);

                await transaction.CommitAsync(ct);
            }

            // Yes this is required i think
            ChannelDTO channel = await this.Database.Channels.Where(x => x.TwitchID == userToJoin.TwitchID).FirstAsync(ct);

            this.Application.AddChannel(channel);

            return response;
        }
        catch (Exception ex)
        {
            this.Logger.Error(ex, "Failed to join {Channel}", userToJoin.TwitchID);

            return "An error occured joining that channel";
        }
    }
}
