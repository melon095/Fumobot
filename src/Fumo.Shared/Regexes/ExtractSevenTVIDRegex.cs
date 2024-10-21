using System.Text.RegularExpressions;

namespace Fumo.Shared.Regexes;

public static partial class ExtractSevenTVIDRegex
{
    [GeneratedRegex(
       @"\b(?:6\d[a-f\d]{22}|[0-7][\dA-HJKMNP-TV-Z]{25})\b",
        RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 100
        )]
    private static partial Regex Yeah();
    public static string? Extract(string input)
    {
        var match = Yeah().Match(input);

        if (match.Success)
        {
            return match.Value;
        }
        else
        {
            return null;
        }
    }
}
