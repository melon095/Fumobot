namespace Fumo.ThirdParty.ThreeLetterAPI.Instructions;


/// <summary>
/// id -> String
/// login -> String
/// </summary>
public class BasicUserInstruction : IThreeLetterAPIInstruction
{
    public string? Id { get; }
    public string? Login { get; }

    public BasicUserInstruction(string? id = null, string? login = null)
    {
        Id = id;
        Login = login;
    }

    public ThreeLetterAPIRequest Create()
    {
        return new()
        {
            Query = @"query($id: ID, $login: String){ 
                user(id: $id, login: $login, lookupType: ALL) {
                    id
                    login
                }
            }",
            Variables = new
            {
                id = Id,
                login = Login
            }
        };
    }
}
