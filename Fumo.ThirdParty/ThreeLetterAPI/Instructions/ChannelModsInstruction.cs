using Fumo.ThirdParty.GraphQL;

namespace Fumo.ThirdParty.ThreeLetterAPI.Instructions;

public class ChannelModsInstruction : IGraphQLInstruction
{
    public string? Id { get; }
    public string? Login { get; }
    public string? Cursor { get; }

    public ChannelModsInstruction(string? id = null, string? login = null, string? cursor = null)
    {
        Id = id;
        Login = login;
        Cursor = cursor;
    }
    public GraphQLRequest Create()
    {
        return new()
        {
            Query = @"query($id: ID, $login: String, $cursor: Cursor) {user(id: $id, login: $login, lookupType: ALL) {mods(first: 100, after: $cursor) {edges {cursor grantedAt isActive node {id login}}pageInfo {hasNextPage}}}}",
            Variables = new
            {
                id = Id,
                login = Login,
                cursor = Cursor,
            },
        };
    }
}
