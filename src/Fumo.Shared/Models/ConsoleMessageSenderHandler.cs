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

    public void ScheduleMessage(MessageSendSpec spec)
    {
        Logger.Debug("{Method}\t{ChannelId}\t{Message}", nameof(ScheduleMessage), spec.ChannelId, spec.Message);
    }

    public async ValueTask ScheduleMessageWithBanphraseCheck(MessageSendSpec spec, ChannelDTO channel, CancellationToken cancellationToken = default)
    {
        await CheckBanphrase(channel, spec.Message, cancellationToken);
        ScheduleMessage(spec);
    }

    public ValueTask SendMessage(MessageSendSpec spec)
    {
        Logger.Debug("{Method}\t{ChannelId}\t{Message}", nameof(SendMessage), spec.ChannelId, spec.Message);

        return ValueTask.CompletedTask;
    }

    public void Cleanup(string channelId) { }

    public ValueTask<BanphraseReason> CheckBanphrase(ChannelDTO channel, string message, CancellationToken cancellationToken = default)
    {
        Logger.Debug("{Method}\t{ChannelId}\t{Message}", nameof(CheckBanphrase), channel.TwitchID, message);

        return ValueTask.FromResult(BanphraseReason.None);
    }
}
