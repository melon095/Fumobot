using Fumo.Shared.Models;
using Fumo.Shared.Repositories;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;

namespace Fumo.WebService.Service;

public record DocumentationFileRep(string Class, string? Doc);

public class DescriptionService
{
    private readonly ConcurrentDictionary<Guid, DocumentationFileRep> Dictionary = new();
    private readonly string DocumentationPath;
    private readonly CommandRepository CommandRepository;
    private bool Ready = false;

    public DescriptionService(CommandRepository commandRepository)
    {
        // Very nice
        var domain = AppDomain.CurrentDomain.BaseDirectory.Replace(nameof(WebService), nameof(Commands));

        Assembly.Load("Fumo.Commands");
        DocumentationPath = $"{domain}/Data/Documentation.json"; ;

        CommandRepository = commandRepository;
    }

    private async Task Load(CancellationToken ct)
    {
        if (Ready) return;

        var stream = File.Open(DocumentationPath, FileMode.Open);

        var root = await JsonSerializer.DeserializeAsync<Dictionary<Guid, DocumentationFileRep>>(stream, cancellationToken: ct)
            ?? throw new NullReferenceException("Failed to deserialize documentation file.");

        foreach (var (key, value) in root)
        {
            Dictionary.TryAdd(key, value);
        }

        Ready = true;
    }

    public async Task<ChatCommand?> GetCommandByID(Guid id, CancellationToken ct)
    {
        await Load(ct);

        if (!Dictionary.TryGetValue(id, out var doc))
        {
            return null;
        }

        foreach (var command in CommandRepository.Commands)
        {
            if (doc.Class == command.Value.FullName)
            {
                return Activator.CreateInstance(command.Value) as ChatCommand;
            }
        }

        return null;
    }

    public async Task<Guid> GetMatchingID(Type command, CancellationToken ct)
    {
        await Load(ct);

        foreach (var (key, value) in Dictionary)
        {
            if (value.Class == command.FullName)
            {
                return key;
            }
        }

        throw new NullReferenceException("Failed to find matching ID.");
    }

    public async Task<string> CompileDescription(Guid id, string prefix, CancellationToken ct)
    {
        await Load(ct);

        if (!Dictionary.TryGetValue(id, out var doc))
        {
            return string.Empty;
        }

        if (doc.Doc is null) return string.Empty;

        return CleanDescription(doc.Doc, prefix);
    }

    private static string CleanDescription(string dirt, string prefix)
        => dirt
            .Replace("%PREFIX%", prefix)
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
}
