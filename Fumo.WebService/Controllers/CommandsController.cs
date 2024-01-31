using Fumo.Shared.Models;
using Fumo.Shared.Repositories;
using Fumo.WebService.Models;
using Fumo.WebService.Service;
using Microsoft.AspNetCore.Mvc;

namespace Fumo.WebService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommandsController : ControllerBase
{
    private readonly CommandRepository CommandRepository;
    private readonly IConfiguration Config;
    private readonly DescriptionService DescriptionService;

    public CommandsController(CommandRepository commandRepository, IConfiguration config, DescriptionService descriptionService)
    {
        CommandRepository = commandRepository;
        Config = config;
        DescriptionService = descriptionService;
    }

    [HttpGet]
    public async Task<IEnumerable<BasicCommandDTO>> Get(CancellationToken ct)
    {
        List<BasicCommandDTO> commands = new();

        foreach (var command in CommandRepository.Commands)
        {
            // not very nice xp
            var instance = (Activator.CreateInstance(command.Value) as ChatCommand)
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
    public async Task<ActionResult<IndepthCommandDTO>> GetByName(string name, CancellationToken ct)
    {
        var prefix = Config["GlobalPrefix"] ?? "!";

        var command = await DescriptionService.GetCommandByID(name, ct);
        if (command is null)
        {
            return NotFound();
        }

        var description = await DescriptionService.CompileDescription(name, prefix, ct);

        return Ok(new IndepthCommandDTO
        {
            Regex = command.NameMatcher.ToString(),
            Permission = command.Permissions,
            Description = description,
        });

    }
}
