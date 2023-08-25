using Autofac;
using Microsoft.Extensions.Configuration;

namespace Fumo.Extensions.AutoFacInstallers;

internal static class AutoFacConfigInstaller
{
    public static ContainerBuilder InstallConfig(this ContainerBuilder builder, IConfiguration config)
    {
        builder.RegisterInstance(config).SingleInstance();

        return builder;
    }
}
