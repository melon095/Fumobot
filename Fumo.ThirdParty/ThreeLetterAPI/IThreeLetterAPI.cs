namespace Fumo.ThirdParty.ThreeLetterAPI;

public interface IThreeLetterAPI
{
    /// <returns>
    /// Responds with the $.data property of the response.
    /// </returns>
    /// <exception cref="Exceptions.ThreeLetterAPIException">
    /// Throws when the $.error property exists. Contains $.error[0].message
    /// </exception>
    /// <exception cref="Exception">
    /// Throws when the status is not 200. indicating the service is broken.
    /// </exception>
    Task<TResponse> SendAsync<TResponse>(IThreeLetterAPIInstruction instructions, CancellationToken cancellationToken = default);

    Task<List<TResponse>> PaginatedQueryAsync<TResponse>(Func<TResponse?, IThreeLetterAPIInstruction?> prepare, CancellationToken cancellationToken = default);

}
