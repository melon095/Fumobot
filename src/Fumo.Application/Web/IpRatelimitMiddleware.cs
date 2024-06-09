
using AspNetCoreRateLimit;
using Microsoft.Extensions.Options;

namespace Fumo.Web;

public class IpRateLimitMiddleware : RateLimitMiddleware<IpRateLimitProcessor>
{
    public IpRateLimitMiddleware(RequestDelegate next,
        IProcessingStrategy processingStrategy,
        IOptions<IpRateLimitOptions> options,
        IIpPolicyStore policyStore,
        IRateLimitConfiguration config
    )
        : base(next, options?.Value, new IpRateLimitProcessor(options?.Value, policyStore, processingStrategy), config)
    {
    }

    protected override void LogBlockedRequest(HttpContext httpContext, ClientRequestIdentity identity, RateLimitCounter counter, RateLimitRule rule)
    {
        // DONT LOGGING BLOCKED REQUESTS
    }
}