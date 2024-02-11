using Fumo.Shared.ThirdParty.Exceptions;

namespace Fumo.Shared.ThirdParty.Emotes.SevenTV;

public static class SevenTVErrorMapper
{
    public const int ErrorEmoteAlreadyEnabled = 704611;

    public static bool TryErrorCodeFromGQL(GraphQLException ex, out int errorCode)
    {
        errorCode = 0;
        var split = ex.Message.Split(' ');

        if (split.Length < 1)
        {
            return false;
        }

        if (int.TryParse(split[0], out errorCode))
        {
            return true;
        }

        return false;
    }
}
