namespace Fumo.WebService.Models;

public class BasicCommandDTO
{
    public string NameMatcher { get; set; }

    public List<string> Permissions { get; set; }

    public string Description { get; set; }

    public int Cooldown { get; set; }
}
