using System.Net.Http.Json;

namespace Fumo.ThirdParty.Pajbot1;

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

    /// <exception cref="Exception"></exception>
    public async Task<PajbotResponse> Check(string message, string baseURL, CancellationToken cancellationToken)
    {
        if (!baseURL.StartsWith("https://")) baseURL = $"https://{baseURL}";

        var url = $"{baseURL}/{Endpoint}";

        PajbotRequest request = new(message);

        var result = await this.HttpClient.PostAsJsonAsync(url, request, cancellationToken);

        if (result.IsSuccessStatusCode)
        {
            var response = await result.Content.ReadFromJsonAsync<PajbotResponse>(cancellationToken: cancellationToken);

            return response is null
                ? throw new Exception("Pajbot returned null")
                : response;
        }
        else
        {
            throw new Exception($"Pajbot at {url} responded with a bad error code ({result.StatusCode})");
        }
    }
}
