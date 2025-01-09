using Autofac;
using Fumo.Shared.Models;
using Serilog;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Fumo.Shared.Repositories;

public class CommandRepository
{
    public readonly Dictionary<Regex, Type> Commands = [];

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
                if (RuntimeHelpers.GetUninitializedObject(x) is ChatCommand c)
                {
                    c.OnInit();

                    Commands.Add(c.NameMatcher, x);
                }
            });
    }

    private T? Search<T>(string identifier, Func<Type, T> action)
    {
        foreach (var command in Commands)
        {
            if (command.Key.IsMatch(identifier))
            {
                return action(command.Value);
            }
        }

        return default;
    }

    public Type? GetCommandType(string identifier)
        => Search(identifier, type => type);
}
