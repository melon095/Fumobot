using Fumo.Shared.Models;
using Fumo.Shared.ThirdParty.GraphQL;
using Microsoft.Extensions.Configuration;

namespace Fumo.Shared.ThirdParty.ThreeLetterAPI;

public class ThreeLetterAPI : AbstractGraphQLClient, IThreeLetterAPI
{
    protected override Uri URI { get; } = new("https://gql.twitch.tv/gql");

    public ThreeLetterAPI(AppSettings config)
    {
        HttpClient.DefaultRequestHeaders.Add("Client-ID", config.Twitch.ThreeLetterAPI);

        WithBrowserUA();
    }

    public new ValueTask<TResponse> Send<TResponse>(IGraphQLInstruction instructions, CancellationToken cancellationToken = default)
        => base.Send<TResponse>(instructions, cancellationToken);

    public async ValueTask<List<TResponse>> PaginatedQuery<TResponse>(Func<TResponse?, IGraphQLInstruction?> prepare, CancellationToken cancellationToken = default)
    {
        List<TResponse> responses = [];

        // This could be fixed by passing in a separate instruction as a first parameter, but this should work.
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
}
