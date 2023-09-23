namespace Fumo.WebService.Models;

public class BasicCommandDTO
{

    public Guid Id { get; set; }

    public string NameMatcher { get; set; }

    public string Description { get; set; }

    public int Cooldown { get; set; }
}
