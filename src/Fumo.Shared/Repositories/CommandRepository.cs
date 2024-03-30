using Autofac;
using Fumo.Shared.Interfaces.Command;
using Fumo.Shared.Models;
using Serilog;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Fumo.Shared.Repositories;

public class CommandRepository
{
    public readonly Dictionary<Regex, Type> Commands = new();

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
            .Where(x => x.IsClass && !x.IsAbstract && x.GetInterfaces().Contains(typeof(IChatCommand)) && x.IsSubclassOf(typeof(ChatCommand)))
            .ToList()
            .ForEach(x =>
            {
                var instance = Activator.CreateInstance(x);
                if (instance is ChatCommand c)
                {
                    // Prevents keeping a reference to the local instance variable.
                    var regexCopy = new Regex(c.NameMatcher.ToString(), c.NameMatcher.Options);

                    Commands.Add(regexCopy, c.GetType());
                }
            });

        Logger.Debug("Commands loaded {Commands}", Commands.Select(x => x.Key).ToArray());
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

    public ChatCommand? GetCommand(string identifier)
        => Search(identifier, x => Activator.CreateInstance(x) as ChatCommand);

    public ILifetimeScope? CreateCommandScope(string identifier, ILifetimeScope lifetimeScope)
        => Search(identifier, type => lifetimeScope.BeginLifetimeScope(x => x.RegisterType(type).As<ChatCommand>()));
}
