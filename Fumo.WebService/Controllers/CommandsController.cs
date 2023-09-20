using Fumo.Shared.Models;
using Fumo.Shared.Repositories;
using Fumo.WebService.Mapper;
using Fumo.WebService.Models;
using Fumo.WebService.Service;
using Microsoft.AspNetCore.Mvc;

namespace Fumo.WebService.Controllers;

[ApiController]
[Route("[controller]")]
public class CommandsController : ControllerBase
{
    private readonly CommandMapper Mapper;
    private readonly CommandRepository CommandRepository;
    private readonly IConfiguration Config;
    private readonly DescriptionService DescriptionService;

    public CommandsController(CommandMapper mapper, CommandRepository commandRepository, IConfiguration config, DescriptionService descriptionService)
    {
        Mapper = mapper;
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
    public IEnumerable<BasicCommandDTO> Get()
    {
        List<BasicCommandDTO> commands = new();

        foreach (var command in CommandRepository.Commands)
        {
            // not very nice xp
            var instance = Activator.CreateInstance(command.Value) as ChatCommand;

            commands.Add(Mapper.CommandToBasic(instance!));
        }

        return commands;
    }

    [HttpGet("{id}")]
    public ActionResult<IndepthCommandDTO> GetByName(Guid id)
    {
        var prefix = Config["GlobalPrefix"] ?? "!";

        var command = CommandRepository.GetCommand(id);

        if (command is null)
        {
            return NotFound();
        }

        var description = DescriptionService.CreateDescription(command);

        BasicCommandDTO dto = Mapper.CommandToBasic(command);

        // TODO: Can mapperly do this for me.
        IndepthCommandDTO indepth = new()
        {
            NameMatcher = dto.NameMatcher,
            Permissions = dto.Permissions,
            Cooldown = dto.Cooldown,
        };

        if (description is not null)
        {
            indepth.DetailedDescription = CleanDescription(description, prefix);
        }

        return Ok(indepth);
    }

    private static string CleanDescription(string dirt, string prefix)
        => dirt
            .Replace("%TAB%", "&#9;")
            .Replace("%PREFIX%", prefix);
}
