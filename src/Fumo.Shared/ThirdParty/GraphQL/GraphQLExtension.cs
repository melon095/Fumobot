namespace Fumo.Shared.ThirdParty.GraphQL;

public record PersistedQuery(
    int Version,
    string Sha256Hash
);

public record GraphQLExtension(
    PersistedQuery PersistedQuery
);
