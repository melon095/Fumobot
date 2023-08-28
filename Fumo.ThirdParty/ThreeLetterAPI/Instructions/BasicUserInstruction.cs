namespace Fumo.ThirdParty.ThreeLetterAPI.Instructions;


/// <summary>
/// id -> String
/// login -> String
/// </summary>
public class BasicUserInstruction : IThreeLetterAPIInstruction
{
    public ThreeLetterAPIRequest Create(object variables)
    {
        return new()
        {
            Query = @"query($id: ID, $login: String){ 
                user(id: $id, login: $login, lookupType: ALL) {
                    id
                    login
                }
            }",
            Variables = variables
        };
    }
}
