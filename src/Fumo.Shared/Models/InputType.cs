namespace Fumo.Shared.Models;

public abstract record InputType()
{
    public sealed record Optional(string Name) : InputType;
    public sealed record Required(string Name) : InputType;

    public string ToMarkdown()
        => this switch
        {
            Optional(string Name) => $"[{Name}]",
            Required(string Name) => $"<{Name}>",
            _ => throw new NotImplementedException()
        };
}
