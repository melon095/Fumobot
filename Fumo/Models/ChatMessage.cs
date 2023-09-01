using Fumo.Database.DTO;
using MiniTwitch.Irc.Models;

namespace Fumo.Models;

// TODO: Remove the Privmsg dependency
public record ChatMessage(
    ChannelDTO Channel,
    UserDTO User,
    List<string> Input,
    Privmsg Privmsg
);
