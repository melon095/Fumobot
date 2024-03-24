using Fumo.Shared.ThirdParty.GraphQL;

namespace Fumo.Shared.ThirdParty.ThreeLetterAPI;

public interface IThreeLetterAPI
{
    /// <returns>
    /// Responds with the $.data property of the response.
    /// </returns>
    /// <exception cref="Exceptions.GraphQLException">
    /// Throws when the $.error property exists. Contains $.error[0].message
    /// </exception>
    /// <exception cref="Exception">
    /// Throws when the status is not 200. indicating the service is broken.
    /// </exception>
    ValueTask<TResponse> Send<TResponse>(IGraphQLInstruction instructions, CancellationToken cancellationToken = default);

    ValueTask<TResponse> SendMultiple<TResponse>(IEnumerable<IGraphQLInstruction> instructions, CancellationToken cancellationToken = default);

    ValueTask<List<TResponse>> PaginatedQuery<TResponse>(Func<TResponse?, IGraphQLInstruction?> prepare, CancellationToken cancellationToken = default);

}
