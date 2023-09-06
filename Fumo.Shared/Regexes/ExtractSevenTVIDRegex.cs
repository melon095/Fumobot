using System.Text.RegularExpressions;

namespace Fumo.Shared.Regexes;

public static partial class ExtractSevenTVIDRegex
{
    [GeneratedRegex(
        @"\b6\d[a-f0-9]{22}\b",
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
