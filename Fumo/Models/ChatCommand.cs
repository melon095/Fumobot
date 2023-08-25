using Fumo.Database;
using Fumo.Enums;
using Fumo.Interfaces.Command;
using System.Collections.ObjectModel;

namespace Fumo.Models;

internal abstract class ChatCommand : IChatCommand
{
    public readonly ChannelDTO Channel;
    public readonly UserDTO User;
    public readonly string[] Input;

    /// <summary>
    /// Name of the command used for executing the command
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Flgas that change behaviour
    /// </summary>
    public ChatCommandFlags Flags
    {
        get => ChatCommandFlags.None;
    }

    /// <summary>
    /// Permissions a user requires to execute this command
    /// 
    /// Every user has "default"
    /// </summary>
    public readonly ReadOnlyCollection<string> Permission = new(new string[] { "default" });

    /// <summary>
    /// If Moderators and Broadcasters are the only ones that can execute this command in a chat
    /// </summary>
    public bool ModeratorOnly => (Flags & ChatCommandFlags.ModeratorOnly) != 0;

    /// <summary>
    /// If the Broadcaster of the chat is the only one that can execute this command in a chat
    /// </summary>
    public bool BroadcasterOnly => ((Flags & ChatCommandFlags.BroadcasterOnly) != 0);

    public abstract Task<string> Execute(CancellationToken ct);

    public abstract Task<List<string>>? GenerateWebsiteDescription(CancellationToken ct);
}
