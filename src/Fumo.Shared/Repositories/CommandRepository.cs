using Autofac;
using Fumo.Shared.Models;
using Serilog;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Fumo.Shared.Repositories;

public class CommandRepository
{
    public readonly Dictionary<Regex, ChatCommand> Commands = new();

    public ILogger Logger { get; }

    public CommandRepository(ILogger logger) => Logger = logger.ForContext<CommandRepository>();

    public void LoadAssemblyCommands()
    {
        if (Commands.Count > 0)
        {
            Logger.Warning("Commands already loaded");
            return;
        }

        Logger.Information("Loading commands");

        Assembly.Load("Fumo.Commands")
            .GetTypes()
            .Where(x => x.IsClass && !x.IsAbstract && x.IsSubclassOf(typeof(ChatCommand)))
            .ToList()
            .ForEach(x =>
            {
                if (Activator.CreateInstance(x) is ChatCommand c)
                    Commands.Add(c.NameMatcher, c);
            });

        Logger.Debug("Commands loaded {Commands}", Commands.Select(x => x.Key).ToArray());
    }

    private T? Search<T>(string identifier, Func<Type, T> action)
    {
        foreach (var command in Commands)
        {
            if (command.Key.IsMatch(identifier))
            {
                return action(command.Value.GetType());
            }
        }

        return default;
    }

    public Type? GetCommandType(string identifier)
        => Search(identifier, type => type);
}
