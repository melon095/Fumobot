using Fumo.Database.DTO;
using MiniTwitch.Irc.Models;

namespace Fumo.Shared.Models;

// TODO: Remove the Privmsg dependency
public record ChatMessage(
    ChannelDTO Channel,
    UserDTO User,
    List<string> Input,
    bool IsBroadcaster,
    bool IsMod,
    string? ReplyID = null
);
