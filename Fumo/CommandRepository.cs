using Autofac;
using Fumo.Interfaces.Command;
using Fumo.Models;
using Serilog;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Fumo.Shared.Repositories;

public class CommandRepository
{
    private ReadOnlyDictionary<Regex, Type> Commands;

    public CommandRepository(ILogger logger, ILifetimeScope lifetimeScope)
    {
        Logger = logger.ForContext<CommandRepository>();
        LifetimeScope = lifetimeScope;
    }

    public ILogger Logger { get; }
    public ILifetimeScope LifetimeScope { get; }

    public void LoadAssemblyCommands()
    {
        Logger.Information("Loading commands");

        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        List<Type> commands = new();

        foreach (var assembly in assemblies)
        {
            var types = assembly.GetTypes()
                .Where(type =>
                    type.IsClass &&
                    !type.IsAbstract &&
                    type.GetInterfaces().Contains(typeof(IChatCommand)) &&
                    type.IsSubclassOf(typeof(ChatCommand))
                    );

            if (types is not null)
            {
                commands.AddRange(types);
            }
        }

        Dictionary<Regex, Type> anotherList = new();
        foreach (var command in commands)
        {
            Logger.Debug("Command loaded {Name}", command.Name);

            var instance = Activator.CreateInstance(command) as ChatCommand;
            if (instance is not null)
            {
                anotherList.Add(instance.NameMatcher, instance.GetType());
            }
        }

        Commands = new(anotherList);
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
