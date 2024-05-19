using System.Security.Claims;
using Fumo.Database.DTO;
using Fumo.Shared.Interfaces;

namespace Fumo.Application.Web.Service;

public class HttpUserService
{
    private readonly IUserRepository UserRepository;
    private readonly IHttpContextAccessor HttpContextAccessor;

    private ClaimsPrincipal User => HttpContextAccessor.HttpContext!.User;

    public HttpUserService(IUserRepository userRepository, IHttpContextAccessor httpContextAccessor)
    {
        UserRepository = userRepository;
        HttpContextAccessor = httpContextAccessor;
    }

    // Any routes utlizing this method must use the [Authorize] attribute
    public async ValueTask<UserDTO> GetUser(CancellationToken ct = default)
    {
        if (!User.Identity?.IsAuthenticated ?? true) throw new UnauthorizedAccessException();

        var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return await UserRepository.SearchID(id!, ct);
    }
}
