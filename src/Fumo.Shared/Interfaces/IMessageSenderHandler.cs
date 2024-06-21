using Fumo.Shared.Models;

namespace Fumo.Shared.Interfaces;

public interface IMessageSenderHandler
{
    /// <summary>
    /// Schedule a message to be sent to a channel after the global message interval rule
    /// </summary>
    void ScheduleMessage(ScheduleMessageSpecification spec);
    void ScheduleMessage(string channel, string message);

    /// <summary>
    /// Will directly send a message to chat without obeying the message queue
    /// </summary>
    ValueTask SendMessage(ScheduleMessageSpecification spec);
    ValueTask SendMessage(string channel, string message);

    void Cleanup(string channel);
}