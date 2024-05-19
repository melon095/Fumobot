using Autofac;
using Autofac.Extensions.DependencyInjection;
using Fumo.Application.AutofacModule;
using Fumo.Application.Startable;
using Fumo.Application.Web;
using Fumo.Shared.Models;
using Serilog;

var configPath = args.FirstOrDefault("config.json");
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile(configPath, optional: false)
    .Build();

var appsettings = config.Get<AppSettings>()
        ?? throw new InvalidOperationException($"Unable to bind {nameof(AppSettings)} from config");

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

var builder = WebApplication.CreateBuilder(new WebApplicationOptions()
{
    Args = args,
    ContentRootPath = Directory.GetCurrentDirectory(),
    EnvironmentName = environment,
    WebRootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot")
});

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory(x =>
    {
        x.RegisterInstance(config).As<IConfiguration>();
        x.RegisterInstance(appsettings).SingleInstance();

        x.RegisterModule(new WebModule());
        x.RegisterModule(new CancellationTokenModule());
        x.RegisterModule(new LoggerModule(appsettings));
        x.RegisterModule(new SingletonModule(appsettings));
        x.RegisterModule(new ScopedModule(appsettings));
        x.RegisterModule(new QuartzModule(appsettings));
        x.RegisterModule(new StartableModule());
    }));

builder.Host.UseSerilog();
builder.Services.AddControllers();

builder
    .SetupDataProtection(appsettings)
    .SetupHTTPAuthentication(appsettings);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSerilogRequestLogging();
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

app.UseRouting()
    .UseAuthentication()
    .UseAuthorization();

app.MapControllers();

var token = app.Services.GetRequiredService<CancellationToken>();

await RunStartup(app, token);
await app.RunAsync(token);

static async Task RunStartup(WebApplication app, CancellationToken ct)
{
    // Starts everything in order.

    using var scope = app.Services.CreateScope();

    foreach (var startup in StartableModule.Order)
    {
        try
        {
            var service = scope.ServiceProvider.GetRequiredService(startup);

            if (service is IAsyncStartable startable)
            {
                app.Logger.LogInformation("Starting {ClassName}", startup.Name);

                await startable.Start(ct);
            }
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "Error starting {ClassName}", startup.Name);

            throw;
        }
    }
}
