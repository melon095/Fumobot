using System.Text.Json.Serialization;

namespace Fumo.Shared.ThirdParty.GraphQL;
public record PersistedQuery(
    [property: JsonPropertyName("version")] int Version,
    [property: JsonPropertyName("sha256Hash")] string Sha256Hash
);

public record GraphQLExtension(
    [property: JsonPropertyName("persistedQuery")] PersistedQuery PersistedQuery
);

