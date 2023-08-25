using Autofac;
using Microsoft.Extensions.Configuration;

namespace Fumo.Extensions.AutoFacInstallers;

internal static class AutoFacDatabaseInstaller
{
    public static ContainerBuilder InstallDatabase(this ContainerBuilder builder, IConfiguration config)
    {
        return builder;
    }
}
