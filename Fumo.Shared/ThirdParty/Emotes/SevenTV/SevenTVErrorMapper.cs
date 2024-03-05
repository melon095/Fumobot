using Fumo.Shared.ThirdParty.Exceptions;

namespace Fumo.Shared.ThirdParty.Emotes.SevenTV;

public static class SevenTVErrorMapper
{
    public static bool TryErrorCodeFromGQL(GraphQLException ex, out SevenTVErrorCode errorCode)
    {
        errorCode = SevenTVErrorCode.Unknown;
        var split = ex.Message.Split(' ');

        if (split.Length < 1)
        {
            return false;
        }
        
        if (Enum.TryParse(split[0], out errorCode))
        {
            return true;
        }

        return false;
    }
}
