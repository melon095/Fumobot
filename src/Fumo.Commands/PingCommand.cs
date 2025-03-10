﻿using System.Text;
using Fumo.Database;
using Fumo.Database.Extensions;
using Fumo.Shared.Enums;
using Fumo.Shared.Models;
using Fumo.Shared.Utils;

namespace Fumo.Commands;

public class PingCommand : ChatCommand
{
    protected override List<Parameter> Parameters =>
    [
        MakeParameter<bool>("detailed")
    ];

    public override ChatCommandMetadata Metadata => new()
    {
        Name = "[Pp]ing",
        Flags = ChatCommandFlags.Reply,
    };

    private static DateTime Start;

    public override void OnInit()
    {
        Start = DateTime.Now;
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
