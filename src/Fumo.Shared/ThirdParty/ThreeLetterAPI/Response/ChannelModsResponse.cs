using Fumo.Shared.ThirdParty.ThreeLetterAPI.Models;

namespace Fumo.Shared.ThirdParty.ThreeLetterAPI.Response;

public record ChannelModsEdge(
    string Cursor,
    object GrantedAt,
    bool IsActive,
    BasicUser Node
);

public record ChannelMods(IReadOnlyList<ChannelModsEdge> Edges, PageInfo PageInfo);

public record ChannelModsUser(string Id, string Login);

public record PageInfo(bool HasNextPage);

public record ChannelModsList(ChannelMods Mods);

public record ChannelModsResponse(ChannelModsList User);
