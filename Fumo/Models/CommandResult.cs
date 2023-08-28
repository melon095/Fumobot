namespace Fumo.Models;

// TODO: Add more things here lol
public record CommandResult(string Message)
{
    public static implicit operator CommandResult(string message) => new(message);
}
