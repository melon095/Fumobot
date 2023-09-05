using Fumo.Database.DTO;
using System.Text.RegularExpressions;

namespace Fumo.Database.Extensions;

public static class UserDTOExtensions
{
    public static bool IsAdmin(this UserDTO user)
        => user.MatchesPermission("admin(\\.)?.*");

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
            if (Regex.IsMatch(input, permission))
            {
                return true;
            }
        }

        return false;
    }
}
