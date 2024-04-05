using Fumo.Application.Web.Models;
using Fumo.Application.Web.Service;
using Microsoft.AspNetCore.Mvc;

namespace Fumo.Application.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommandsController : ControllerBase
{
    private readonly DescriptionService DescriptionService;

    public CommandsController(DescriptionService descriptionService)
    {
        DescriptionService = descriptionService;
    }

    [HttpGet]
    public IEnumerable<BasicCommandDTO> Get()
        => DescriptionService.GetAll().Select(x => new BasicCommandDTO
        {
            Name = x.Key,
            Description = x.Value.Instance.Description,
            Cooldown = (int)x.Value.Instance.Cooldown.TotalSeconds,
        }).ToList();

    [HttpGet("{name}")]
    public async ValueTask<ActionResult<IndepthCommandDTO>> GetByName(string name, CancellationToken ct)
    {
        var command = DescriptionService.GetByDisplayName(name);
        if (command is null)
            return NotFound();

        var description = await DescriptionService.CreateHelp(name, ct);

        return Ok(new IndepthCommandDTO
        {
            Regex = command.NameMatcher.ToString(),
            Permission = command.Permissions,
            Description = description,
        });
    }
}
