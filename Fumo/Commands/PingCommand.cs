using Fumo.Enums;
using Fumo.Models;
using Fumo.Utils;

namespace Fumo.Commands;

internal class PingCommand : ChatCommand
{
    private readonly IApplication Application;

    public PingCommand()
    {
        SetName("[Pp]ing");
        SetFlags(ChatCommandFlags.Reply);
    }

    public PingCommand(IApplication application) : this()
    {
        Application = application;
    }

    public override ValueTask<CommandResult> Execute(CancellationToken ct)
    {
        var uptime = DateTime.Now - Application.StartTime;

        string time = new SecondsFormatter().SecondsFmt(uptime.TotalSeconds);
        return ValueTask.FromResult(new CommandResult
        {
            Message = $"🕴️ Uptime: {time}",
        });
    }
}
