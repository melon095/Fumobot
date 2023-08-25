using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fumo.Database;

public class UserDTO
{
    [Key]
    public required string TwitchID { get; set; }

    public required string TwitchName { get; set; }

    public required string[] UsernameHistory { get; set; }

    public required DateTime DateSeen { get; set; }

    [Column(TypeName = "jsonb")]
    public required Setting[] Settings { get; set; }
}
