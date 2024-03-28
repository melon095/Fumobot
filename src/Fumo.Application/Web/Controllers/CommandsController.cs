using Fumo.Application.Web.Models;
using Fumo.Application.Web.Service;
using Fumo.Shared.Models;
using Fumo.Shared.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Fumo.Application.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommandsController : ControllerBase
{
    private readonly CommandRepository CommandRepository;
    private readonly DescriptionService DescriptionService;
    private readonly string GlobalPrefix;

    public CommandsController(CommandRepository commandRepository, AppSettings config, DescriptionService descriptionService)
    {
        CommandRepository = commandRepository;
        DescriptionService = descriptionService;
        GlobalPrefix = config.GlobalPrefix;
    }

    [HttpGet]
    public async ValueTask<IEnumerable<BasicCommandDTO>> Get(CancellationToken ct)
    {
        List<BasicCommandDTO> commands = new();

        foreach (var command in CommandRepository.Commands)
        {
            // not very nice xp
            var instance = Activator.CreateInstance(command.Value) as ChatCommand
                ?? throw new Exception("Failed to create instance of command");

            var name = await DescriptionService.GetMatchingName(command.Value, ct);

            commands.Add(new()
            {
                Name = name,
                Description = instance.Description,
                Cooldown = (int)instance.Cooldown.TotalSeconds,
            });
        }

        return commands;
    }

    [HttpGet("{name}")]
    public async ValueTask<ActionResult<IndepthCommandDTO>> GetByName(string name, CancellationToken ct)
    {
        var command = await DescriptionService.GetCommandByID(name, ct);
        if (command is null)
        {
            return NotFound();
        }

        var description = await DescriptionService.CompileDescription(name, GlobalPrefix, ct);

        return Ok(new IndepthCommandDTO
        {
            Regex = command.NameMatcher.ToString(),
            Permission = command.Permissions,
            Description = description,
        });

    }
}
