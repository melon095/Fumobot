using Fumo.Database.DTO;
using Fumo.Database;
using Fumo.Shared.Exceptions;
using Fumo.Shared.ThirdParty.Emotes.SevenTV.Models;
using Fumo.Shared.ThirdParty.GraphQL;
using StackExchange.Redis;
using System.Text.Json;
using Fumo.Database.Extensions;
using Fumo.Shared.Models;
using Serilog;
using SerilogTracing;
using Serilog.Context;
using System.Collections.Immutable;

namespace Fumo.Shared.ThirdParty.Emotes.SevenTV;

public class SevenTVService : AbstractGraphQLClient, ISevenTVService
{
    private readonly IDatabase Redis;
    private readonly string BotID;
    private readonly ILogger Logger;

    protected override string Name => "SevenTV";

    public SevenTVService(IHttpClientFactory factory, IDatabase redis, AppSettings settings, ILogger logger)
        : base(factory)
    {
        Redis = redis;
        BotID = settings.Twitch.UserID;
        Logger = logger.ForContext<SevenTVService>();
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

    public async ValueTask<SevenTVUser> GetUserInfo(string twitchID, CancellationToken ct = default)
    {
        using var enrich = LogContext.PushProperty("RequestedTwitchID", twitchID);
        using var activity = Logger.StartActivity("SevenTVService.GetUserInfo");

        GraphQLRequest request = new()
        {
            Query = @"
            query ($id: String!) {
              users {
                userByConnection(platform: TWITCH, platformId: $id) {
                  id
                  roles {
                    name
                  }
                  connections {
                    platform
                    platformUsername
                    platformId
                  }
                  style {
                    activeEmoteSet {
                      id
                      emotes {
                        items {
                          id
                          alias
                        }
                      }
                      capacity
                    }
                  }
                }
              }
            }",
            Variables = new
            {
                platform = "TWITCH",
                id = twitchID
            }
        };

        return await Send<SevenTVUser>(request, ct);
    }

    public async ValueTask<SevenTVBotEditors> GetEditorEmoteSetsOfUser(string twitchID, CancellationToken ct = default)
    {
        using var enrich = LogContext.PushProperty("RequestedTwitchID", twitchID);
        using var activity = Logger.StartActivity("SevenTVService.GetEditorEmoteSetsOfUser");

        GraphQLRequest request = new()
        {
            Query = @"
            query($id: String!) {
              users {
                userByConnection(platform: TWITCH, platformId: $id) {
                  editorFor {
                    user {
                      connections {
                        platformId
                        platform
                      }
                      editors {
                        editor {
                          connections {
                            platform
                            platformId 
                          }
                        }
                      }
                    }
                  }
                }
              }
            }",
            Variables = new
            {
                platform = "TWITCH",
                id = twitchID
            }
        };

        return await Send<SevenTVBotEditors>(request, ct);
    }

    public async ValueTask<SevenTVBasicEmote?> SearchEmoteByID(string Id, CancellationToken ct)
    {
        using var enrich = LogContext.PushProperty("RequestedEmoteID", Id);
        using var activity = Logger.StartActivity("SevenTVService.SearchEmoteByID");

        GraphQLRequest request = new()
        {
            Query = @"query ($id: Id!) {
              emotes {
                emote(id: $id) {
                  id
                  defaultName
                }
              }
            }",
            Variables = new
            {
                id = Id
            }
        };

        return await Send<SevenTVBasicEmote>(request, ct);
    }

    public async ValueTask<SevenTVEmoteByName> SearchEmotesByName(string name, bool exact = false, CancellationToken ct = default)
    {
        using var enrich = Logger.PushProperties(("RequestedEmoteName", name), ("ExactMatch", exact));
        using var activity = Logger.StartActivity("SevenTVService.SearchEmotesByName");

        GraphQLRequest request = new()
        {
            Query = @"query ($query: String!, $exact: Boolean) {
              emotes {
                search(
                  query: $query
                  sort: {sortBy: TOP_ALL_TIME, order: DESCENDING}
                  perPage: 100
                  filters: {exactMatch: $exact}
                ) {
                  items {
                    id
                    defaultName
                    owner {
                      connections {
                        platform
                        platformUsername
                        platformId
                      }
                      id
                    }
                    tags
                  }
                }
              }
            }",
            Variables = new
            {
                query = name,
                exact,
            }
        };

        return await Send<SevenTVEmoteByName>(request, ct);
    }

    public async ValueTask<string?> AddEmote(string setID, string emoteID, string? alias = null, CancellationToken ct = default)
    {
        using var enrich = Logger.PushProperties(("SetID", setID), ("EmoteID", emoteID), ("Alias", alias));
        using var activity = Logger.StartActivity("SevenTVService.AddEmote");

        GraphQLRequest request = new()
        {
            Query = @"
            mutation ($setId: Id!, $emoteId: Id!, $alias: String) {
              emoteSets {
                emoteSet(id: $setId) {
                  addEmote(id: {emoteId: $emoteId, alias: $alias}) {
                    id
                    emotes {
                      items {
                        id
                        alias
                      }
                    }
                  }
                }
              }
            }",
            Variables = new
            {
                setId = setID,
                emoteId = emoteID,
                alias
            }
        };

        var response = await Send<JsonDocument>(request, ct);

        return response
            .RootElement
            .GetProperty("emoteSets")
            .GetProperty("emoteSet")
            .GetProperty("addEmote")
            .GetProperty("emotes")
            .GetProperty("items")
            .EnumerateArray()
            .Where(x => x.GetProperty("id").GetString() == emoteID)
            .Select(x => x.GetProperty("alias").GetString())
            .FirstOrDefault();
    }

    public async ValueTask RemoveEmote(string setID, SevenTVBasicEmote emote, CancellationToken ct = default)
    {
        using var enrich = Logger.PushProperties(("SetID", setID), ("EmoteID", emote.ID), ("Alias", emote.Name));
        using var activity = Logger.StartActivity("SevenTVService.RemoveEmote");

        GraphQLRequest request = new()
        {
            Query = @"
            mutation ($setId: Id!, $emoteId: Id!, $alias: String) {
              emoteSets {
                emoteSet(id: $setId) {
                  removeEmote(id: {emoteId: $emoteId, alias: $alias}) {
                    __typename
                  }
                }
              }
            }",
            Variables = new
            {
                setId = setID,
                emoteId = emote.ID,
                alias = emote.Name
            }
        };

        await Send<JsonElement>(request, ct);
    }

    public async ValueTask AliasEmote(string setID, SevenTVBasicEmote emote, string newName, CancellationToken ct = default)
    {
        using var enrich = Logger.PushProperties(("SetID", setID), ("EmoteID", emote.ID), ("Name", newName));
        using var activity = Logger.StartActivity("SevenTVService.AliasEmote");

        GraphQLRequest request = new()
        {
            Query = @"
            mutation($setId: Id!, $emoteId: Id!, $origAlias: String, $newAlias: String!) {
                emoteSets {
                    emoteSet(id: $setId) {
                        updateEmoteAlias(id: { emoteId: $emoteId, alias: $origAlias}, alias: $newAlias) {
                            id
                            alias
                        }
                    }
                }
            }",
            Variables = new
            {
                setId = setID,
                emoteId = emote.ID,
                origAlias = emote.Name,
                newAlias = newName
            }
        };

        await Send<JsonElement>(request, ct);
    }

    public async ValueTask<IImmutableList<SevenTVBasicEmote>> GetEnabledEmotes(string emoteSet, CancellationToken ct = default)
    {
        using var enrich = LogContext.PushProperty("EmoteSet", emoteSet);
        using var activity = Logger.StartActivity("SevenTVService.GetEnabledEmotes");

        GraphQLRequest request = new()
        {
            Query = @"
            query ($id: Id!) {
              emoteSets {
                emoteSet(id: $id) {
                  id
                  name
                  emotes {
                    items {
                      id
                      alias
                    }
                  }
                }
              }
            }",
            Variables = new
            {
                id = emoteSet
            }
        };

        var response = await Send<JsonDocument>(request, ct);

        return response
            .RootElement
            .Deserialize<IImmutableList<SevenTVBasicEmote>>(SerializerOptions) ?? [];
    }

    public async ValueTask RemoveEditor(string channelId, string editorId, CancellationToken ct = default)
    {
        using var enrich = Logger.PushProperties(("ChannelID", channelId), ("EditorID", editorId));
        using var activity = Logger.StartActivity("SevenTVService.RemoveEditor");

        GraphQLRequest request = new()
        {
            Query = @"
            mutation ($userId: Id!, $editorId: Id!) {
              userEditors {
                editor(userId: $userId, editorId: $editorId) {
                  delete
                }
              }
            }",
            Variables = new
            {
                userId = channelId,
                editorId
            }
        };

        await Send<JsonElement>(request, ct);
    }

    public async ValueTask AddEditor(string channelId, string editorId, CancellationToken ct = default)
    {
        using var enrich = Logger.PushProperties(("ChannelID", channelId), ("EditorID", editorId));
        using var activity = Logger.StartActivity("SevenTVService.AddEditor");

        GraphQLRequest request = new()
        {
            Query = @"
            mutation ($userId: Id!, $editorId: Id!) {
              userEditors {
                create(
                  userId: $userId
                  editorId: $editorId
                  permissions: {
                    superAdmin: false, 
                    emote: {
                        admin: false, 
                        manage: false, 
                        create: false, 
                        transfer: false
                    }, 
                    user: {
                        admin: false, 
                        manageBilling: false, 
                        manageProfile: false, 
                        manageEditors: false, 
                        managePersonalEmoteSet: false
                    }, 
                    emoteSet: {
                        manage: true, 
                        admin: false, 
                        create: false
                    }}) {
                  __typename
                }
              }
            }",
            Variables = new
            {
                userId = channelId,
                editorId
            }
        };

        await Send<JsonElement>(request, ct);
    }

    #endregion
}
