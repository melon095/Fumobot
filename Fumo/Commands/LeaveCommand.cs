using Autofac;
using Fumo.Database;
using Fumo.Enums;
using Fumo.Models;
using Microsoft.EntityFrameworkCore;
using MiniTwitch.Irc;
using Serilog;
using System.Runtime.InteropServices;

namespace Fumo.Commands;

internal class LeaveCommand : ChatCommand
{
    public LeaveCommand()
    {
        SetName("leave|part");
        SetFlags(ChatCommandFlags.BroadcasterOnly);
    }

    public ILogger Logger { get; }

    public ILifetimeScope LifetimeScope { get; }

    public IApplication Application { get; }

    public IrcClient IrcClient { get; }

    public LeaveCommand(ILogger logger, ILifetimeScope lifetimeScope, IApplication application, IrcClient ircClient) : this()
    {
        Logger = logger.ForContext<LeaveCommand>();
        LifetimeScope = lifetimeScope;
        Application = application;
        IrcClient = ircClient;
    }


    public override async ValueTask<CommandResult> Execute(CancellationToken ct)
    {
        using var scope = LifetimeScope.BeginLifetimeScope();
        using var db = scope.Resolve<DatabaseContext>();
        try
        {
            using var transaction = await db.Database.BeginTransactionAsync(ct);

            Channel.SetForDeletion = true;
            db.Channels.Update(Channel);
            await db.SaveChangesAsync(ct);

            await this.IrcClient.PartChannel(Channel.TwitchName, ct);
            // Unsure if this is right
            this.Application.Channels[Channel.TwitchName].SetForDeletion = true;

            await transaction.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            this.Logger.Error(ex, "Failed to leave {Channel}", Channel.TwitchName);
            return "An error occured, try again later";
        }

        return "👍";
    }
}
