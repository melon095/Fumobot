using System.Text.Json.Serialization;

namespace Fumo.Shared.ThirdParty.GraphQL;

public record GraphQLRequest
{
    public string? OperationName { get; init; }

    public GraphQLExtension? Extensions { get; init; }

    public string? Query { get; init; }

    public object? Variables { get; init; }
}
