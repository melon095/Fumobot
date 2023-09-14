namespace Fumo.Shared.Interfaces;

public interface IMessageSenderHandler
{
    /// <summary>
    /// Schedule a message to be sent to a channel after the global message interval rule
    /// </summary>
    void ScheduleMessage(string channel, string message, string? replyID = null);

    /// <summary>
    /// Will directly send a message to chat without obeying the message queue
    /// </summary>
    ValueTask SendMessage(string channel, string message, string? replyID = null);
}