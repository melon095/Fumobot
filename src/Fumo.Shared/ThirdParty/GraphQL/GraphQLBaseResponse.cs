namespace Fumo.Shared.ThirdParty.GraphQL;

public class GraphQLBaseResponse<TResponse> : GraphQLBaseResponse<TResponse, GraphQLError>;

public class GraphQLBaseResponse<TResponse, TError>
{
    public TResponse Data { get; init; }

    public IReadOnlyList<TError>? Errors { get; init; }
}

public record GraphQLError(
    string Message,
    IReadOnlyList<ErrorLocation> Locations
);

public record ErrorLocation(
    int Line,
    int Column
);
