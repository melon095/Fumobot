namespace Fumo.Shared.Utils;

public static class UnpingUser
{
    public static string Unping(string input) => $"{input.ElementAt(0)}\uE000{input[1..]}";
}
