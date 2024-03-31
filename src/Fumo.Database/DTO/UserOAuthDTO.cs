using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Fumo.Database.DTO;

[PrimaryKey(nameof(TwitchID), nameof(Provider))]
public class UserOauthDTO
{
    [Required]
    [Column(Order = 0)]
    public string TwitchID { get; set; }

    [Required]
    [Column(Order = 1)]
    public string Provider { get; set; }

    [Required]
    public string AccessToken { get; set; }

    [Required]
    public string RefreshToken { get; set; }

    [Required]
    public DateTime ExpiresAt { get; set; }

    [ForeignKey(nameof(TwitchID))]
    public UserDTO User { get; set; }
}