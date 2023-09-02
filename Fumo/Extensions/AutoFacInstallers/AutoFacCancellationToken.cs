
using Autofac;
using Microsoft.Extensions.Configuration;
using System.Runtime.Loader;

namespace Fumo.Extensions.AutoFacInstallers;

public static class AutoFacCancellationToken
{
    public static ContainerBuilder InstallGlobalCancellationToken(this ContainerBuilder builder, IConfiguration _)
    {
        CancellationTokenSource tokenSource = new();

        var AllCleanup = () =>
        {
            tokenSource.Cancel();
        };

        Console.CancelKeyPress += (sender, e) =>
        {
            AllCleanup();
        };

        AssemblyLoadContext.Default.Unloading += (ctx) =>
        {
            AllCleanup.Invoke();
        };

        builder
            .RegisterInstance(tokenSource)
            .AsSelf()
            .AsImplementedInterfaces()
            .SingleInstance();

        return builder;
    }
}
