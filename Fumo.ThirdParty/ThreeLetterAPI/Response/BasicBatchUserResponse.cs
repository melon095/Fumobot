
using System.Text.Json.Serialization;

namespace Fumo.ThirdParty.ThreeLetterAPI.Response;

public record BasicBatchUserResponse(
    [property: JsonPropertyName("users")] IReadOnlyList<InnerUser> Users
    );

