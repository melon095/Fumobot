namespace Fumo.Interfaces;

public interface IMessageSenderHandler
{
    void ScheduleMessage(string channel, string message, string? replyID = null);
}