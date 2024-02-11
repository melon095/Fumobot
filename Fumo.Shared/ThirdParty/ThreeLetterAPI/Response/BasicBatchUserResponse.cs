
using System.Text.Json.Serialization;

namespace Fumo.Shared.ThirdParty.ThreeLetterAPI.Response;

public record BasicBatchUserResponse(
    [property: JsonPropertyName("users")] IReadOnlyList<InnerUser> Users
    );

