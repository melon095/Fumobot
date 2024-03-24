namespace Fumo.Shared.Models;

public struct ScheduleMessageSpecification(string channel, string message)
{
    public string Channel { get; set; } = channel;
    public string Message { get; set; } = message;
    public string? ReplyID { get; set; } = null;
    public bool IgnoreBanphrase { get; set; } = false;
}
