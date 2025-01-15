using Autofac;
using Fumo.Shared.Models;
using System.Collections.Concurrent;

namespace Fumo.Application.Web.Service;

using HelpStoreDictionary = ConcurrentDictionary<string, DescriptionService.HelpEntry>;

public class DescriptionService
{
    private static readonly HelpStoreDictionary HelpStore = [];

    private readonly ILifetimeScope LifetimeScope;
    private readonly string GlobalPrefix;

    public DescriptionService(ILifetimeScope lifetimeScope, AppSettings settings)
    {
        LifetimeScope = lifetimeScope;
        GlobalPrefix = settings.GlobalPrefix;
    }

    public async Task Prepare(CancellationToken ct)
    {
        foreach (var command in LifetimeScope.Resolve<IEnumerable<ChatCommand>>())
        {
            HelpEntry entry = new(null, command.Metadata, command.GetType());

            ChatCommandHelpBuilder helpBuilder = new(GlobalPrefix);

            await command.BuildHelp(helpBuilder, ct);

            var markdown = helpBuilder.BuildMarkdown();

            if (helpBuilder.ShouldBeCached)
                entry.CachedHelp = markdown;

            HelpStore.TryAdd(helpBuilder.DisplayName, entry);
        }
    }

    public HelpStoreDictionary GetAll() => HelpStore;

    public ChatCommandMetadata? GetMetadataByDisplayName(string name) => HelpStore.GetValueOrDefault(name)?.Metadata;

    public async ValueTask<string> CreateHelp(string name, CancellationToken ct)
    {
        if (!HelpStore.TryGetValue(name, out var entry))
            throw new NullReferenceException("Failed to find command.");

        if (entry.CachedHelp is not null)
            return entry.CachedHelp;

        var command = (ChatCommand)LifetimeScope.Resolve(entry.Type);

        ChatCommandHelpBuilder helpBuilder = new(GlobalPrefix);

        await command.BuildHelp(helpBuilder, ct);

        return helpBuilder.BuildMarkdown();
    }

    public class HelpEntry(string? cachedHelp, ChatCommandMetadata metadata, Type type)
    {
        public string? CachedHelp { get; set; } = cachedHelp;

        public ChatCommandMetadata Metadata { get; } = metadata;

        public Type Type { get; } = type;
    }
}
