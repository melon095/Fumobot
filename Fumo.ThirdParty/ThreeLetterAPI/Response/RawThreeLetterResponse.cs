using System.Text.Json.Serialization;

namespace Fumo.ThirdParty.ThreeLetterAPI.Response;

public record Error(
    [property: JsonPropertyName("message")] string Message
);

public record Extensions(
    [property: JsonPropertyName("durationMilliseconds")] int DurationMilliseconds,
    [property: JsonPropertyName("requestID")] string RequestID
);

public record RawThreeLetterResponse<TResponse>(
    [property: JsonPropertyName("data")] TResponse Data,
    [property: JsonPropertyName("errors")] IReadOnlyList<Error> Errors,
    [property: JsonPropertyName("extensions")] Extensions Extensions
);


