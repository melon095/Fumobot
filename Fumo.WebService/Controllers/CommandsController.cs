using Fumo.Shared.Models;
using Fumo.Shared.Repositories;
using Fumo.WebService.Mapper;
using Fumo.WebService.Models;
using Microsoft.AspNetCore.Mvc;

namespace Fumo.WebService.Controllers;

[ApiController]
[Route("[controller]")]
public class CommandsController : ControllerBase
{
    private readonly CommandMapper Mapper;
    private readonly CommandRepository CommandRepository;
    private readonly IConfiguration Config;

    public CommandsController(CommandMapper mapper, CommandRepository commandRepository, IConfiguration config)
    {
        Mapper = mapper;
        CommandRepository = commandRepository;
        Config = config;

        // TODO: Move this somewhere else maybe
        if (CommandRepository.Commands is null)
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

    [HttpGet("{name}")]
    public async Task<ActionResult<IndepthCommandDTO>> GetByName(string name, CancellationToken ct)
    {
        var prefix = Config["GlobalPrefix"] ?? "!";

        //// No there's not much i can do here. Maybe having a UUID for every command would be possible.
        var command = CommandRepository.Commands.Where(x => x.Key.ToString() == name).FirstOrDefault().Value;

        if (command is null)
        {
            return NotFound();
        }

        var instance = (Activator.CreateInstance(command) as ChatCommand)!;

        var description = await instance.GenerateWebsiteDescription(prefix, ct);

        BasicCommandDTO dto = Mapper.CommandToBasic(instance);

        // TODO: Can mapperly do this for me.
        IndepthCommandDTO indepth = new()
        {
            NameMatcher = dto.NameMatcher,
            Permissions = dto.Permissions,
            DetailedDescription = CleanDescription(description),
            Cooldown = dto.Cooldown,
        };

        return indepth;
    }

    private static string CleanDescription(string dirt)
        => dirt.Replace("%TAB%", "&#9;");
}