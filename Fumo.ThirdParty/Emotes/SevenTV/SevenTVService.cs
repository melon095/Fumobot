using Fumo.ThirdParty.GraphQL;
using Microsoft.Extensions.Configuration;

namespace Fumo.ThirdParty.Emotes.SevenTV;

public class SevenTVService : AbstractGraphQLClient, ISevenTVService
{
    public SevenTVService(IConfiguration config)
        : base("https://7tv.io/v3/gql")
    {
        var token = config["SevenTV:Bearer"];

        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Bearer token is missing from configuration", nameof(config));

        HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
    }

    public async Task<SevenTVRoles> GetGlobalRoles(CancellationToken ct = default)
    {
        GraphQLRequest request = new()
        {
            Query = @"query GetRoles{roles {name id}}"
        };

        return await SendAsync<SevenTVRoles>(request, ct);
    }

    public async Task<SevenTVUser> GetUserInfo(string twitchID, CancellationToken ct = default)
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

        return (await SendAsync<OuterSevenTVUser>(request, ct)).UserByConnection;
    }
}
