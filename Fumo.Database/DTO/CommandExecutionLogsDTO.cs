using System.ComponentModel.DataAnnotations.Schema;

namespace Fumo.Database.DTO;

public class CommandExecutionLogsDTO
{
    public Guid Id { get; set; }

    [ForeignKey(nameof(Channel))]
    public string ChannelId { get; set; }

    [ForeignKey(nameof(User))]
    public string UserId { get; set; }

    public bool Success { get; set; }

    public string CommandName { get; set; }

    public List<string> Input { get; set; }

    public string Result { get; set; }

    public DateTime Date { get; set; }

    public ChannelDTO Channel { get; set; }

    public UserDTO User { get; set; }
}
