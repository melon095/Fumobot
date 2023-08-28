using Fumo.Database;
using MiniTwitch.Irc.Models;

namespace Fumo.Models;

public record ChatMessage(
    ChannelDTO Channel,
    UserDTO User,
    List<string> Input,
    MessageData Data
);

public record MessageData(
    Privmsg Privmsg
);