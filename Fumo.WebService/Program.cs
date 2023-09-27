using Autofac;
using Autofac.Extensions.DependencyInjection;
using Fumo.Shared.Extensions;
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

        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureContainer<ContainerBuilder>(x =>
            {
                x.InstallShared(configuration);
                x.RegisterType<DescriptionService>().SingleInstance();
            });

        builder.Host.UseSerilog();
        builder.Services.AddControllers();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseSerilogRequestLogging();

        app.UseDefaultFiles(new DefaultFilesOptions
        {
            DefaultFileNames = new[] { "index.html" }
        });


        app.UseStaticFiles();

        app.UseAuthorization();

        app.UseRouting();
        app.MapControllers();

        MapBasicRoutes(app);

        app.Run();
    }

    static void MapBasicRoutes(WebApplication app)
    {
        app.MapGet("/", (ctx) =>
        {
            ctx.Response.Redirect("/commands/");

            return Task.FromResult(0);
        });
    }
}