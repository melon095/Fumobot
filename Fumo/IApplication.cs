using Fumo.Database.DTO;
using Fumo.Models;

namespace Fumo;

internal interface IApplication
{
    event Func<ChatMessage, CancellationToken, ValueTask> OnMessage;

    DateTime StartTime { get; }

    Dictionary<string, ChannelDTO> Channels { get; set; }

    Task StartAsync();
}
