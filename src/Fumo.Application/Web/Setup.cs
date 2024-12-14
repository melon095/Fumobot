using Fumo.Database.DTO;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Fumo.Shared.Models;
using Fumo.Shared.OAuth;
using StackExchange.Redis;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection;
using AspNetCoreRateLimit;
using Fumo.Shared.Repositories;

namespace Fumo.Application.Web;

public static class Setup
{
    private const string NAUGHTY_SCOPE = "user:read:email";

    private static readonly List<string> TWITCH_SCOPES =
    [
        "openid",
        "channel:bot"
    ];


    private static readonly List<string> TWITCH_BOT_SCOPES =
    [
        "openid",
        "user:read:chat",
        "user:write:chat",
        "user:bot",
    ];

    public static WebApplicationBuilder SetupDataProtection(this WebApplicationBuilder builder, AppSettings settings)
    {
        var dataProtection = settings.Website.DataProtection;
        var redis = ConnectionMultiplexer.Connect(settings.Connections.Redis);

        builder.Services.AddDataProtection()
            .PersistKeysToStackExchangeRedis(redis, $"fumobot:{dataProtection.RedisKey}")
            .ProtectKeysWithCertificate(new X509Certificate2(dataProtection.CertificateFile, dataProtection.CertificatePass));

        return builder;
    }

    public static WebApplicationBuilder SetupRatelimitOptions(this WebApplicationBuilder builder)
    {
        var services = builder.Services;

        services.AddMemoryCache();

        services.Configure<IpRateLimitOptions>(o =>
        {
            o.EnableEndpointRateLimiting = true;
            o.StackBlockedRequests = false;
            o.RealIpHeader = "X-Real-IP";
            o.HttpStatusCode = 429;
            o.GeneralRules =
            [
                new()
                {
                    Endpoint = "*",
                    Limit = 1000,
                    Period = "1m"
                },
                new()
                {
                    Endpoint = "get:/Account/Join",
                    Limit = 4,
                    Period = "1h"
                }
            ];
        })
            .AddInMemoryRateLimiting()
            .AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();


        return builder;
    }

    public static WebApplicationBuilder SetupHTTPAuthentication(this WebApplicationBuilder builder, AppSettings settings)
    {
        builder.Services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, x =>
            {
                x.LoginPath = "/Account/Login";
                x.LogoutPath = "/Account/Logout";
                x.AccessDeniedPath = "/error/403";

                x.Cookie.Name = "Fumo.Token";
                x.Cookie.HttpOnly = true;
                x.ExpireTimeSpan = TimeSpan.FromDays(30);
            })
            .AddTwitch(x =>
            {
                x.ForceVerify = false;
                x.ClientId = settings.Twitch.ClientID;
                x.ClientSecret = settings.Twitch.ClientSecret;
                x.SaveTokens = true;

                foreach (var scope in TWITCH_SCOPES)
                {
                    x.Scope.Add(scope);
                }

                x.Scope.Remove(NAUGHTY_SCOPE);

                x.Events.OnCreatingTicket = async (context) =>
                {
                    var userRepo = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
                    var oauthRepo = context.HttpContext.RequestServices.GetRequiredService<IOAuthRepository>();

                    var userId = context.Principal!.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? throw new InvalidOperationException("User ID not found in claims");

                    if (!DateTime.TryParse(context.Properties.GetTokenValue("expires_at"), out DateTime expiresAt))
                    {
                        throw new InvalidOperationException("ExpiresAt not found in token");
                    }

                    expiresAt = expiresAt.ToUniversalTime();

                    var user = await userRepo.SearchID(userId);

                    var existing = await oauthRepo.Get(user.TwitchID, OAuthProviderName.Twitch);

                    if (existing is not null)
                    {
                        existing.AccessToken = context.AccessToken!;
                        existing.RefreshToken = context.RefreshToken!;
                        existing.ExpiresAt = expiresAt;
                        existing.Scopes = TWITCH_SCOPES;

                        await oauthRepo.Update(existing);
                    }
                    else
                    {
                        var oauth = new UserOauthDTO
                        {
                            TwitchID = user.TwitchID,
                            Provider = OAuthProviderName.Twitch,
                            AccessToken = context.AccessToken!,
                            RefreshToken = context.RefreshToken!,
                            Scopes = TWITCH_SCOPES,
                            ExpiresAt = expiresAt,
                        };

                        await oauthRepo.Update(oauth);
                    }
                };
            })
            .AddTwitch(OAuthProviderName.TwitchBot, x =>
            {
                x.ForceVerify = true;
                x.ClientId = settings.Twitch.ClientID;
                x.ClientSecret = settings.Twitch.ClientSecret;
                x.SaveTokens = true;

                foreach (var scope in TWITCH_BOT_SCOPES)
                {
                    x.Scope.Add(scope);
                }

                x.Scope.Remove(NAUGHTY_SCOPE);

                // Don't do anything with the token.
            });

        return builder;
    }
}
