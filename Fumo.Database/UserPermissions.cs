namespace Fumo.Shared.Enums;

[Flags]
public enum UserPermissions : ulong
{
    Default = 0,

    User = 1 << 0,
    User_ChatError = 1 << 1,

    Admin = 1 << 6,
    Admin_Commands = 1 << 7,
}
