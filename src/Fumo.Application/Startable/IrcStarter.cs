using Fumo.Application.Bot;
using Fumo.Shared.Interfaces;
using Fumo.Shared.Models;

namespace Fumo.Application.Startable;

internal class IrcStarter : IAsyncStartable
{
    private readonly MetricsTracker MetricsTracker;
    private readonly IrcHandler IrcHandler;

    public IrcStarter(IMessageSenderHandler messageSenderHandler,
                      MetricsTracker metricsTracker, IrcHandler ircHandler)
    {
        // TODO: Can we use AutoActivate here?
        _ = messageSenderHandler;
        MetricsTracker = metricsTracker;
        IrcHandler = ircHandler;
    }

    public async ValueTask Start(CancellationToken ct)
    {
        MetricsTracker.Start();
        await IrcHandler.Start();
    }
}
