using Fumo.ThirdParty.Exceptions;
using Fumo.ThirdParty.ThreeLetterAPI.Response;
using System.Net.Http.Json;
using System.Text.Json;

namespace Fumo.ThirdParty.GraphQL;

public abstract class AbstractGraphQLClient : IDisposable
{
    private static readonly string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/110.0";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
    
    protected HttpClient HttpClient { get; set; }

    private bool disposed = false;

    public AbstractGraphQLClient(string gqlAddress, HttpClient? httpClient = null)
    {
        HttpClient = httpClient ?? new HttpClient();

        this.HttpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
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

    protected Task<TResponse> SendAsync<TResponse>(IGraphQLInstruction instructions, CancellationToken ct)
        => SendAsync<TResponse>(instructions.Create(), ct);

    // FIXME: Maybe change that "object" constraint to something better
    protected async Task<TResponse> SendAsync<TResponse>(object request, CancellationToken cancellationToken)
    {
        var response = await HttpClient.PostAsJsonAsync(string.Empty, request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new GraphQLException("Bad response statuscode", response.StatusCode);
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
