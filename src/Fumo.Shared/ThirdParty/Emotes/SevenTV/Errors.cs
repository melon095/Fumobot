using System.Runtime.CompilerServices;
using Fumo.Shared.Exceptions;

namespace Fumo.Shared.ThirdParty.Emotes.SevenTV;

public static class SevenTVErrors
{
    #region Server Errors
    public const string AddEmoteNameConflict = "BAD_REQUEST this emote has a conflicting name";
    public const string UpdateAliasNameConflict = "BAD_REQUEST emote name conflict";
    public const string LackingPrivileges = "LACKING_PRIVILEGES";
    #endregion

    #region Library Exceptions
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static InvalidInputException NotLinkedToTwitch() => new("user is not linked to Twitch");
    #endregion
}
