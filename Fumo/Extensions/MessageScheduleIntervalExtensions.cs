using Fumo.Models;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace Fumo.Extensions;

public static class MessageScheduleIntervalExtensions
{
    public static int DefaultMessageInterval = 1250;

    public static string String(this MessageScheduleInterval messageScheduleInterval)
        => messageScheduleInterval switch
        {
            MessageScheduleInterval.Read => "Read",
            MessageScheduleInterval.Write => "Write",
            MessageScheduleInterval.VIP => "VIP",
            MessageScheduleInterval.Mod => "Moderator",
            MessageScheduleInterval.Bot => "Bot",
            _ => "Unknown",
        };

    public static int ToMessageCooldown(this MessageScheduleInterval messageScheduleInterval)
    {
        switch (messageScheduleInterval)
        {
            case MessageScheduleInterval.Read:
            case MessageScheduleInterval.Write:
                {
                    return DefaultMessageInterval;
                }

            case MessageScheduleInterval.VIP:
                {
                    return 250;
                }

            case MessageScheduleInterval.Mod:
            case MessageScheduleInterval.Bot:
                {
                    return 50;
                }

            default: return DefaultMessageInterval;
        }
    }
}
