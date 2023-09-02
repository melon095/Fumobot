using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace Fumo.Shared.Regexes;

public static partial class BanphraseRegex
{
    public static readonly ReadOnlyCollection<Func<string, bool>> GlobalRegex = new(new List<Func<string, bool>>
    {
        Racism1,
        Racism2,
        Racism3,
        Racism4,

        Underage,
    });

    [GeneratedRegex(
        @"(?:(?:\\b(?<![-=\\.])(?<!\\.com\\/)|monka)(?:[Nn\\x{00F1}]|[Ii7]V)|\\/\\\\\\/)[\\s\\.]*?[liI1y!j\\/]+[\\s\\.]*?(?:[GgbB6934Q🅱qğĜƃ၅5\\*][\\s\\.]*?){2,}(?!arcS|l|Ktlw|ylul|ie217|64|\\d? ?times)\\\\?",
        RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex Racism1_();
    public static bool Racism1(string a) => Racism1_().IsMatch(a);

    [GeneratedRegex(
        @"(?<!monte)negr[o|u]s?(?<!ni)",
        RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex Racism2_();
    public static bool Racism2(string a) => Racism2_().IsMatch(a);

    [GeneratedRegex(
        @"knee grow",
        RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex Racism3_();
    public static bool Racism3(string a) => Racism3_().IsMatch(a);

    [GeneratedRegex(
        @"gibson.*dog",
        RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex Racism4_();
    public static bool Racism4(string a) => Racism4_().IsMatch(a);

    [GeneratedRegex(
        @".*((\b[Ii].[Mm]\b)|(\b[Aa][Mm]\b)|(\b[Ii][Mm]\b)|(\b[Aa][Gg][Ee]\b)) \b([1-9]|1[0-2])\b.*",
        RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex Underage_();
    public static bool Underage(string a) => Underage_().IsMatch(a);
}
