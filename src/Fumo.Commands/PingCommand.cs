using System.Text;
using Fumo.Database;
using Fumo.Database.Extensions;
using Fumo.Shared.Enums;
using Fumo.Shared.Models;
using Fumo.Shared.Utils;

namespace Fumo.Commands;

public class PingCommand : ChatCommand
{
    private static DateTime Start;

    public PingCommand()
    {
        if (Start == DateTime.MinValue)
        {
            Start = DateTime.Now;
        }

        SetName("[Pp]ing");
        SetFlags(ChatCommandFlags.Reply);
        AddParameter(new(typeof(bool), "detailed"));
    }

    public override ValueTask<CommandResult> Execute(CancellationToken ct)
    {
        var detailed = GetArgument<bool>("detailed");
        var uptime = DateTime.Now - Start;

        StringBuilder builder = new();


        string time = new SecondsFormatter().SecondsFmt(uptime.TotalSeconds, limit: 4);

        builder.Append($"🕴️ Uptime: {time}");

        if (detailed)
        {
            builder.Append($" 🔗 EventSub?: ");

            if (Channel.GetSettingBool(ChannelSettingKey.ConnectedWithEventsub))
                builder.Append("Yes :)");
            else
                builder.Append("No :(");
        }

        return ValueTask.FromResult(new CommandResult
        {
            Message = builder.ToString(),
        });
    }
    public override ValueTask BuildHelp(ChatCommandHelpBuilder builder, CancellationToken ct)
        => builder
            .WithCache()
            .WithDisplayName("ping")
            .WithArgument("detailed", (x) => { })
            .Finish;
}
