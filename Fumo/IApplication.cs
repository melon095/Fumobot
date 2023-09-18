using Fumo.Database.DTO;
using Fumo.Shared.Models;

namespace Fumo;

internal interface IApplication
{
    event Func<ChatMessage, CancellationToken, ValueTask> OnMessage;
    Task StartAsync();
}
