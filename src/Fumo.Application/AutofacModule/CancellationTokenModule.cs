using System.Runtime.Loader;
using Autofac;

namespace Fumo.Application.AutofacModule;

internal class CancellationTokenModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        CancellationTokenSource tokenSource = new();

        void AllCleanup()
        {
            if (!tokenSource.IsCancellationRequested)
            {
                tokenSource.Cancel();
            }
        }

        Console.CancelKeyPress += (_, _) => AllCleanup();
        AssemblyLoadContext.Default.Unloading += (_) => AllCleanup();

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
