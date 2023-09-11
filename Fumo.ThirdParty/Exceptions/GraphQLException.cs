using System.Net;

namespace Fumo.ThirdParty.Exceptions;

public class GraphQLException : Exception
{
    /// <summary>
    /// A GQL server always returns an 200 OK if the request was successful. The errors are in the response body.
    /// </summary>
    public HttpStatusCode StatusCode { get; } = HttpStatusCode.OK;

    public GraphQLException(string message) : base(message)
    {
    }

    public GraphQLException(string message, HttpStatusCode statusCode) : base(message)
    {
        StatusCode = statusCode;
    }
}
