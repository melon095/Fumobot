using Fumo.Shared.Models;
using MediatR;

namespace Fumo.Shared.MediatorCommands;

public record MessageCommand(ChatMessage Message) : INotification
{
    public static implicit operator MessageCommand(ChatMessage message) => new(message);
}