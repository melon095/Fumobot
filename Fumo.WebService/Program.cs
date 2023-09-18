using Autofac;
using Autofac.Extensions.DependencyInjection;
using Fumo.Shared.Extensions;
using Fumo.WebService.Mapper;
using Serilog;

namespace Fumo.WebService;

public class Program
{
    public static void Main(string[] args)
    {
        var cwd = Directory.GetCurrentDirectory();
        var configPath = args.Length > 0 ? args[0] : "config.json";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(cwd)
            .AddJsonFile(configPath, optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureContainer<ContainerBuilder>(x =>
            {
                x.InstallShared(configuration);
                x.RegisterType<CommandMapper>();
            });

        builder.Host.UseSerilog();
        builder.Services.AddControllers();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseSerilogRequestLogging();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}