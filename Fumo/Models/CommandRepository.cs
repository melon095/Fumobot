using Autofac.Core.Activators;
using Fumo.Interfaces.Command;
using Serilog;
using System.Collections.ObjectModel;
using System.Reflection;

namespace Fumo.Models;

public class CommandRepository
{
    /// <summary>
    /// Contains the list of commands read from the assembly.
    /// They're are not capable of being ran.
    /// </summary>
    private ReadOnlyCollection<ChatCommand> InitializedCommands;

    public CommandRepository(ILogger logger)
    {
        Logger = logger.ForContext<CommandRepository>();
    }

    public ILogger Logger { get; }

    public void LoadAssemblyCommands()
    {
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

        List<ChatCommand> anotherList = new();
        foreach (var command in commands)
        {
            this.Logger.Debug("Command loaded {Name}", command.Name);

            var instance = Activator.CreateInstance(command) as ChatCommand;
            if (instance is not null)
            {
                anotherList.Add(instance);
            }
        }

        this.InitializedCommands = new(anotherList);
    }

    public ChatCommand? GetCommand(string identifier)
    {
        if (this.InitializedCommands.Where(x => x.Name.Equals(identifier)) is ChatCommand command)
        {
            return (ChatCommand)Activator.CreateInstance(command.GetType())!;
        }

        return null;
    }
}
