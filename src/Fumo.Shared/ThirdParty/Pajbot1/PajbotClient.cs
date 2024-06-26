﻿using System.Net.Http.Headers;
using System.Net.Http.Json;
using Fumo.Shared.Models;

namespace Fumo.Shared.ThirdParty.Pajbot1;

public class PajbotClient
{
    private static readonly string Endpoint = "api/v1/banphrases/test";

    private HttpClient HttpClient { get; set; }

    public PajbotClient()
    {
        HttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(10),
        };
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
    public async ValueTask<(bool Banned, string Reason)> Check(string message, string baseURL, CancellationToken cancellationToken)
    {
        var url = $"{baseURL}/{Endpoint}";

        Dictionary<string, string> formData = new()
        {
            { "message", message }
        };

        var content = new FormUrlEncodedContent(formData);

        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

        // Not going to catch the exceptions. I would rather have the caller worry about it.
        var result = await HttpClient.PostAsync(url, content, cancellationToken);

        if (!result.IsSuccessStatusCode)
        {
            return (true, $"bad Status Code: {result.StatusCode}");
        }

        var response = await result.Content.ReadFromJsonAsync<PajbotResponse>(FumoJson.SnakeCase, cancellationToken: cancellationToken);

        return response is null
            ? (true, "FeelsOkayMan blocked by Pajbot")
            : (response.Banned, "FeelsOkayMan blocked by Pajbot");
    }
}
