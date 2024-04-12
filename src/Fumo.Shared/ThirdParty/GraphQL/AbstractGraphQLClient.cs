using Fumo.Shared.ThirdParty.Exceptions;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fumo.Shared.ThirdParty.GraphQL;

public abstract class AbstractGraphQLClient : IDisposable
{
    private static readonly string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:120.0) Gecko/20100101 Firefox/120.0";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
    private static readonly JsonSerializerOptions DefaultSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    protected abstract Uri URI { get; }

    protected HttpClient HttpClient { get; set; }

    protected JsonSerializerOptions SerializerOptions = DefaultSerializerOptions;

    private bool Disposed = false;

    public AbstractGraphQLClient(HttpClient? httpClient = null)
    {
        HttpClient = httpClient ?? new HttpClient();

        HttpClient.Timeout = Timeout;
        HttpClient.BaseAddress = URI;
    }

    public void Dispose()
    {
        if (!Disposed)
        {
            GC.SuppressFinalize(this);

            HttpClient.Dispose();
        }

        Disposed = true;
    }

    protected void WithBrowserUA()
    {
        HttpClient.DefaultRequestHeaders.Remove("User-Agent");
        HttpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
    }

    protected void WithSerializerOptions(JsonSerializerOptions options)
    {
        SerializerOptions = options;
    }

    protected ValueTask<TResponse> Send<TResponse>(IGraphQLInstruction instructions, CancellationToken ct)
        => Send<TResponse>(instructions.Create(), ct);

    protected async ValueTask<TResponse> Send<TResponse>(GraphQLRequest request, CancellationToken cancellationToken)
    {
        var response = await HttpClient.PostAsJsonAsync(string.Empty, request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new GraphQLException($"Bad Response StatusCode ({response.StatusCode})", response.StatusCode);
        }

        var responseJson = await response.Content.ReadFromJsonAsync<GraphQLBaseResponse<TResponse>>(SerializerOptions, cancellationToken)
            ?? throw new GraphQLException("Failed to deserialize response");


        if (responseJson.Errors is not null)
        {
            // Really only need the first one

            var message = responseJson.Errors.ElementAt(0).Message;
            throw new GraphQLException(message);
        }

        return responseJson.Data;
    }
}
