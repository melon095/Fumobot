using Autofac;
using Fumo.Shared.Interfaces.Command;
using Fumo.Shared.Models;
using Serilog;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Fumo.Shared.Repositories;

public class CommandRepository
{
    public readonly Dictionary<Regex, Type> Commands = new();

    public CommandRepository(ILogger logger, ILifetimeScope lifetimeScope)
    {
        Logger = logger.ForContext<CommandRepository>();
        LifetimeScope = lifetimeScope;
    }

    public ILogger Logger { get; }
    public ILifetimeScope LifetimeScope { get; }

    public void LoadAssemblyCommands()
    {
        if (Commands.Count > 0)
        {
            Logger.Warning("Commands already loaded");
            return;
        }

        Logger.Information("Loading commands");

        List<Type> commands = Assembly.Load("Fumo.Commands")
            .GetTypes()
            .Where(x => x.IsClass && !x.IsAbstract && x.GetInterfaces().Contains(typeof(IChatCommand)) && x.IsSubclassOf(typeof(ChatCommand)))
            .ToList();

        foreach (var command in commands)
        {
            var instance = Activator.CreateInstance(command) as ChatCommand;
            if (instance is not null)
            {
                Commands.Add(instance.NameMatcher, instance.GetType());
            }
        }

        Logger.Debug("Commands loaded {Commands}", Commands.Select(x => x.Key).ToArray());
    }

    public ChatCommand? GetCommand(string identifier)
    {
        foreach (var command in Commands)
        {
            if (command.Key.IsMatch(identifier))
            {
                return Activator.CreateInstance(command.Value) as ChatCommand;
            }
        }

        return null;
    }

    // FIXME: This class is ugly fix it
    // FIXME: Yes this would create a memory leak if the one that runs the command doesn't call Dispose. I have no idea how else i should structure this.
    public ILifetimeScope? CreateCommandScope(string identifier)
    {
        // Try to match identifier by regex
        foreach (var command in Commands)
        {
            if (command.Key.IsMatch(identifier))
            {
                var scope = LifetimeScope.BeginLifetimeScope(x => x.RegisterType(command.Value).As<ChatCommand>());
                return scope;
            }
        }

        return null;
    }
}
