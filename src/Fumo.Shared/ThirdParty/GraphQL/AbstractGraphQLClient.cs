using Fumo.Shared.ThirdParty.Exceptions;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fumo.Shared.ThirdParty.GraphQL;

public abstract class AbstractGraphQLClient
{
    private static readonly JsonSerializerOptions DefaultSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    protected JsonSerializerOptions SerializerOptions = DefaultSerializerOptions;

    protected abstract string Name { get; }

    private readonly IHttpClientFactory Factory;

    public AbstractGraphQLClient(IHttpClientFactory factory)
    {
        Factory = factory;
    }

    protected void WithSerializerOptions(JsonSerializerOptions options)
    {
        SerializerOptions = options;
    }

    protected ValueTask<TResponse> Send<TResponse>(IGraphQLInstruction instructions, CancellationToken ct)
        => Send<TResponse>(instructions.Create(), ct);

    protected async ValueTask<TResponse> Send<TResponse>(GraphQLRequest request, CancellationToken cancellationToken)
    {
        using var client = Factory.CreateClient(Name);

        var response = await client.PostAsJsonAsync(string.Empty, request, cancellationToken);

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
