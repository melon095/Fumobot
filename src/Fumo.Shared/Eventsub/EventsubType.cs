namespace Fumo.Shared.Eventsub;

public record EventsubType(string Name, string[] RequiredScopes)
{
    public static readonly EventsubType ChannelChatMessage = new("channel.chat.message", ["user:read:chat"]);
}
