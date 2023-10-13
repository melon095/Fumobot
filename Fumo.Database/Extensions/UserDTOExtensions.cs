using Fumo.Database.DTO;

namespace Fumo.Database.Extensions;

public static class UserDTOExtensions
{
    public static bool HasPermission(this UserDTO user, string input)
    {
        if (user == null)
        {
            return false;
        }

        if (user.Permissions == null)
        {
            return false;
        }

        var requiredParts = input.Split('.');

        foreach (var permission in user.Permissions)
        {
            var userParts = permission.Split('.');

            if (userParts.Length != requiredParts.Length)
            {
                continue;
            }

            // admin.foo -> admin.*
            // admin.foo -> admin.foo
            if (userParts[0] == requiredParts[0] && (userParts[1] == requiredParts[1] || userParts[1] == "*"))
            {
                return true;
            }
        }

        return false;
    }
}
