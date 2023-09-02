using Fumo.Enums;
using Fumo.Models;
using Fumo.Utils;
using Serilog;

namespace Fumo.Commands;

internal class PingCommand : ChatCommand
{
    public ILogger Logger { get; }
    public IApplication Application { get; }

    public PingCommand()
    {
        SetName("[Pp]ing");
        SetFlags(ChatCommandFlags.Reply);
    }

    public PingCommand(ILogger logger, IApplication application) : this()
    {
        Logger = logger.ForContext<PingCommand>();
        Application = application;
    }

    public override ValueTask<CommandResult> Execute(CancellationToken ct)
    {
        var uptime = DateTime.Now - this.Application.StartTime;

        string time = new SecondsFormatter().SecondsFmt(uptime.TotalSeconds);
        return ValueTask.FromResult(new CommandResult
        {
            Message = $"🕴️ Uptime: {time}",
        });
    }
}
