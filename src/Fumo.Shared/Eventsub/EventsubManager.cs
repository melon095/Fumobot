using Autofac;
using Fumo.Shared.Models;
using Fumo.Shared.OAuth;
using Fumo.Shared.ThirdParty.Helix;
using MediatR;
using MiniTwitch.Helix.Enums;
using MiniTwitch.Helix.Models;
using MiniTwitch.Helix.Requests;
using MiniTwitch.Helix.Responses;
using Serilog;
using StackExchange.Redis;
using static MiniTwitch.Helix.Requests.UpdateConduitRequest;

namespace Fumo.Shared.Eventsub;

public class EventsubManager(
    IOAuthRepository oAuthRepository,
    IDatabase redis,
    IHelixFactory helixFactory,
    ILogger logger,
    AppSettings settings,
    ILifetimeScope lifetimeScope,
    IEventsubCommandRegistry eventsubCommandRegistry)
        : IEventsubManager
{
    #region Constants
    private const string ConduitKey = "eventsub:conduit";
    private const string WebhookSecret = "eventsub:conduit:secret";
    private const string ShardKey = "eventsub:conduit:shard";
    #endregion

    #region Dependencies
    private readonly IOAuthRepository OAuthRepository = oAuthRepository;
    private readonly IDatabase Redis = redis;
    private readonly IHelixFactory HelixFactory = helixFactory;
    private readonly ILogger Logger = logger.ForContext<EventsubManager>();
    private readonly AppSettings AppSettings = settings;
    private readonly ILifetimeScope LifetimeScope = lifetimeScope;
    private readonly IEventsubCommandRegistry EventsubCommandRegistry = eventsubCommandRegistry;
    #endregion

    private string? Secret = null;

    public Uri CallbackUrl => new(AppSettings.Website.PublicURL, "/api/Eventsub/Callback");

    #region Private Methods

    private static string CooldownKey(string userId, IEventsubType type) => $"eventsub:cooldown:{userId}:{type.Name}";

    private static string CreateSecret() => Guid.NewGuid().ToString("N");

    private async ValueTask SendSubscriptionCommand<TCondition>(EventsubType<TCondition> request, CreatedSubscription.Info left, CancellationToken ct)
        where TCondition : class
    {
        try
        {
            var commandType = EventsubCommandRegistry.Get((request.Name, EventsubCommandType.Subscribed));
            if (commandType is null) return;

            var instance = Activator.CreateInstance(commandType, left);

            if (instance is IRequest command)
            {
                using var scope = LifetimeScope.BeginLifetimeScope();
                var bus = scope.Resolve<IMediator>();
                await bus.Send(command, ct);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to handle eventsub command for {SubscriptionType}", request.Name);
        }
    }

    private async ValueTask<IEnumerable<EventSubSubscriptions.Subscription>> GetSubscriptions(IEventsubType type, string userId, CancellationToken ct)
    {
        var helix = await HelixFactory.Create(ct);

        var subscriptions = await helix.
            GetEventSubSubscriptions(userId: long.Parse(userId), cancellationToken: ct).
            PaginationHelper<EventSubSubscriptions, EventSubSubscriptions.Subscription>
            ((x) => Logger.Error("Failed to get '{SubscriptionType}' subscriptions for {UserId}: {Error}", type.Name, userId, x.Message), ct);

        return subscriptions.Where(x => x.Type == type.Name);
    }

    #endregion

    #region Public Methods

    public async ValueTask<bool> IsUserEligible(string userId, IEventsubType type, CancellationToken ct)
    {
        var oauth = await OAuthRepository.Get(userId, OAuthProviderName.Twitch, ct);
        if (oauth is null) return false;
        if (oauth.Scopes.Count == 0) return false;

        return type.RequiredScopes.All(oauth.Scopes.Contains);
    }

    public async ValueTask<bool> CheckSubscribeCooldown(string userId, IEventsubType type)
        => await Redis.KeyExistsAsync(CooldownKey(userId, type));

    public async ValueTask SetCooldown(string userId, IEventsubType type)
        => await Redis.StringSetAsync(CooldownKey(userId, type), "1", type.SuccessCooldown, When.Always, CommandFlags.FireAndForget);

    public async ValueTask<bool> Subscribe<TCondition>(EventsubSubscriptionRequest<TCondition> request, CancellationToken ct)
        where TCondition : class
    {
        var log = Logger.ForContext("UserId", request.UserId).ForContext("SubscriptionType", request.Type.Name);

        HelixResult<CreatedSubscription> response;
        try
        {
            log.Information("Subscribing to {SubscriptionType} for {UserId}");

            var helix = await HelixFactory.Create(ct);
            var conduit = await GetConduitID(ct);

            if (conduit is null)
            {
                log.Error("Failed to get conduit ID");
                return false;
            }

            var transport = new NewSubscription.EventsubTransport("conduit", conduitId: conduit);
            var helixRequest = request.Type.ToHelix(transport, request.Condition);

            response = await helix.CreateEventSubSubscription(helixRequest, ct);
            if (!response.Success)
            {
                log.ForContext("Error", response.Message).Error("Failed to subscribe to {SubscriptionType} for {UserId}: {Error}");
                return false;
            }

            log.Information("Successfully subscribed to {SubscriptionType} for {UserId}");

            if (request.Type.ShouldSetCooldown)
                await SetCooldown(request.UserId, request.Type);
        }
        catch (Exception ex)
        {
            log.Error(ex, "Failed to subscribe to {SubscriptionType} for {UserId}");

            return false;
        }

        await SendSubscriptionCommand(request.Type, response.Value.Data[0], ct);

        return true;
    }

    public async ValueTask<bool> Unsubscribe<TCondition>(string userId, EventsubType<TCondition> type, CancellationToken ct)
        where TCondition : class
    {
        var log = Logger.ForContext("UserId", userId).ForContext("SubscriptionType", type.Name);

        try
        {
            var subscription = (await GetSubscriptions(type, userId, ct)).FirstOrDefault();

            if (subscription is null) return false;

            log.Information("Unsubscribing from {SubscriptionType} for {UserId}");

            var helix = await HelixFactory.Create(ct);
            var response = await helix.DeleteEventSubSubscription(subscription.Id, ct);

            if (!response.Success)
            {
                log.ForContext("Error", response.Message).Error("Failed to unsubscribe from {SubscriptionType} for {UserId}: {Error}");
                return false;
            }

            log.Information("Successfully unsubscribed from {SubscriptionType} for {UserId}");
        }
        catch (Exception ex)
        {
            log.Error(ex, "Failed to unsubscribe from {SubscriptionType} for {UserId}");

            return false;
        }

        return true;
    }

    public async ValueTask<bool> IsSubscribed(IEventsubType type, string userId, CancellationToken ct)
        => (await GetSubscriptions(type, userId, ct)).Any(x => x.Status == EventSubStatus.Enabled);

    public async ValueTask<bool> IsSubscribed(IEventsubType type, CancellationToken ct)
    {
        var helix = await HelixFactory.Create(ct);

        var subscriptions = await helix.
            GetEventSubSubscriptions(type: type.Name, cancellationToken: ct).
            PaginationHelper<EventSubSubscriptions, EventSubSubscriptions.Subscription>
            ((x) => Logger.Error("Failed to get '{SubscriptionTypes}' subscriptions: {Error}", type.Name, x.Message), ct);

        return subscriptions.Any(x => x.Status == EventSubStatus.Enabled);
    }

    #region Conduit

    public async ValueTask<string?> GetConduitID(CancellationToken ct)
    {
        var ourConduit = await Redis.StringGetAsync(ConduitKey);
        if (ourConduit.HasValue) return ourConduit;

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

        return conduit.Id;
    }

    public async ValueTask CreateConduit(CancellationToken ct)
    {
        const string shardId = "0";
        const int shardCount = 1;

        var helix = await HelixFactory.Create(ct);
        var secret = CreateSecret();

        var conduits = await helix.CreateConduits(shardCount, ct);
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
            Logger.Warning("Conduit {ConduitId} is in a weird state - {ShardStatus}.", conduit.Id, shard.Status);

        await Redis.StringSetAsync(ConduitKey, conduit.Id);
        await Redis.StringSetAsync(WebhookSecret, secret);
        await Redis.StringSetAsync(ShardKey, shard.Id);

        Logger.Information("Conduit {ConduitId} has been created - {ShardStatus}", conduit.Id, shard.Status);
    }

    #endregion

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

    #endregion
}
