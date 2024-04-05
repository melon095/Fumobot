using System.Text;

namespace Fumo.Shared.Models;

public partial class ChatCommandHelpBuilder
{
    #region Properties

    public string DisplayName = string.Empty;

    public bool ShouldBeCached = false;

    private readonly string Prefix;

    private string Description = string.Empty;

    private List<InputType> Usage = [];

    private readonly List<ExampleLine> Examples = [];

    private readonly List<ArgumentLine> Arguments = [];

    private readonly List<ChatCommandHelpBuilder> Subcommands = [];

    private readonly int SubDepth = 0;

    private NameStrategyFunctor NameStrategy = DefaultNameStrategy;

    #endregion

    #region Constructor

    public ChatCommandHelpBuilder(string prefix, int depth = 0)
    {
        Prefix = prefix;
        SubDepth = depth;
    }

    #endregion

    #region Public Interface


    public ChatCommandHelpBuilder WithCache(bool set = true)
    {
        ShouldBeCached = set;
        return this;
    }

    public ChatCommandHelpBuilder WithDisplayName(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));

        DisplayName = name;
        return this;
    }

    public ChatCommandHelpBuilder WithDescription(string description)
    {
        Description = description;
        return this;
    }

    public ChatCommandHelpBuilder WithUsage(Action<UsageBuilder> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        UsageBuilder builder = new();

        func(builder);

        Usage = builder.Build();

        return this;
    }

    public ChatCommandHelpBuilder WithExample(string example, string? notes = null)
    {
        Examples.Add(new ExampleLine(example, notes));
        return this;
    }

    public ChatCommandHelpBuilder WithArgument(string name, Action<ArgumentLine> func)
    {
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
        ArgumentNullException.ThrowIfNull(func);

        ArgumentLine line = new(name[0], name);

        func(line);

        Arguments.Add(line);

        return this;
    }

    public ChatCommandHelpBuilder WithSubcommand(string name, Action<ChatCommandHelpBuilder> func)
    {
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
        ArgumentNullException.ThrowIfNull(func);

        // NOTE: This techincally is the prefix for the subcommand
        var prefix = $"{Prefix} {DisplayName}";

        ChatCommandHelpBuilder builder = new(prefix, SubDepth + 1)
        {
            DisplayName = name,
            NameStrategy = SubNameStrategy
        };

        func(builder);

        Subcommands.Add(builder);

        return this;
    }

    public ValueTask Finish => ValueTask.CompletedTask;

    #endregion

    #region Output Generators

    public string BuildMarkdown()
    {
        StringBuilder
            output = new(),
            usageBuilder = new(),
            examplesBuilder = new(),
            subcommandsBuilder = new(),
            argumentsBuilder = new();

        output.AppendLine(NameStrategy.Invoke(DisplayName)).AppendLine();

        if (!string.IsNullOrWhiteSpace(Description))
            output.AppendLine(Description.Trim());

        if (Usage.Count > 0)
        {
            BuildUsage(usageBuilder);
            output.AppendLine().AppendLine(usageBuilder.ToString());
        }

        if (Examples.Count > 0)
        {
            BuildExamples(examplesBuilder);
            output.AppendLine(examplesBuilder.ToString());
        }

        if (Subcommands.Count > 0)
        {
            BuildSubcommands(subcommandsBuilder);
            output.AppendLine(subcommandsBuilder.ToString());
        }

        if (Arguments.Count > 0)
        {
            BuildArguments(argumentsBuilder);
            output.AppendLine(argumentsBuilder.ToString());
        }


        return output.ToString().Trim();
    }

    private void BuildUsage(StringBuilder output)
    {
        var codeBlock = $"{Prefix} {DisplayName} {string.Join(' ', Usage.Select(x => x.ToMarkdown()))}";

        output
            .AppendLine($"{MarkdownHelper.Heading(3, "Usage")}")
            .AppendLine($"{MarkdownHelper.CodeBlock(codeBlock)}");
    }

    private void BuildExamples(StringBuilder output)
    {
        output.AppendLine($"{MarkdownHelper.Heading(3, "Examples")}");

        StringBuilder exampleOut = new();

        for (var i = 0; i < Examples.Count; i++)
        {
            var example = Examples[i];

            exampleOut.Append($"{Prefix} {DisplayName} {example.Example}");

            if (!string.IsNullOrWhiteSpace(example.Notes))
                exampleOut.Append($"\n\t- {example.Notes}");

            if (i != Examples.Count - 1)
                exampleOut.AppendLine();
        }

        output.AppendLine(MarkdownHelper.CodeBlock(exampleOut.ToString()));
    }

    private void BuildSubcommands(StringBuilder output)
    {
        output.AppendLine($"{MarkdownHelper.Heading(3, "Subcommands")}").AppendLine();

        foreach (var subcommand in Subcommands)
        {
            var markdown = subcommand.BuildMarkdown();

            output
                .Append("<details>")
                .AppendLine(markdown)
                .AppendLine("</details>");
        }
    }

    private void BuildArguments(StringBuilder output)
    {
        output.AppendLine($"{MarkdownHelper.Heading(3, "Arguments")}");

        foreach (var argument in Arguments)
            output.AppendLine(argument.ToMarkdown());
    }

    #endregion

    #region Types

    public class ArgumentLine(char Short, string Long) : InputTypeHelper<ArgumentLine>
    {
        private char Short { get; init; } = Short;

        private string Long { get; init; } = Long;

        public string Description = string.Empty;

        public string ToMarkdown()
        {
            // FIXME: Can maybe enforce better
            // FIXME: Can remove 'InputTypeHelper' and specify data type?
            var input = Inputs.Count > 0
                ? " " + Inputs[0].ToMarkdown()
                : string.Empty;

            var shortName = MarkdownHelper.Code($"-{Short}{input}");
            var longName = MarkdownHelper.Code($"--{Long}{input}");

            return $"- {shortName}, {longName}: {Description}";
        }
    }

    readonly record struct ExampleLine(string Example, string? Notes = null);

    public class UsageBuilder() : InputTypeHelper<UsageBuilder>
    {
        public List<InputType> Build() => Inputs;
    }

    public class InputTypeHelper<TParent> where TParent : class
    {
        // FIXME: Clean TParent?

        protected readonly List<InputType> Inputs = [];

        public TParent Required(string name)
        {
            Inputs.Add(new InputType.Required(name));
            return (this as TParent)!;
        }

        public TParent Optional(string name)
        {
            Inputs.Add(new InputType.Optional(name));
            return (this as TParent)!;
        }
    }

    public delegate string NameStrategyFunctor(string input);

    public static readonly NameStrategyFunctor DefaultNameStrategy = input => MarkdownHelper.Heading(2, input);

    public static readonly NameStrategyFunctor SubNameStrategy = input => $"<summary>{input}</summary>";

    #endregion
}
