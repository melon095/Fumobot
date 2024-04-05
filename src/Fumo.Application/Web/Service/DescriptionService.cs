using Fumo.Shared.Models;
using Fumo.Shared.Repositories;
using System.Collections.Concurrent;

namespace Fumo.Application.Web.Service;

using HelpStoreDictionary = ConcurrentDictionary<string, DescriptionService.HelpEntry>;

public class DescriptionService
{
    private readonly HelpStoreDictionary HelpStore = [];
    private readonly CommandRepository CommandRepository;
    private readonly string GlobalPrefix;

    public DescriptionService(CommandRepository commandRepository, AppSettings settings)
    {
        CommandRepository = commandRepository;
        GlobalPrefix = settings.GlobalPrefix;
    }

    public async Task Prepare(CancellationToken ct)
    {
        foreach (var command in CommandRepository.Commands)
        {
            HelpEntry entry = new(null, command.Value);

            ChatCommandHelpBuilder helpBuilder = new(GlobalPrefix);

            await command.Value.BuildHelp(helpBuilder, ct);

            var markdown = helpBuilder.BuildMarkdown();

            if (helpBuilder.ShouldBeCached)
                entry.CachedHelp = markdown;

            HelpStore.TryAdd(helpBuilder.DisplayName, entry);
        }
    }

    public HelpStoreDictionary GetAll() => HelpStore;

    public ChatCommand? GetByDisplayName(string name) => HelpStore.GetValueOrDefault(name)?.Instance ?? null;

    public async ValueTask<string> CreateHelp(string name, CancellationToken ct)
    {
        if (!HelpStore.TryGetValue(name, out var entry))
            throw new NullReferenceException("Failed to find command.");

        if (entry.CachedHelp is not null)
            return entry.CachedHelp;

        ChatCommandHelpBuilder helpBuilder = new(GlobalPrefix);

        await entry.Instance.BuildHelp(helpBuilder, ct);

        return helpBuilder.BuildMarkdown();
    }

    public class HelpEntry(string? cachedHelp, ChatCommand instance)
    {
        public string? CachedHelp = cachedHelp;
        public ChatCommand Instance = instance;
    }
}

//    private static string CleanDescription(string dirt, string prefix)
//        => dirt
//            .Replace("%PREFIX%", prefix)
//            .Replace("<", "&lt;")
//            .Replace(">", "&gt;");
