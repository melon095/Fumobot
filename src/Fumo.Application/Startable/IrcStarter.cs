using Fumo.Application.Bot;
using Fumo.Shared.Interfaces;
using Fumo.Shared.Models;

namespace Fumo.Application.Startable;

internal class IrcStarter
{
    private readonly MetricsTracker MetricsTracker;
    private readonly BotApplication BotApplication;

    public IrcStarter(ICommandHandler commandHandler, ICooldownHandler cooldownHandler, IMessageSenderHandler messageSenderHandler, MetricsTracker metricsTracker, BotApplication botApplication)
    {
        // TODO: Can we use AutoActivate here?
        _ = commandHandler;
        _ = cooldownHandler;
        _ = messageSenderHandler;
        MetricsTracker = metricsTracker;
        BotApplication = botApplication;
    }

    public async ValueTask Start()
    {
        MetricsTracker.Start();
        await BotApplication.Start();
    }
}
