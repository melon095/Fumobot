using System.Runtime.Loader;
using Autofac;

namespace Fumo.Application.AutofacModule;

internal class CancellationTokenModule : Module
{
    protected override void Load(ContainerBuilder builder)
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

        // register CancellaationToken
        builder
            .Register(c => c.Resolve<CancellationTokenSource>().Token)
            .AsSelf()
            .AsImplementedInterfaces()
            .SingleInstance();

    }
}
