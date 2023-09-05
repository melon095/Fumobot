using System.Text.Json.Serialization;

namespace Fumo.ThirdParty.GraphQL;
public record PersistedQuery(
    [property: JsonPropertyName("version")] int Version,
    [property: JsonPropertyName("sha256Hash")] string Sha256Hash
);

public record GraphQLExtension(
    [property: JsonPropertyName("persistedQuery")] PersistedQuery PersistedQuery
);

