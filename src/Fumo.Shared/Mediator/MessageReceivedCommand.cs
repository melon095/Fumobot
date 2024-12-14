﻿using Fumo.Shared.Models;
using MediatR;

namespace Fumo.Shared.Mediator;

public record MessageReceivedCommand(ChatMessage Message) : INotification
{
    public static implicit operator MessageReceivedCommand(ChatMessage message) => new(message);
}