using System.Text.Json.Serialization;

namespace Fumo.Shared.ThirdParty.ThreeLetterAPI.Response;

public record Error(
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("locations")] IReadOnlyList<Location> Locations
);

public record Location(
    [property: JsonPropertyName("line")] int Line,
    [property: JsonPropertyName("column")] int Column
);

public record RawThreeLetterResponse<TResponse>(
    [property: JsonPropertyName("data")] TResponse Data,
    [property: JsonPropertyName("errors")] IReadOnlyList<Error> Errors
);


