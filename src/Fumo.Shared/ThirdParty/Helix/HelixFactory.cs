using System.Net.Http.Json;
using Fumo.Shared.Models;
using Microsoft.Extensions.Logging;
using MiniTwitch.Helix;
using Serilog;
using StackExchange.Redis;

namespace Fumo.Shared.ThirdParty.Helix;

public interface IHelixFactory
{
    ValueTask<HelixWrapper> Create(CancellationToken ct);
}

public class HelixFactory : IHelixFactory
{
    private readonly Serilog.ILogger Logger;
    private readonly IDatabase Redis;
    private readonly AppSettings Settings;
    private readonly ILoggerFactory LoggerFactory;
    private readonly IHttpClientFactory HttpFactory;

    private HelixWrapper? Helix = null;
    private readonly long UserId;

    private const string TokenEndpoint = "https://id.twitch.tv/oauth2/token";
    private const string TokenKey = "twitch:token";

    public HelixFactory(Serilog.ILogger logger, IDatabase redis, AppSettings settings, IHttpClientFactory factory)
    {
        Logger = logger.ForContext<HelixFactory>();
        Redis = redis;
        Settings = settings;
        UserId = long.Parse(settings.Twitch.UserID);
        LoggerFactory = new LoggerFactory().AddSerilog(logger);
        HttpFactory = factory;
    }

    private async ValueTask SaveToken(TwitchToken token)
        => await Redis.StringSetAsync(TokenKey, token.AccessToken, expiry: TimeSpan.FromSeconds(token.ExpiresIn));

    private async ValueTask<string?> GetToken()
        => await Redis.StringGetAsync(TokenKey);

    private async ValueTask<TwitchToken> QueryForToken(CancellationToken ct)
    {
        // TODO: error handling but twitch doesn't document error response xd

        try
        {
            using var client = HttpFactory.CreateClient("HelixFactory");
            var request = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = Settings.Twitch.ClientID,
                    ["client_secret"] = Settings.Twitch.ClientSecret,
                    ["grant_type"] = "client_credentials"
                }),
            };

            var response = await client.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            return (await response.Content.ReadFromJsonAsync<TwitchToken>(FumoJson.SnakeCase, ct))!;
        }
        catch (Exception e)
        {
            Logger.Error(e, "Failed to get token");
            throw;
        }
    }

    public async ValueTask<HelixWrapper> Create(CancellationToken ct)
    {
        var token = await GetToken();
        if (token is null)
        {
            Logger.Information("App Access Token expired. Creating new one");

            var newToken = await QueryForToken(ct);
            await SaveToken(newToken);

            token = newToken.AccessToken;

            Logger.Information("New App Access Token has been created. Expires in {ExpiresIn} seconds", newToken.ExpiresIn);

            Helix?.Client.ChangeToken(token, UserId);
        }

        Helix ??= new(token, UserId, LoggerFactory.CreateLogger<HelixWrapper>());

        return Helix;
    }
}
