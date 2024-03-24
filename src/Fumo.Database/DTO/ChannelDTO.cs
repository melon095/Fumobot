using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fumo.Database.DTO;

[Index(nameof(TwitchID), IsUnique = true)]
[Index(nameof(UserTwitchID), IsUnique = true)]
public class ChannelDTO
{
    [Key]
    public required string TwitchID { get; set; }

    public required string TwitchName { get; set; }

    public DateTime DateJoined { get; set; }

    [ForeignKey("User")]
    public string UserTwitchID { get; set; }
    public UserDTO User { get; set; }

    public bool SetForDeletion { get; set; }

    [Column(TypeName = "jsonb")]
    public List<Setting> Settings { get; set; }
}
