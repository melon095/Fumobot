namespace Fumo.ThirdParty.ThreeLetterAPI.Instructions;

public class UserByIDInstruction : IThreeLetterAPIInstruction
{
    public string Instruction
        => @"query($id: ID){ 
                user(id: $id, lookupType: ALL) {
                    id
                    login
                }
            }";
}
