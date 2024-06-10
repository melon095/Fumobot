namespace Fumo.Shared.Eventsub;

public enum EventsubCommandType
{
    Verification,
    Revocation,
    Notification
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class EventsubCommandAttribute(EventsubCommandType type, string name) : Attribute
{
    public EventsubCommandType Type { get; } = type;
    public string Name { get; } = name;
}
