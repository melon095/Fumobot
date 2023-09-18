namespace Fumo.WebService.Models;

public class IndepthCommandDTO
{
    public string NameMatcher { get; set; }

    public List<string> Permissions { get; set; }

    public string? DetailedDescription { get; set; } = null;

    public int Cooldown { get; set; }
}
