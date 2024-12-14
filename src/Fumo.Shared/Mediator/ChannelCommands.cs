using Fumo.Database.DTO;
using MediatR;

namespace Fumo.Shared.Mediator;

public record OnChannelCreatedCommand(ChannelDTO Channel) : INotification;

public record OnChannelDeletedCommand(ChannelDTO Channel) : INotification;
