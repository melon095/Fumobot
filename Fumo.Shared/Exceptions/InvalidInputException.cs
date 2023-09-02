namespace Fumo.Exceptions;

public class InvalidInputException : Exception
{
    public InvalidInputException(string? message) : base(message)
    {
    }
}
