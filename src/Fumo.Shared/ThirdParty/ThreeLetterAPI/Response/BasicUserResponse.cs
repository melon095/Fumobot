using System.Text.Json.Serialization;

namespace Fumo.Shared.ThirdParty.ThreeLetterAPI.Response;

public record BasicUserResponse(
    [property: JsonPropertyName("user")] InnerUser User
);

public record InnerUser(
    [property: JsonPropertyName("id")] string ID,
    [property: JsonPropertyName("login")] string Login
);

