namespace Fumo.ThirdParty.Exceptions;

public class ThreeLetterAPIException : Exception
{
    public ThreeLetterAPIException()
    {
    }

    public ThreeLetterAPIException(string message) : base(message)
    {
    }

    public ThreeLetterAPIException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
