namespace Fumo.Shared.ThirdParty.Emotes.SevenTV.Models;

public record SevenTVRole(string ID, string Name);

public record SevenTVRoles(IReadOnlyList<SevenTVRole> Roles);