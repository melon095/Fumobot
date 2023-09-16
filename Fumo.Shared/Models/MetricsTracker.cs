using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Prometheus;
using Serilog;

namespace Fumo.Shared.Models;

public class MetricsTracker : IDisposable
{
    #region Fields

    private readonly ILogger Logger;
    private readonly IConfiguration Config;
    private readonly IDisposable unregisterChangeCallback;
    private readonly int port;

    private MetricServer MetricServer;
    private bool isEnabled = false;

    #endregion

    #region Metrics

    public readonly Counter CommandsExecuted = Metrics.CreateCounter("fumo_commands_executed", "Total number of commands executed", new CounterConfiguration
    {
        LabelNames = new[] { "channel", "command", "success" }
    });

    public readonly Counter TotalMessagesSent = Metrics.CreateCounter("fumo_messages_sent_total", "Total number of messages sent", new CounterConfiguration
    {
        LabelNames = new[] { "channel" }
    });

    public readonly Counter TotalMessagesRead = Metrics.CreateCounter("fumo_messages_read_total", "Total numberof messages read by Fumobot", new CounterConfiguration
    {
        LabelNames = new[] { "channel" }
    });

    public readonly Gauge ChannelsJoined = Metrics.CreateGauge("fumo_channels_joined", "Total number of channels joined");

    #endregion

    #region Constructor

    public MetricsTracker(IConfiguration config, ILogger logger)
    {
        this.Logger = logger.ForContext<MetricsTracker>();

        this.port = config.GetValue<int>("Metrics:Port");
        this.isEnabled = config.GetValue<bool>("Metrics:Enabled");

        var server = new MetricServer(port);

        this.MetricServer = server;
        this.Config = config;

        unregisterChangeCallback = ChangeToken.OnChange(
            () => Config.GetReloadToken(),
            OnConfigChange);
    }

    #endregion

    #region Methods

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        unregisterChangeCallback.Dispose();
        MetricServer.Stop();
    }

    private void OnConfigChange()
    {
        var newConfig = Config.GetValue<bool>("Metrics:Enabled");

        if (newConfig != isEnabled)
        {
            isEnabled = newConfig;
            if (isEnabled)
            {
                Logger.Information("Config changed and starting Metrics server at port {Port}", port);

                MetricServer.Dispose();
                MetricServer = new(port);

                MetricServer.Start();
            }
            else
            {
                Logger.Information("Config changed and disabling Metrics server");

                MetricServer.Stop();
            }
        }
    }

    public void Start()
    {
        if (isEnabled)
        {
            Logger.Information("Starting Metrics server at port {Port}", port);

            MetricServer.Start();
        }
        else
        {
            Logger.Information("Metrics server is disabled");
        }
    }

    #endregion
}
