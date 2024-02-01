using Autofac;
using Autofac.Extensions.DependencyInjection;
using Fumo.Shared.Extensions;
using Fumo.Shared.Repositories;
using Fumo.WebService.Service;
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

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions()
        {
            Args = args,
            ContentRootPath = cwd,
            EnvironmentName = configuration["Environment"] ?? Environments.Production,
            WebRootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot")
        });

        builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureContainer<ContainerBuilder>(x =>
            {
                x.InstallShared(configuration);
                x.RegisterType<DescriptionService>().SingleInstance();
            });

        builder.Host.UseSerilog();
        builder.Services.AddControllers();

        var app = builder.Build();

        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseDefaultFiles(new DefaultFilesOptions
            {
                DefaultFileNames = ["index.html"]
            });

            app.UseStaticFiles();
        }

        app.UseAuthorization();

        app.UseRouting();
        app.MapControllers();

        PrepareServices(app.Services);

        app.Run();
    }

    static void PrepareServices(IServiceProvider serviceProvider)
    {
        var CommandRepository = serviceProvider.GetRequiredService<CommandRepository>();

        if (CommandRepository.Commands is { Count: 0 })
        {
            CommandRepository.LoadAssemblyCommands();
        }
    }
}
