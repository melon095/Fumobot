using AspNet.Security.OAuth.Twitch;
using Fumo.Database.DTO;
using Fumo.Shared.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Fumo.Shared.Models;
using Fumo.Shared.OAuth;
using StackExchange.Redis;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection;
using Fumo.Database.Migrations;

namespace Fumo.Application.Web;

public static class Setup
{
    private static readonly List<string> TWITCH_SCOPES =
    [
        "openid",
        "user:read:email",
        "channel:bot"
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

    public static WebApplicationBuilder SetupHTTPAuthentication(this WebApplicationBuilder builder, AppSettings settings)
    {
        builder.Services
            .AddAuthentication(x =>
            {
                x.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = TwitchAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(x =>
            {
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
            });

        return builder;
    }
}
