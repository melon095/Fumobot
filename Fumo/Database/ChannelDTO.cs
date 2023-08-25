using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fumo.Database;

[Index(nameof(TwitchID), IsUnique = true)]
[Index(nameof(UserTwitchID), IsUnique = true)]
public class ChannelDTO
{
    [Key]
    public required string TwitchID { get; set; }

    public required string TwitchName { get; set; }

    public required DateTime DateJoined { get; set; }

    [ForeignKey("User")]
    public required string UserTwitchID { get; set; }
    public required UserDTO User { get; set; }

    [Column(TypeName = "jsonb")]
    public required Setting[] Settings { get; set; }
}
