using Fumo.Shared.ThirdParty.GraphQL;

namespace Fumo.Shared.ThirdParty.ThreeLetterAPI.Instructions;

public class BasicUserInstruction : IGraphQLInstruction
{
    public string? Id { get; }
    public string? Login { get; }

    public BasicUserInstruction(string? id = null, string? login = null)
    {
        Id = id;
        Login = login;
    }

    public GraphQLRequest Create()
    {
        return new()
        {
            Query = @"query($id: ID, $login: String){user(id: $id, login: $login, lookupType: ALL) {id login}}",
            Variables = new
            {
                id = Id,
                login = Login
            }
        };
    }
}
