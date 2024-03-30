using Autofac;
using Fumo.Database.DTO;
using MiniTwitch.Irc.Models;

namespace Fumo.Shared.Models;

public record struct ChatMessage(
    ChannelDTO Channel,
    UserDTO User,
    List<string> Input,
    bool IsBroadcaster,
    bool IsMod,
    ILifetimeScope Scope,
    string? ReplyID = null
);