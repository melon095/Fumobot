namespace Fumo.Application.Web.Models;

public class IndepthCommandDTO
{
    public string Regex { get; set; }

    public IReadOnlyList<string> Permission { get; set; }

    public string Description { get; set; }
}