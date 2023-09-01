using System.Text.Json.Serialization;

namespace Fumo.ThirdParty.ThreeLetterAPI.Response;

public record ChannelModsEdge(
[property: JsonPropertyName("cursor")] string Cursor,
[property: JsonPropertyName("grantedAt")] object GrantedAt,
[property: JsonPropertyName("isActive")] bool IsActive,
[property: JsonPropertyName("node")] ChannelModsUser Node
);

public record ChannelMods(
[property: JsonPropertyName("edges")] IReadOnlyList<ChannelModsEdge> Edges,
[property: JsonPropertyName("pageInfo")] PageInfo PageInfo
);

public record ChannelModsUser(
[property: JsonPropertyName("id")] string Id,
[property: JsonPropertyName("login")] string Login
);

public record PageInfo(
[property: JsonPropertyName("hasNextPage")] bool HasNextPage
);

public record ChannelModsList(
[property: JsonPropertyName("mods")] ChannelMods Mods
);

public record ChannelModsResponse(
    [property: JsonPropertyName("user")] ChannelModsList User
);
