using Fumo.Database.DTO;
using Fumo.Shared.Enums;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Fumo.Shared.Models;

public abstract partial class ChatCommand
{
    #region Properties

    public virtual ChatCommandMetadata Metadata { get; } = new();

    public ChatMessage Context { private get; set; }

    public ChannelDTO Channel => Context.Channel;

    public UserDTO User => Context.User;

    public List<string> Input => Context.Input;

    public Regex NameMatcher => Metadata._name;

    public ChatCommandFlags Flags => Metadata.Flags;

    public IReadOnlyList<string> Permissions => Metadata.Permissions;

    public string Description => Metadata.Description;

    public TimeSpan Cooldown => Metadata.Cooldown;

    /// <summary>
    /// If Moderators and Broadcasters are the only ones that can execute this command in a chat
    /// </summary>
    public bool ModeratorOnly => Flags.HasFlag(ChatCommandFlags.ModeratorOnly);

    /// <summary>
    /// If the Broadcaster of the chat is the only one that can execute this command in a chat
    /// </summary>
    public bool BroadcasterOnly => Flags.HasFlag(ChatCommandFlags.BroadcasterOnly);

    #endregion

    public virtual void OnInit() { }

    public abstract ValueTask<CommandResult> Execute(CancellationToken ct);

    public abstract ValueTask BuildHelp(ChatCommandHelpBuilder builder, CancellationToken ct);
}

public class ChatCommandMetadata
{
    // Not the prettiest
    public Regex _name;

    [StringSyntax(StringSyntaxAttribute.Regex)]
    public string Name
    {
        get => _name.ToString();
        init => _name = new Regex($"^{value}", RegexOptions.Compiled);
    }

    public string Description { get; init; } = "No description provided";

    public ChatCommandFlags Flags { get; init; } = ChatCommandFlags.None;

    public IReadOnlyList<string> Permissions { get; init; } = ["default"];

    public TimeSpan Cooldown { get; init; } = TimeSpan.FromSeconds(5);
}
