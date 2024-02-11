using Fumo.Shared.ThirdParty.GraphQL;
using Microsoft.Extensions.Configuration;

namespace Fumo.Shared.ThirdParty.ThreeLetterAPI;

public class ThreeLetterAPI : AbstractGraphQLClient, IThreeLetterAPI
{
    public IConfiguration Config { get; }

    public ThreeLetterAPI(IConfiguration config)
        : base("https://gql.twitch.tv/gql")
    {
        Config = config;

        HttpClient.DefaultRequestHeaders.Add("Client-ID", Config["Twitch:ThreeLetterAPI"]);

        WithBrowserUA();
    }

    public new ValueTask<TResponse> Send<TResponse>(IGraphQLInstruction instructions, CancellationToken cancellationToken = default)
        // This is fine..
        => base.Send<TResponse>(instructions, cancellationToken);

    public async ValueTask<List<TResponse>> PaginatedQuery<TResponse>(Func<TResponse?, IGraphQLInstruction?> prepare, CancellationToken cancellationToken = default)
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
    public async ValueTask<TResponse> SendMultiple<TResponse>(IEnumerable<IGraphQLInstruction> instructions, CancellationToken cancellationToken = default)
    {
        List<GraphQLRequest> requestList = [];

        foreach (var instruction in instructions)
        {
            requestList.Add(instruction.Create());
        }

        return await Send<TResponse>(requestList, cancellationToken);
    }
}
