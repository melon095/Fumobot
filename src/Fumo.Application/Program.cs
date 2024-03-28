using Autofac;
using Autofac.Extensions.DependencyInjection;
using Fumo.Application.AutofacModule;
using Fumo.Application.Startable;
using Fumo.Shared.Models;
using Quartz;
using Serilog;

var config = PrepareConfig(args);
var appsettings = config.Get<AppSettings>()
        ?? throw new InvalidOperationException($"Unable to bind {nameof(AppSettings)} from config");

var builder = WebApplication.CreateBuilder(new WebApplicationOptions()
{
    Args = args,
    ContentRootPath = Directory.GetCurrentDirectory(),
    EnvironmentName = GetEnvironment(),
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

        // TODO: Don't use AutoFac for this.
        x.RegisterType<ChainStarter>().As<IStartable>().SingleInstance();
    }));

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

var cts = app.Services.GetRequiredService<CancellationTokenSource>();

await app.StartAsync();
await OnShutdown();

string GetEnvironment()
{
    var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

    if (string.IsNullOrWhiteSpace(env))
    {
        return Environments.Development;
    }

    return env;
}

IConfigurationRoot PrepareConfig(string[] args)
{
    var configPath = args.Length > 0 ? args[0] : "config.json";

    return new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile(configPath, optional: false, reloadOnChange: true)
        .Build();
}

async ValueTask OnShutdown()
{
    await app.Services.GetRequiredService<IScheduler>().Shutdown(cts.Token);

    while (!cts.IsCancellationRequested)
    {
        // Idk, Console.ReadLine doesn't work as a systemctl service
        await Task.Delay(100);
    }
}
