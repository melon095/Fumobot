namespace Fumo.Shared.Enums;

[Flags]
public enum ChatCommandFlags : long
{
    None = 0,
    IgnoreBanphrase = (1L << 1),
    Reply = (1L << 2),
    OnlyOffline = (1L << 3),
    ModeratorOnly = (1L << 4),
    BroadcasterOnly = (1L << 5),
}