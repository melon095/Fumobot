
using StackExchange.Redis;

namespace Fumo.Application.Startable;

public class ConduitStarter : IAsyncStartable
{
    private readonly IDatabase Redis;

    public ConduitStarter(IDatabase redis)
    {
        Redis = redis;
    }

    public async ValueTask Start(CancellationToken ct)
    {
        // Check for Token
        var tokenExists = await Redis.KeyExistsAsync("app_token");

        if (!tokenExists) await UpdateAppToken();

        var token = await Redis.StringGetAsync("app_token");


    }

    private async ValueTask UpdateAppToken()
    {

    }
}
