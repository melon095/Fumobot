using Fumo.Models;

namespace Fumo.Interfaces;

public interface IMessageHandler
{
    public event Func<ChatMessage, CancellationToken, ValueTask> OnMessage;
}
