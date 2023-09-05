namespace Fumo.ThirdParty.Exceptions;

public class GraphQLException : Exception
{
    public GraphQLException()
    {
    }

    public GraphQLException(string message) : base(message)
    {
    }

    public GraphQLException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
