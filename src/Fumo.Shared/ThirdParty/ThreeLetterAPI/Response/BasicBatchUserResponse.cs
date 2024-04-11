using Fumo.Shared.ThirdParty.ThreeLetterAPI.Models;

namespace Fumo.Shared.ThirdParty.ThreeLetterAPI.Response;

public record BasicBatchUserResponse(IReadOnlyList<BasicUser> Users);

