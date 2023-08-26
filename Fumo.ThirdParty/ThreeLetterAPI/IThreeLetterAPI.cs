namespace Fumo.ThirdParty.ThreeLetterAPI;

public interface IThreeLetterAPI
{
    IThreeLetterAPI AddInstruction(IThreeLetterAPIInstruction instruction);

    IThreeLetterAPI AddInstruction(string instruction);

    IThreeLetterAPI AddOperation(string operation, Extension extension);

    IThreeLetterAPI AddVariables(object variables);

    /// <returns>
    /// Responds with the $.data property of the response.
    /// </returns>
    /// <exception cref="Fumo.ThirdParty.Exceptions.ThreeLetterAPIException">
    /// Throws when the $.error property exists. Contains $.error[0].message
    /// </exception>
    /// <exception cref="System.Exception">
    /// Throws when the status is not 200. indicating the service is broken.
    /// </exception>
    Task<TResponse> SendAsync<TResponse>(CancellationToken cancellationToken = default);
}
