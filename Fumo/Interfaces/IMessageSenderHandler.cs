namespace Fumo.Interfaces;

public interface IMessageSenderHandler
{
    void Init();

    void ScheduleMessage(string channel, string message, string? replyID = null);
}