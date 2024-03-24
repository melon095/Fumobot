using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Prometheus;
using Serilog;

namespace Fumo.Shared.Models;

public class MetricsTracker : IDisposable
{
    #region Fields

    private readonly ILogger Logger;
    private MetricServer MetricServer;
    private MetricsSettings Settings;

    #endregion

    #region Metrics

    public readonly Counter TotalMessagesSent = Metrics.CreateCounter("fumo_messages_sent_total", "Total number of messages sent");

    public readonly Counter TotalMessagesRead = Metrics.CreateCounter("fumo_messages_read_total", "Total numberof messages read by Fumobot", new CounterConfiguration
    {
        LabelNames = ["channel"]
    });

    #endregion

    #region Constructor

    public MetricsTracker(AppSettings settings, ILogger logger)
    {
        Settings = settings.Metrics;
        Logger = logger.ForContext<MetricsTracker>();

        var server = new MetricServer(Settings.Port);
        MetricServer = server;
    }

    #endregion

    #region Methods

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        MetricServer.Stop();
    }

    public void Start()
    {
        if (Settings.Enabled)
        {
            Logger.Information("Starting Metrics server at port {Port}", Settings.Port);

            MetricServer.Start();
        }
        else
        {
            Logger.Information("Metrics server is disabled");
        }
    }

    #endregion
}
