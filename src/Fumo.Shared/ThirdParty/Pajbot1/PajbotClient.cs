using System.Net.Http.Headers;
using System.Net.Http.Json;
using Fumo.Shared.Models;
using Serilog;

namespace Fumo.Shared.ThirdParty.Pajbot1;

public interface IPajbotClient
{
    string NormalizeDomain(string input);

    ValueTask<bool> ValidateDomain(string url, CancellationToken ct = default);

    /// <exception cref="Exception"></exception>
    ValueTask<bool> Check(string message, string baseURL, CancellationToken cancellationToken);
}

public class PajbotClient : IPajbotClient
{
    private static readonly string Endpoint = "api/v1/banphrases/test";
    private static readonly MediaTypeHeaderValue ContentType = new("application/x-www-form-urlencoded");

    private readonly ILogger Logger;
    private readonly IHttpClientFactory Factory;


    public PajbotClient(ILogger logger, IHttpClientFactory factory)
    {
        Logger = logger.ForContext<PajbotClient>();
        Factory = factory;
    }

    public string NormalizeDomain(string input)
    {
        if (!input.StartsWith("https://"))
        {
            input = "https://" + input;
        }

        if (input.EndsWith('/'))
        {
            input = input[..^1];
        }

        if (input.EndsWith(Endpoint))
        {
            input = input[..^Endpoint.Length];
        }

        return input;
    }

    public async ValueTask<bool> ValidateDomain(string url, CancellationToken ct = default)
    {
        try
        {
            using var client = Factory.CreateClient("Pajbot");

            await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url), ct);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async ValueTask<bool> Check(string message, string baseURL, CancellationToken cancellationToken)
    {
        using var client = Factory.CreateClient("Pajbot");
        Uri url = new(new Uri(baseURL), Endpoint);

        Dictionary<string, string> formData = new()
        {
            { "message", message }
        };

        var content = new FormUrlEncodedContent(formData);

        content.Headers.ContentType = ContentType;

        var result = await client.PostAsync(url, content, cancellationToken);
        result.EnsureSuccessStatusCode();

        var response = await result.Content.ReadFromJsonAsync<PajbotResponse>(FumoJson.SnakeCase, cancellationToken: cancellationToken);

        if (response is null || response.Banned == false) return false;

        Logger.Information("Pajbot Banphrase triggered: {BanphraseName} {Message}", response.BanphraseData.Name, message);

        return true;
    }
}
