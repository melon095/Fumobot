using Fumo.Database.DTO;
using Fumo.Shared.Enums;
using Fumo.Shared.Interfaces.Command;
using MiniTwitch.Irc.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Fumo.Shared.Models;

public abstract class ChatCommand : ChatCommandArguments, IChatCommand
{
    #region Properties

    public ChannelDTO Channel { get; set; }
    public UserDTO User { get; set; }
    public List<string> Input { get; set; }

    /// <summary>
    /// The command invocation is the part of the message that matches the command name
    /// </summary>
    public string CommandInvocationName { get; set; }

    /// <summary>
    /// Regex that matches the command
    /// </summary>
    public Regex NameMatcher { get; protected set; }

    /// <summary>
    /// Flags that change behaviour
    /// </summary>
    public ChatCommandFlags Flags { get; protected set; } = ChatCommandFlags.None;

    public List<string> Permissions { get; protected set; } = ["default"];

    public string Description { get; protected set; } = "No description provided";

    /// <summary>
    /// If Moderators and Broadcasters are the only ones that can execute this command in a chat
    /// </summary>
    public bool ModeratorOnly => Flags.HasFlag(ChatCommandFlags.ModeratorOnly);

    /// <summary>
    /// If the Broadcaster of the chat is the only one that can execute this command in a chat
    /// </summary>
    public bool BroadcasterOnly => Flags.HasFlag(ChatCommandFlags.BroadcasterOnly);

    public TimeSpan Cooldown { get; protected set; } = TimeSpan.FromSeconds(5);

    #endregion

    public virtual ValueTask<CommandResult> Execute(CancellationToken ct)
        => throw new NotImplementedException();

    public virtual ValueTask BuildHelp(ChatCommandHelpBuilder builder, CancellationToken ct)
        => throw new NotImplementedException();

    #region Setters

    protected void SetName([StringSyntax(StringSyntaxAttribute.Regex)] string regex)
        => this.NameMatcher = new($"^{regex}", RegexOptions.Compiled);

    protected void SetCooldown(TimeSpan cd)
        => this.Cooldown = cd;

    protected void SetFlags(ChatCommandFlags flags)
        => Flags = flags;

    protected void AddPermission(string permission)
        => this.Permissions.Add(permission);

    protected void SetDescription(string description)
        => this.Description = description;

    #endregion
}
