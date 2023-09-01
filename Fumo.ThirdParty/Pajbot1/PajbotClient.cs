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
        var url = $"{Endpoint}/{baseURL}";

        PajbotRequest request = new(message);

        var result = await this.HttpClient.PostAsJsonAsync(url, request, cancellationToken);

        if (result.IsSuccessStatusCode)
        {
            return await result!.Content!.ReadFromJsonAsync<PajbotResponse>(cancellationToken: cancellationToken)!;
        }
        else
        {
            throw new Exception($"Pajbot at {url} responded with a bad error code ({result.StatusCode})");
        }
    }
}
