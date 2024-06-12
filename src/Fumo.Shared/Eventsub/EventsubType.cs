using System.Text.Json.Serialization;
using MiniTwitch.Helix.Requests;

namespace Fumo.Shared.Eventsub;

public interface IEventsubType
{
    string Name { get; }
    string[] RequiredScopes { get; }
    string Version { get; }
    TimeSpan SuccessCooldown { get; }
}

public record EventsubType<TCondition>(string Name, string[] RequiredScopes, TimeSpan SuccessCooldown, string Version = "1")
    : IEventsubType
    where TCondition : class
{
    public static readonly EventsubType ChannelChatMessage = new("channel.chat.message", ["channel:bot"], SuccessCooldown: DefaultCooldown);

    public static readonly Dictionary<string, EventsubType> TypeMapper = new()
    {
        [ChannelChatMessage.Name] = ChannelChatMessage,
    };

    public bool ShouldSetCooldown => SuccessCooldown != default;

    public NewSubscription ToHelix(NewSubscription.EventsubTransport transport, TCondition condition)
        => new(Name, Version, transport, condition);

    private static readonly TimeSpan DefaultCooldown = TimeSpan.FromMinutes(1);
}

public record EventsubType(string Name, string[] RequiredScopes, string Version = "1", TimeSpan SuccessCooldown = default)
    : EventsubType<EventsubBasicCondition>(Name, RequiredScopes, SuccessCooldown, Version);
