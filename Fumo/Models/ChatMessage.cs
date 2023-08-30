using Fumo.Database.DTO;
using MiniTwitch.Irc.Models;

namespace Fumo.Models;

public record ChatMessage(
    ChannelDTO Channel,
    UserDTO User,
    List<string> Input,
    Privmsg Privmsg
);
