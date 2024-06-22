using Fumo.Database.DTO;
using Fumo.Shared.Enums;

namespace Fumo.Shared.Interfaces;

public interface IMessageSenderHandler
{
    /// <summary>
    /// Schedule a message to be sent to a channel after the global message interval rule
    /// </summary>
    void ScheduleMessage(string channelId, string message, string? replyId = null);

    /// <summary>
    /// Will directly send a message to chat without obeying the message queue
    /// </summary>
    ValueTask SendMessage(string channelId, string message, string? replyId = null);

    void Cleanup(string channelId);

    ValueTask<BanphraseReason> CheckBanphrase(ChannelDTO channel, string message, CancellationToken cancellationToken = default);
}
