using Fumo.Shared.Exceptions;
using System.Text;

namespace Fumo.Shared.Models;

public partial class ChatCommand
{
    private const string CLRString = nameof(String);
    private const string CLRInt32 = nameof(Int32);
    private const string CLRBoolean = nameof(Boolean);

    private const char DoubleQuote = '"';
    private const char SingleQuote = '\'';

    private readonly Dictionary<string, object> parsedParameters = [];

    #region Implementation

    /// <exception cref="InvalidCommandArgumentException">Invalid input</exception>
    /// <exception cref="Exception">Internal error</exception>
    public void ParseArguments(List<string> input)
    {
        if (Parameters.Count == 0)
            return;

        for (int idx = 0; idx < input.Count; idx++)
        {
            var param = Parameters.FirstOrDefault(x => $"--{x.Name}" == input[idx] || $"-{x.Name[0]}" == input[idx]);

            if (param is null)
                continue;

            var value = input.ElementAtOrDefault(idx + 1);

            parsedParameters[param.Name] = param.Type.Name switch
            {
                // Parse quoted strings
                CLRString when value is not null && (value.StartsWith(DoubleQuote) || value.StartsWith(SingleQuote)) =>
                    ParseQuotedString(input, ref idx, param, value),

                // Parse strings
                CLRString when value is not null =>
                    ParseString(input, ref idx, param, value),

                // Strings when missing value
                CLRString when value is null =>
                    throw new InvalidCommandArgumentException(param.Name, "expected a text value"),

                // Parse integers
                CLRInt32 when value is not null && int.TryParse(value, out var number) =>
                    ParseInt(input, ref idx, param, number),

                // Integers when missing value
                CLRInt32 when value is null =>
                    throw new InvalidCommandArgumentException(param.Name, "expected a number"),

                // Parse booleans
                CLRBoolean =>
                    ParseBoolean(input, ref idx, param),

                _ => throw new InvalidCommandArgumentException(param.Name, "Invalid argument type or missing value")
            };
        }
    }

    private static string ParseQuotedString(List<string> input, ref int idx, Parameter param, string value)
    {
        var usedQuote = value[0];
        var endIdx = input.FindIndex(idx + 1, x => x.EndsWith(usedQuote));
        if (endIdx == -1)
            throw new InvalidCommandArgumentException(param.Name, $"is missing a closing quote (--{param.Name} \"value\")");

        var sb = new StringBuilder();
        for (int i = idx + 1; i <= endIdx; i++)
        {
            sb.Append(input[i]);
            sb.Append(' ');
        }

        input.RemoveRange(idx, endIdx);
        idx--;

        return sb.ToString().Trim()[1..^1];
    }

    private static string ParseString(List<string> input, ref int idx, Parameter param, string value)
    {
        input.RemoveRange(idx, 2);
        if (idx < input.Count)
        {
            idx--;
        }
        return value;
    }

    private static int ParseInt(List<string> input, ref int idx, Parameter param, int number)
    {
        input.RemoveRange(idx, 2);
        return number;
    }

    private static bool ParseBoolean(List<string> input, ref int idx, Parameter param)
    {
        input.RemoveAt(idx);
        idx--;
        return true;
    }

    #endregion

    #region Public

    protected virtual List<Parameter> Parameters { get; } = [];

    protected static Parameter MakeParameter<TValue>(string name)
        => new(typeof(TValue), name);

    protected TValue GetArgument<TValue>(string name)
    {
        if (typeof(TValue) == typeof(string))
        {
            return (TValue)parsedParameters.GetValueOrDefault(name, string.Empty);
        }

        return (TValue)parsedParameters.GetValueOrDefault(name, default(TValue)!);
    }

    protected bool TryGetArgument<TValue>(string name, out TValue value)
    {
        if (this.parsedParameters.TryGetValue(name, out var t))
        {
            value = (TValue)t;

            return true;
        }

        value = default!;

        return false;
    }

    public record Parameter(Type Type, string Name);

    #endregion
}
