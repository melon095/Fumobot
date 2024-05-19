using System.Security.Cryptography;
using System.Text;
using Fumo.Shared.Models;
using Fumo.Shared.OAuth;
using Fumo.Shared.ThirdParty.Helix;
using MiniTwitch.Helix.Enums;
using MiniTwitch.Helix.Requests;
using MiniTwitch.Helix.Responses;
using Serilog;
using StackExchange.Redis;
using static MiniTwitch.Helix.Requests.UpdateConduitRequest;

namespace Fumo.Shared.Eventsub;

public class EventsubManager : IEventsubManager
{
    private const string ConduitKey = "eventsub:conduit";
    private const string WebhookSecret = "eventsub:conduit:secret";
    private const string ShardKey = "eventsub:conduit:shard";

    private readonly IOAuthRepository OAuthRepository;
    private readonly IDatabase Redis;
    private readonly IHelixFactory HelixFactory;
    private readonly ILogger Logger;
    private readonly AppSettings AppSettings;

    private string? Secret = null;

    public EventsubManager(IOAuthRepository oAuthRepository, IDatabase redis, IHelixFactory helixFactory, ILogger logger, AppSettings settings)
    {
        OAuthRepository = oAuthRepository;
        Redis = redis;
        HelixFactory = helixFactory;
        Logger = logger.ForContext<EventsubManager>();
        AppSettings = settings;
    }

    private static string CooldownKey(string userId, EventsubType type) => $"eventsub:cooldown:{userId}:{type.Name}";
    private static string CreateSecret() => Guid.NewGuid().ToString("N");

    public Uri CallbackUrl => new(new Uri(AppSettings.Website.PublicURL), "/api/Eventsub/Callback");

    public async ValueTask<bool> IsUserEligible(string userId, EventsubType type, CancellationToken ct)
    {
        var oauth = await OAuthRepository.Get(userId, OAuthProviderName.Twitch, ct);
        if (oauth is null) return false;
        if (oauth.Scopes.Count == 0) return false;

        return type.RequiredScopes.All(oauth.Scopes.Contains);
    }

    public async ValueTask<bool> CheckSubscribeCooldown(string userId, EventsubType type)
    {
        var key = CooldownKey(userId, type);

        return await Redis.KeyExistsAsync(key);
    }

    public async ValueTask Subscribe(string userId, EventsubType type, CancellationToken ct)
    {
        var helix = await HelixFactory.Create(ct);

        // TODO: Ensure conduit is created

    }

    public async ValueTask<Conduits.Conduit?> GetConduit(CancellationToken ct)
    {
        var ourConduit = await Redis.StringGetAsync(ConduitKey);
        var ourShard = await Redis.StringGetAsync(ShardKey);

        var helix = await HelixFactory.Create(ct);

        var conduits = await helix.GetConduits(ct);
        if (!conduits.Success)
        {
            Logger.Error("Failed to get conduits: {Error}", conduits.Message);

            return null;
        }

        var conduit = conduits.Value.Data.Where(c => c.Id == ourConduit).FirstOrDefault();

        if (conduit is null)
            return null;

        var shards = await helix.GetConduitShards(conduit.Id, cancellationToken: ct);
        if (!shards.Success)
        {
            Logger.Error("Failed to get shards for conduit {ConduitId}: {Error}", conduit.Id, shards.Message);

            return null;
        }

        var shard = shards
            .Value
            .Data
            .Where(s => s.Id == ourShard && s.Transport is ConduitTransport.Webhook)
            .FirstOrDefault();

        if (shard is null)
            return null;

        if (shard.Status != ConduitShardStatus.Enabled)
        {
            Logger.Error("Shard {ShardId} assigned to conduit {ConduitId} is not enabled {Status}", shard.Id, conduit.Id, shard.Status);

            return null;
        }

        return conduit;
    }

    public async ValueTask CreateConduit(CancellationToken ct)
    {
        const string shardId = "0";

        var helix = await HelixFactory.Create(ct);
        var secret = CreateSecret();

        var conduits = await helix.CreateConduits(1, ct);
        if (!conduits.Success)
        {
            Logger.Error("Failed to create conduit: {Error}", conduits.Message);

            return;
        }

        var conduit = conduits.Value.Data[0];
        var transport = new ShardTransport("webhook", secret: secret, callbackUrl: CallbackUrl.ToString());
        var request = new UpdateConduitRequest(shardId, transport);

        var shardStatus = await helix.UpdateConduitShards(conduit.Id, request, ct);
        if (!shardStatus.Success || shardStatus.Value.Errors.Count > 0)
        {
            var errors = string.Join(", ", shardStatus.Value.Errors.Select(x => x.Message));

            Logger.Error("Failed to update shard for conduit {ConduitId}: {ShardError}", conduit.Id, errors);

            return;
        }

        var shard = shardStatus.Value.Data.Where(x => x.Id == shardId).First();
        if (shard.Status != ConduitShardStatus.Enabled || shard.Status != ConduitShardStatus.WebhookCallbackVerificationPending)
            Logger.Warning("Shard {ShardId} is in a weird state {ShardStatus}.", shard.Id, shard.Status);

        await Redis.StringSetAsync(ConduitKey, conduit.Id);
        await Redis.StringSetAsync(WebhookSecret, secret);
        await Redis.StringSetAsync(ShardKey, shard.Id);

        Logger.Information("Conduit {ConduitId} has been created with shard {ShardId} {ShardStatus}", conduit.Id, shard.Id, shard.Status);
    }

    public async ValueTask<string> GetSecret()
    {
        if (Secret is not null) return Secret;

        var secret = await Redis.StringGetAsync(WebhookSecret);

        if (secret.HasValue)
        {
            Secret = secret;
            return Secret!;
        }

        var newSecret = CreateSecret();
        await Redis.StringSetAsync(WebhookSecret, newSecret);

        Secret = newSecret;

        return newSecret;
    }

    public async ValueTask<bool> CheckSignature(string message, string signature)
    {
        var secret = await GetSecret();
        var messageBytes = Encoding.UTF8.GetBytes(message);
        var signatureBytes = Encoding.UTF8.GetBytes(signature);

        using HMACSHA256 hmacGen = new(Encoding.UTF8.GetBytes(secret));
        var computedHash = hmacGen.ComputeHash(messageBytes);
        var finalHmac = $"sha256={BitConverter.ToString(computedHash).Replace("-", "").ToLower()}";
        var finalBytes = Encoding.UTF8.GetBytes(finalHmac);

        return CryptographicOperations.FixedTimeEquals(finalBytes, signatureBytes);
    }

    public async ValueTask HandleMessage(MessageTypeRevocationBody message, CancellationToken ct)
    {
        Logger.Information("Revoked {SubscriptionType} subscription", message.Subscription.Type);
    }

    public async ValueTask HandleMessage(MessageTypeNotificationBody message, CancellationToken ct)
    {
        Logger.Information("Received notification for {SubscriptionType}", message.Subscription.Type);
    }
}
