using Autofac;
using Fumo.Database.DTO;

namespace Fumo.Shared.Models;

public record ChatMessage(
    ChannelDTO Channel,
    UserDTO User,
    List<string> Input,
    bool IsBroadcaster,
    bool IsMod,
    ILifetimeScope Scope,
    string? ReplyID = null
);