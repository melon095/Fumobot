using System.Text.Json.Serialization;

namespace Fumo.ThirdParty.ThreeLetterAPI;
public record PersistedQuery(
    [property: JsonPropertyName("version")] int Version,
    [property: JsonPropertyName("sha256Hash")] string Sha256Hash
);

public record Extension(
    [property: JsonPropertyName("persistedQuery")] PersistedQuery PersistedQuery
);

