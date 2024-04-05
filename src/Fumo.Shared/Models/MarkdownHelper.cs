namespace Fumo.Shared.Models;

public static class MarkdownHelper
{
    public static string Bold(string text) => $"**{text}**";

    public static string Italic(string text) => $"*{text}*";

    public static string Heading(int level, string text) => $"{new string('#', level)} {text}";

    public static string Code(string text) => $"`{text}`";

    public static string CodeBlock(string text) => $"```{Environment.NewLine}{text}{Environment.NewLine}```";
}
