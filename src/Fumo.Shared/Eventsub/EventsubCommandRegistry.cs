using System.Reflection;

namespace Fumo.Shared.Eventsub;

using CommandKey = (string Name, EventsubCommandType Type);

public interface IEventsubCommandRegistry
{
    void RegisterAll();

    Type? Get(CommandKey key);
}


public class EventsubCommandRegistry : IEventsubCommandRegistry
{
    private readonly Dictionary<CommandKey, Type> CommandMap = [];

    public void RegisterAll() => Assembly.GetExecutingAssembly().GetTypes()
        .Where(t => t.GetCustomAttribute<EventsubCommandAttribute>() is not null)
        .ToList()
        .ForEach(t =>
        {
            var attr = t.GetCustomAttribute<EventsubCommandAttribute>()!;
            CommandMap.Add((attr.Name, attr.Type), t);
        });

    public Type? Get(CommandKey key)
        => CommandMap.TryGetValue(key, out var commandType) ? commandType : null;
}
