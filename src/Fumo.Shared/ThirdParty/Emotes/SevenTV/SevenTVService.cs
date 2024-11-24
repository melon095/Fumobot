using Fumo.Database.DTO;
using Fumo.Database;
using Fumo.Shared.Exceptions;
using Fumo.Shared.ThirdParty.Emotes.SevenTV.Enums;
using Fumo.Shared.ThirdParty.Emotes.SevenTV.Models;
using Fumo.Shared.ThirdParty.GraphQL;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fumo.Database.Extensions;
using Fumo.Shared.Models;
using Fumo.Shared.ThirdParty.Exceptions;

namespace Fumo.Shared.ThirdParty.Emotes.SevenTV;

public class SevenTVService : AbstractGraphQLClient, ISevenTVService
{
    private readonly IDatabase Redis;
    private readonly string BotID;

    protected override Uri URI { get; } = new("https://7tv.io/v3/gql");

    public SevenTVService(IDatabase redis, AppSettings settings)
    {
        var token = settings.SevenTV.Bearer;

        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Bearer token is missing from configuration");

        HttpClient.DefaultRequestHeaders.Authorization = new("Bearer", token);

        Redis = redis;
        BotID = settings.Twitch.UserID;
    }

    public static string EditorKey(string channelID) => $"channel:{channelID}:seventv:editors";

    public async ValueTask<SevenTVPermissionCheckResult> EnsureCanModify(ChannelDTO channel, UserDTO invoker)
    {
        var currentEmoteSet = channel.GetSetting(ChannelSettingKey.SevenTV_EmoteSet)
            ?? throw new InvalidInputException("The channel is missing an emote set");

        var sevenTVId = channel.GetSetting(ChannelSettingKey.SevenTV_UserID)
            ?? throw new InvalidInputException("The channel is missing a 7TV user ID");

        SevenTVPermissionCheckResult result = new(currentEmoteSet, sevenTVId);

        RedisValue[] redisValues = [new RedisValue(BotID), new RedisValue(invoker.TwitchID)];
        var contains = await Redis.SetContainsAsync(EditorKey(channel.TwitchID), redisValues);

        // Bot is not editor
        if (contains[0] == false)
        {
            throw new InvalidInputException("I am not an editor in this channel");
        }

        // Is broadcaster
        if (channel.TwitchID == invoker.TwitchID)
        {
            return result;
        }

        // Invoker is editor
        if (contains[1] == false)
        {
            throw new InvalidInputException("You're not an editor in this channel");
        }

        return result;
    }

    #region Requests

    public async ValueTask<SevenTVRoles> GetGlobalRoles(CancellationToken ct = default)
    {
        GraphQLRequest request = new()
        {
            Query = @"query GetRoles{roles {name id}}"
        };

        return await Send<SevenTVRoles>(request, ct);
    }

    public async ValueTask<SevenTVUser> GetUserInfo(string twitchID, CancellationToken ct = default)
    {
        GraphQLRequest request = new()
        {
            Query = @"query GetUserByConnection($platform: ConnectionPlatform! $id: String!) {userByConnection (platform: $platform, id: $id) {id type username roles created_at connections{id platform emote_set_id}emote_sets{id emotes{id} capacity}}}",
            Variables = new
            {
                platform = "TWITCH",
                id = twitchID
            }
        };

        return (await Send<OuterSevenTVUser>(request, ct)).UserByConnection;
    }

    public async ValueTask<SevenTVEditorEmoteSets> GetEditorEmoteSetsOfUser(string twitchID, CancellationToken ct = default)
    {
        GraphQLRequest request = new()
        {
            Query = @"query GetUserByConnection($platform: ConnectionPlatform!, $id: String!) {userByConnection(platform: $platform, id: $id){id username connections{id platform emote_set_id}editor_of{id user{username connections{id platform emote_set_id}}}}}",
            Variables = new
            {
                platform = "TWITCH",
                id = twitchID
            }
        };

        return (await Send<SevenTVEditorEmoteSetsRoot>(request, ct)).UserByConnection;
    }

    public async ValueTask<SevenTVEditors> GetEditors(string twitchID, CancellationToken ct = default)
    {
        GraphQLRequest request = new()
        {
            Query = @"query GetUserByConnection($platform: ConnectionPlatform!, $id: String!) {userByConnection(platform: $platform, id: $id) {id username editors{id user{username connections{platform id emote_set_id}}}}}",
            Variables = new
            {
                platform = "TWITCH",
                id = twitchID
            }
        };

        return (await Send<SevenTVEditorsRoot>(request, ct)).UserByConnection;
    }

    public async ValueTask<SevenTVBasicEmote> SearchEmoteByID(string Id, CancellationToken ct)
    {
        GraphQLRequest request = new()
        {
            Query = @"query SearchEmote($id: ObjectID!){emote(id: $id){id name}}",
            Variables = new
            {
                id = Id
            }
        };

        return (await Send<EmoteRoot>(request, ct)).Emote;
    }

    public async ValueTask<SevenTVEmoteByName> SearchEmotesByName(string name, bool exact = false, CancellationToken ct = default)
    {
        GraphQLRequest request = new()
        {
            Query = @"query SearchEmotes($query: String! $page: Int $limit: Int $filter: EmoteSearchFilter) {emotes(query: $query page: $page limit: $limit filter: $filter){items{id name owner{username id} tags}}}",
            Variables = new
            {
                query = name,
                page = 1,
                limit = 100,
                filter = new
                {
                    exact_match = exact,
                    ignore_tags = !exact
                }
            }
        };

        try
        {
            return (await Send<SevenTVEmoteByNameRoot>(request, ct)).Emotes;
        }
        catch (GraphQLException e)
        {
            if (SevenTVErrorMapper.TryErrorCodeFromGQL(e, out var ec))
            {
                if (ec == SevenTVErrorCode.SearchNoResult) return new([]);
            }

            throw;
        }
    }

    public async ValueTask<string?> ModifyEmoteSet(string emoteSet, ListItemAction action, string emoteID, string? name = null, CancellationToken ct = default)
    {
        var stringAction = action switch
        {
            ListItemAction.Add => "ADD",
            ListItemAction.Remove => "REMOVE",
            ListItemAction.Update => "UPDATE",
            _ => throw new NotImplementedException()
        };

        GraphQLRequest request = new()
        {
            Query = @"mutation ChangeEmoteInSet($id: ObjectID! $action: ListItemAction! $emote_id: ObjectID! $name: String) {emoteSet(id: $id){id emotes(id: $emote_id action: $action name: $name){id name}}}",
            Variables = new
            {
                id = emoteSet,
                action = stringAction,
                emote_id = emoteID,
                name,
            }
        };

        var response = await Send<SevenTVModifyEmoteSetRoot>(request, ct);

        return response.EmoteSet.Emotes.LastOrDefault(x => x.ID == emoteID)?.Name ?? default;
    }

    public async ValueTask<List<SevenTVEnabledEmote>> GetEnabledEmotes(string emoteSet, CancellationToken ct = default)
    {
        GraphQLRequest request = new()
        {
            Query = @"query GetEmoteSet($id: ObjectID!){emoteSet(id: $id){id name emotes{id name data{name}}}}",
            Variables = new
            {
                id = emoteSet
            }
        };

        var response = await Send<JsonDocument>(request, ct);

        return response
            .RootElement
            .GetProperty("emoteSet")
            .GetProperty("emotes")
            .Deserialize<List<SevenTVEnabledEmote>>(SerializerOptions) ?? [];
    }

    public async ValueTask ModifyEditorPermissions(string channelId, string userId, UserEditorPermissions permissions, CancellationToken ct = default)
    {
        GraphQLRequest request = new()
        {
            Query = @"mutation UpdateUserEditors($id: ObjectID! $editor_id: ObjectID! $d: UserEditorUpdate!){user(id: $id){editors(editor_id: $editor_id data: $d){id}}}",
            Variables = new
            {
                id = channelId,
                editor_id = userId,
                d = new
                {
                    permissions
                }
            }
        };

        await Send<JsonDocument>(request, ct);
    }

    #endregion
}

file record EmoteRoot(SevenTVBasicEmote Emote);

file record SevenTVEditorsRoot(SevenTVEditors UserByConnection);

file record SevenTVEditorEmoteSetsRoot(SevenTVEditorEmoteSets UserByConnection);

file record SevenTVEmoteByNameRoot(SevenTVEmoteByName Emotes);

file record SevenTVModifyEmoteSetRoot(SevenTVModifyEmoteSet EmoteSet);

file record SevenTVModifyEmoteSet(string ID, IReadOnlyList<SevenTVBasicEmote> Emotes);
