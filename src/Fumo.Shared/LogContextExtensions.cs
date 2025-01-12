using Serilog;
using Serilog.Context;
using Serilog.Core;
using Serilog.Core.Enrichers;

namespace Fumo.Shared;

public static class LogContextExtensions
{
    public static IDisposable Push(this IDisposable context, ILogEventEnricher[] enrichers)
    {
        foreach (var enricher in enrichers)
        {
            context = LogContext.Push(enricher);
        }
        return context;
    }

    public static IDisposable PushProperties(this ILogger logger, params (string Key, object? Value)[] properties)
    {
        var enrichers = new ILogEventEnricher[properties.Length];
        for (int i = 0; i < properties.Length; i++)
        {
            enrichers[i] = new PropertyEnricher(properties[i].Key, properties[i].Value);
        }
        return LogContext.Push(enrichers);
    }

    public static IDisposable PushProperties<TValue>(this ILogger logger, params (string Key, TValue Value)[] properties)
    {
        var enrichers = new ILogEventEnricher[properties.Length];
        for (int i = 0; i < properties.Length; i++)
        {
            enrichers[i] = new PropertyEnricher(properties[i].Key, properties[i].Value);
        }
        return LogContext.Push(enrichers);
    }
}
