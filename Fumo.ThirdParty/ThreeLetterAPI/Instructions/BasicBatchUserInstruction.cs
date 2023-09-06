using Fumo.ThirdParty.GraphQL;

namespace Fumo.ThirdParty.ThreeLetterAPI.Instructions;

public class BasicBatchUserInstruction : IGraphQLInstruction
{
    public BasicBatchUserInstruction(string[]? ids = null, string[]? logins = null)
    {
        Ids = ids;
        Logins = logins;
    }

    public string[]? Ids { get; }

    public string[]? Logins { get; }

    public GraphQLRequest Create()
    {
        return new()
        {
            Query = @"query($ids: [ID!], $logins: [String!]){users(ids: $ids, logins: $logins){id login}}",
            Variables = new
            {
                ids = Ids,
                logins = Logins
            }
        };
    }
}
