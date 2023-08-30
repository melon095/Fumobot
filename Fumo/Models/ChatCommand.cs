using Fumo.Database.DTO;
using Fumo.Enums;
using Fumo.Interfaces.Command;
using MiniTwitch.Irc.Models;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace Fumo.Models;

public abstract class ChatCommand : IChatCommand
{
    public ChannelDTO Channel { get; set; }
    public UserDTO User { get; set; }
    public List<string> Input { get; set; }
    public Privmsg Privmsg { get; set; }

    /// <summary>
    /// Regex that matches the command
    /// </summary>
    /// 
    /// <note>
    ///     NameMatcher = new(..., RegexOptions.Compiled);
    /// </note>
    public Regex NameMatcher { get; }

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
    protected List<string> PrivatePermissions = new(new string[] { "default" });

    public IReadOnlyList<string> Permissions => this.PrivatePermissions;

    /// <summary>
    /// If Moderators and Broadcasters are the only ones that can execute this command in a chat
    /// </summary>
    public bool ModeratorOnly => (Flags & ChatCommandFlags.ModeratorOnly) != 0;

    /// <summary>
    /// If the Broadcaster of the chat is the only one that can execute this command in a chat
    /// </summary>
    public bool BroadcasterOnly => ((Flags & ChatCommandFlags.BroadcasterOnly) != 0);

    public TimeSpan Cooldown = TimeSpan.FromSeconds(5);

    public abstract Task<CommandResult> Execute(CancellationToken ct);

    public abstract Task<ReadOnlyCollection<string>>? GenerateWebsiteDescription(CancellationToken ct);
}
