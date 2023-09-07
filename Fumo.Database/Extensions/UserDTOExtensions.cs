using Fumo.Database.DTO;
using System.Text.RegularExpressions;

namespace Fumo.Database.Extensions;

public static class UserDTOExtensions
{
    public static bool MatchesPermission(this UserDTO user, string input)
    {
        if (user == null)
        {
            return false;
        }

        if (user.Permissions == null)
        {
            return false;
        }

        foreach (var permission in user.Permissions)
        {
            if (Regex.IsMatch(input, permission, RegexOptions.Compiled))
            {
                return true;
            }
        }

        return false;
    }
}
