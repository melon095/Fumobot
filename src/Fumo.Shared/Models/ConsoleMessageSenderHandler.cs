using Fumo.Database.DTO;
using Fumo.Shared.Enums;
using Serilog;

namespace Fumo.Shared.Models;

public class ConsoleMessageSenderHandler : IMessageSenderHandler
{
    private readonly ILogger Logger;

    public ConsoleMessageSenderHandler(ILogger logger)
    {
        Logger = logger.ForContext<ConsoleMessageSenderHandler>();
    }

    public void ScheduleMessage(MessageSendData data)
    {
        Logger.Debug("{Method}\t{ChannelId}\t{Message}", nameof(ScheduleMessage), data.ChannelId, data.Message);
    }
    public void ScheduleMessage(MessageSendData data, ChannelDTO channel)
        => ScheduleMessage(data);

    public async void ScheduleMessageWithBanphraseCheck(MessageSendData data, ChannelDTO channel)
    {
        await CheckBanphrase(channel, data.Message, default);
        ScheduleMessage(data);
    }

    public ValueTask SendMessage(MessageSendData data)
    {
        Logger.Debug("{Method}\t{ChannelId}\t{Message}", nameof(SendMessage), data.ChannelId, data.Message);

        return ValueTask.CompletedTask;
    }

    public void Cleanup(string channelId) { }

    public ValueTask<BanphraseReason> CheckBanphrase(ChannelDTO channel, string message, CancellationToken cancellationToken = default)
    {
        Logger.Debug("{Method}\t{ChannelId}\t{Message}", nameof(CheckBanphrase), channel.TwitchID, message);

        return ValueTask.FromResult(BanphraseReason.None);
    }
}
