using Fumo.Enums;
using Fumo.Models;
using Fumo.Utils;
using Serilog;
using System.Collections.ObjectModel;

namespace Fumo.Commands;

internal class PingCommand : ChatCommand
{
    public ILogger Logger { get; }
    public Application Application { get; }

    public PingCommand()
    {
        SetName("[Pp]ing");
        SetFlags(ChatCommandFlags.Reply);
    }

    public PingCommand(ILogger logger, Application application) : this()
    {
        Logger = logger.ForContext<PingCommand>();
        Application = application;
    }

    public override ValueTask<CommandResult> Execute(CancellationToken ct)
    {
        // FIXME remove lol
        this.Logger.Debug("Ran Ping comamnd");

        var uptime = DateTime.Now - this.Application.StartTime;

        string time = new SecondsFormatter().SecondsFmt(uptime.TotalSeconds);
        return ValueTask.FromResult(new CommandResult
        {
            Message = $"🕴️ Uptime: {time}",
        });
    }
}
