namespace Fumo.Shared.Models;

// TODO: Add more things here lol
public class CommandResult
{
    public string Message { get; set; }

    public string? ReplyID { get; set; } = null;

    public static implicit operator CommandResult(string message) => new() { Message = message };
}
