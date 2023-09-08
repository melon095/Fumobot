using Fumo.Shared.Exceptions;
using System.Text;

namespace Fumo.Shared.Models;

public class ChatCommandArguments
{
    private readonly List<Parameter> parameters = new();
    private readonly Dictionary<string, object> parsedParameters = new();

    /// <exception cref="InvalidCommandArgumentException">Invalid input</exception>
    /// <exception cref="Exception">Internal error</exception>
    public void ParseArguments(List<string> input)
    {
        for (int idx = 0; idx < input.Count; idx++)
        {
            Parameter? param = parameters.FirstOrDefault(x => $"--{x.Name}" == input[idx] || $"-{x.Name[0]}" == input[idx]);

            if (param is null)
                continue;

            switch (param.Type.Name)
            {
                case "String":
                    {
                        var value = input.ElementAtOrDefault(idx + 1) ?? throw new InvalidCommandArgumentException(param.Name, "expected a text value");

                        if (value.StartsWith("\"") || value.StartsWith("'"))
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

                            // Range thingy removes quotes
                            parsedParameters[param.Name] = sb.ToString().Trim()[1..^1];
                            input.RemoveRange(idx, endIdx);
                            idx--;
                        }
                        else
                        {
                            parsedParameters[param.Name] = value;
                            input.RemoveRange(idx, 2);
                            if (idx < input.Count)
                            {
                                idx--;
                            }
                        }
                    }
                    break;

                case "Int32":
                    {
                        var value = input.ElementAtOrDefault(idx + 1) ?? throw new InvalidCommandArgumentException(param.Name, $"is missing a number (--{param.Name} 42)");
                        if (int.TryParse(value, out var number))
                        {
                            parsedParameters[param.Name] = number;

                            input.RemoveRange(idx, 2);
                        }
                        else
                        {
                            throw new InvalidCommandArgumentException(param.Name, "expected a number");
                        }
                    }
                    break;

                case "Boolean":
                    {
                        parsedParameters[param.Name] = true;

                        input.RemoveAt(idx);
                        idx--;
                    }
                    break;

                default:
                    {
                        throw new Exception($"Type {param.Type.Name} is not parsable");
                    }
            }
        }
    }

    protected void AddParameter(Parameter parameter)
    {
        parameters.Add(parameter);
    }

    //protected TValue GetArgument<TValue>(string name) => (TValue)this.parsedParameters.GetValueOrDefault(name, default(TValue));
    // TODO: Maybe remove this, idk if checking typeof is slow or not. But the default of string is null so yeah.
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

    protected record Parameter(Type Type, string Name);
}
