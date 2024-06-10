using System.Text.Json;
using Fumo.Shared.Models;
using MediatR;

namespace Fumo.Shared.Eventsub;

public interface IEventsubCommandFactory
{
    IRequest? Create(EventsubCommandType type, string name, JsonElement body);
}

public class EventsubCommandFactory : IEventsubCommandFactory
{
    private readonly IEventsubCommandRegistry Registry;

    public EventsubCommandFactory(IEventsubCommandRegistry registry)
    {
        Registry = registry;
    }

    public IRequest? Create(EventsubCommandType type, string name, JsonElement body)
    {
        var crlType = Registry.Get((name, type));
        if (crlType is null) return null;

        return (IRequest)body.Deserialize(crlType, FumoJson.SnakeCase)!;
    }
}
