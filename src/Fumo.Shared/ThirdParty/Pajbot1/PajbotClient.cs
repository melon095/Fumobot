using System.Net.Http.Headers;
using System.Net.Http.Json;
using Fumo.Shared.Models;
using Serilog;

namespace Fumo.Shared.ThirdParty.Pajbot1;

public class PajbotClient : IDisposable
{
    private static readonly string Endpoint = "api/v1/banphrases/test";
    private static readonly MediaTypeHeaderValue ContentType = new("application/x-www-form-urlencoded");
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    private readonly ILogger Logger;
    private readonly HttpClient HttpClient;

    public PajbotClient(ILogger logger)
    {
        Logger = logger.ForContext<PajbotClient>();

        HttpClient = new()
        {
            Timeout = DefaultTimeout
        };
    }

    public void Dispose()
    {
        HttpClient?.Dispose();

        GC.SuppressFinalize(this);
    }

    public static string NormalizeDomain(string input)
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
            await HttpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, url), ct);

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <exception cref="Exception"></exception>
    public async ValueTask<bool> Check(string message, string baseURL, CancellationToken cancellationToken)
    {
        Uri url = new(new Uri(baseURL), Endpoint);

        Dictionary<string, string> formData = new()
        {
            { "message", message }
        };

        var content = new FormUrlEncodedContent(formData);

        content.Headers.ContentType = ContentType;

        var result = await HttpClient.PostAsync(url, content, cancellationToken);
        result.EnsureSuccessStatusCode();

        var response = await result.Content.ReadFromJsonAsync<PajbotResponse>(FumoJson.SnakeCase, cancellationToken: cancellationToken);

        if (response is null || response.Banned == false) return false;

        Logger.Information("Pajbot Banphrase triggered: {BanphraseName} {Message}", response.BanphraseData.Name, message);

        return true;
    }
}
