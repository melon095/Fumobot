using System.ComponentModel.DataAnnotations;

namespace Fumo.Database;

public class Setting
{
    [Key]
    public required string Key { get; set; }

    public required string Value { get; set; }
}
