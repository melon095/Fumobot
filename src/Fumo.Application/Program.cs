using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using AspNet.Security.OAuth.Twitch;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Fumo.Application.AutofacModule;
using Fumo.Application.Startable;
using Fumo.Database.DTO;
using Fumo.Shared.Interfaces;
using Fumo.Shared.Models;
using Fumo.Shared.OAuth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Serilog;
using StackExchange.Redis;

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

builder.Services.AddDataProtection()
    .PersistKeysToStackExchangeRedis(ConnectionMultiplexer.Connect(appsettings.Connections.Redis), $"fumobot:{appsettings.Website.DataProtection.RedisKey}")
    .ProtectKeysWithCertificate(new X509Certificate2(appsettings.Website.DataProtection.CertificateFile, appsettings.Website.DataProtection.CertificatePass));

builder.Services
    .AddAuthentication(x =>
    {
        x.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        x.DefaultChallengeScheme = TwitchAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(x =>
    {
        x.LoginPath = "/api/Account/Login";
        x.LogoutPath = "/api/Account/Logout";
        x.Cookie.Name = "Fumo.Token";
        x.Cookie.SameSite = SameSiteMode.Strict;
        x.Cookie.HttpOnly = true;
        x.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        x.ExpireTimeSpan = TimeSpan.FromDays(30);
    })
    .AddTwitch(x =>
    {
        x.ForceVerify = false;
        x.ClientId = appsettings.Twitch.ClientID;
        x.ClientSecret = appsettings.Twitch.ClientSecret;
        x.SaveTokens = true;
        x.Scope.Add("openid");
        x.Scope.Add("user:read:email");
        x.Scope.Add("channel:bot");

        x.Events.OnCreatingTicket = async (context) =>
            {
                var userRepo = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
                var oauthRepo = context.HttpContext.RequestServices.GetRequiredService<IUserOAuthRepository>();

                var userId = context.Principal!.FindFirstValue(ClaimTypes.NameIdentifier)
                            ?? throw new InvalidOperationException("Unable to find user ID in claims");

                if (!DateTime.TryParse(context.Properties.GetTokenValue("expires_at"), out DateTime expiresAt))
                {
                    return;
                }

                expiresAt = expiresAt.ToUniversalTime();

                var user = await userRepo.SearchID(userId);

                if (user is null)
                {
                    return;
                }

                var existing = await oauthRepo.Get(user.TwitchID, TwitchAuthenticationDefaults.Issuer);

                if (existing is not null)
                {
                    existing.AccessToken = context.AccessToken!;
                    existing.RefreshToken = context.RefreshToken!;
                    existing.ExpiresAt = expiresAt;

                    await oauthRepo.CreateOrUpdate(existing);
                }
                else
                {
                    var oauth = new UserOauthDTO
                    {
                        TwitchID = user.TwitchID,
                        Provider = TwitchAuthenticationDefaults.Issuer,
                        AccessToken = context.AccessToken!,
                        RefreshToken = context.RefreshToken!,
                        ExpiresAt = expiresAt
                    };

                    await oauthRepo.CreateOrUpdate(oauth);
                }
            };
    });

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
        }
    }
}
