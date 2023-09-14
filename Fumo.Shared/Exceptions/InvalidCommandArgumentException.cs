namespace Fumo.Shared.Exceptions;

// Yummy long exception names
public class InvalidCommandArgumentException : InvalidInputException
{
    public InvalidCommandArgumentException(string param, string? message)
        : base($"Parameter {param} {message}")
    {
    }

    public InvalidCommandArgumentException(string? message) : base(message)
    {
    }
}
