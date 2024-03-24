using System.Text.RegularExpressions;

namespace Fumo.Shared.Regexes;

public static partial class UsernameCleanerRegex
{
    [GeneratedRegex(
        @"[@#]",
        RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 100)]
    private static partial Regex Username();
    public static string CleanUsername(string username) => Username().Replace(username.ToLower(), "");
}
