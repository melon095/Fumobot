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
            Description = x.Value.Metadata.Description,
            Cooldown = (int)x.Value.Metadata.Cooldown.TotalSeconds,
        }).ToList();

    [HttpGet("{name}")]
    public async ValueTask<ActionResult<IndepthCommandDTO>> GetByName(string name, CancellationToken ct)
    {
        var command = DescriptionService.GetMetadataByDisplayName(name);
        if (command is null)
            return NotFound();

        var description = await DescriptionService.CreateHelp(name, ct);

        return Ok(new IndepthCommandDTO
        {
            Regex = command.Name,
            Permission = command.Permissions,
            Description = description,
        });
    }
}
