using System.Security.Claims;
using AspNetCoreRateLimit;

namespace Fumo.Application.Web;

public class ClientResolveContributor : IClientResolveContributor
{
    public Task<string> ResolveClientAsync(HttpContext httpContext)
        => (httpContext.User.Identity?.IsAuthenticated ?? false) switch
        {
            true => Task.FromResult(httpContext.User.FindFirst(ClaimTypes.Name)!.Value),
            false => Task.FromResult(httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown")
        };
}
