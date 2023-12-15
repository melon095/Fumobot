using Fumo.ThirdParty.GraphQL;
using Microsoft.Extensions.Configuration;

namespace Fumo.ThirdParty.ThreeLetterAPI;

public class ThreeLetterAPI : AbstractGraphQLClient, IThreeLetterAPI
{
    public IConfiguration Config { get; }

    public ThreeLetterAPI(IConfiguration config)
        : base("https://gql.twitch.tv/gql")
    {
        Config = config;

        HttpClient.DefaultRequestHeaders.Add("Client-ID", Config["Twitch:ThreeLetterAPI"]);
    }

    public new Task<TResponse> Send<TResponse>(IGraphQLInstruction instructions, CancellationToken cancellationToken = default)
        // This is fine..
        => base.SendAsync<TResponse>(instructions, cancellationToken);

    public async Task<List<TResponse>> PaginatedQuery<TResponse>(Func<TResponse?, IGraphQLInstruction?> prepare, CancellationToken cancellationToken = default)
    {
        List<TResponse> responses = [];

        /*
            This could be fixed by passing in a separate instruction as a first parameter, but this should work. 
        */
        var instruction = prepare(default!);

        ArgumentNullException.ThrowIfNull(instruction, nameof(instruction));

        do
        {
            // TODO: Add exponential backoff maybe

            var response = await this.Send<TResponse>(instruction, cancellationToken);
            responses.Add(response);
            instruction = prepare(response);
        } while (instruction is not null);

        return responses;
    }

    // TODO Fix complexity limit. Max 35 instructions per request on some queries
    public async Task<TResponse> SendMultiple<TResponse>(IEnumerable<IGraphQLInstruction> instructions, CancellationToken cancellationToken = default)
    {
        List<GraphQLRequest> requestList = new();

        foreach (var instruction in instructions)
        {
            requestList.Add(instruction.Create());
        }

        return await SendAsync<TResponse>(requestList, cancellationToken);
    }
}
