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

    public new Task<TResponse> SendAsync<TResponse>(IGraphQLInstruction instructions, CancellationToken cancellationToken = default)
        // This is fine..
        => base.SendAsync<TResponse>(instructions, cancellationToken);

    public async Task<List<TResponse>> PaginatedQueryAsync<TResponse>(Func<TResponse?, IGraphQLInstruction?> prepare, CancellationToken cancellationToken = default)
    {
        List<TResponse> responses = new();

        /*
            This could be fixed by passing in a seperate instruction as a first parameter, but this should work. 
        */
        var instruction = prepare(default!);

        ArgumentNullException.ThrowIfNull(instruction, nameof(instruction));

        do
        {
            // TODO: Add exponential backoff maybe

            var response = await this.SendAsync<TResponse>(instruction, cancellationToken);
            responses.Add(response);
            instruction = prepare(response);
        } while (instruction is not null);

        return responses;
    }
}
