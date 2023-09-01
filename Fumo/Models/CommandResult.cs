namespace Fumo.Models;

// TODO: Add more things here lol
public class CommandResult
{
    public string Message { get; init; }

    public static implicit operator CommandResult(string message) => new() { Message = message };
}
