using Fumo.Shared.Models;
using Fumo.Shared.Repositories;
using Fumo.WebService.Models;
using Fumo.WebService.Service;
using Microsoft.AspNetCore.Mvc;
using System.Security;

namespace Fumo.WebService.Controllers;

[ApiController]
[Route("v1/[controller]")]
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

        // TODO: Move this somewhere else maybe
        if (CommandRepository.Commands is { Count: 0 })
        {
            CommandRepository.LoadAssemblyCommands();
        }
    }

    [HttpGet]
    public async Task<IEnumerable<BasicCommandDTO>> Get(CancellationToken ct)
    {
        List<BasicCommandDTO> commands = new();

        foreach (var command in CommandRepository.Commands)
        {
            // not very nice xp
            var instance = Activator.CreateInstance(command.Value) as ChatCommand;

            var guid = await DescriptionService.GetMatchingID(command.Value, ct);

            commands.Add(new()
            {
                Id = guid,
                NameMatcher = instance.NameMatcher.ToString(),
                Description = instance.Description,
                // TODO: Not pretty
                Cooldown = (int)instance.Cooldown.TotalSeconds,
            });
        }

        return commands;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<IndepthCommandDTO>> GetByName(Guid id, CancellationToken ct)
    {
        var prefix = Config["GlobalPrefix"] ?? "!";

        var command = await DescriptionService.GetCommandByID(id, ct);
        if (command is null)
        {
            return NotFound();
        }

        var description = await DescriptionService.CompileDescription(id, prefix, ct);

        return Ok(new IndepthCommandDTO
        {
            Permission = command.Permissions,
            Description = description,
        });

    }
}
