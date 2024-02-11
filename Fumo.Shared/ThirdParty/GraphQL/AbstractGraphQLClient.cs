using Fumo.Shared.ThirdParty.Exceptions;
using Fumo.Shared.ThirdParty.ThreeLetterAPI.Response;
using System.Net.Http.Json;
using System.Text.Json;

namespace Fumo.Shared.ThirdParty.GraphQL;

public abstract class AbstractGraphQLClient : IDisposable
{
    private static readonly string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:120.0) Gecko/20100101 Firefox/120.0";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    protected HttpClient HttpClient { get; set; }

    private bool disposed = false;

    public AbstractGraphQLClient(string gqlAddress, HttpClient? httpClient = null)
    {
        HttpClient = httpClient ?? new HttpClient();

        this.HttpClient.Timeout = Timeout;

        this.HttpClient.BaseAddress = new(gqlAddress);
    }

    public void Dispose()
    {
        if (!disposed)
        {
            GC.SuppressFinalize(this);

            HttpClient.Dispose();
        }

        disposed = true;
    }

    protected void WithBrowserUA()
    {
        HttpClient.DefaultRequestHeaders.Remove("User-Agent");
        HttpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
    }

    protected ValueTask<TResponse> Send<TResponse>(IGraphQLInstruction instructions, CancellationToken ct)
        => Send<TResponse>(instructions.Create(), ct);

    // FIXME: Maybe change that "object" constraint to something better
    protected async ValueTask<TResponse> Send<TResponse>(object request, CancellationToken cancellationToken)
    {
        var response = await HttpClient.PostAsJsonAsync(string.Empty, request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new GraphQLException($"Bad Response StatusCode ({response.StatusCode})", response.StatusCode);
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        var responseJson = JsonSerializer.Deserialize<RawThreeLetterResponse<TResponse>>(responseBody);

        if (responseJson!.Errors is not null)
        {
            // Really only need the first one

            var message = responseJson.Errors.ElementAt(0).Message;
            throw new GraphQLException(message);
        }

        return responseJson.Data;
    }
}
