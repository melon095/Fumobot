using Fumo.Shared.Models;

namespace Fumo.Tests;

public class ChatCommandHelpBuilderTests
{
    [Theory, ClassData(typeof(CCHBTTheoryData))]
    public void Test(ChatCommandHelpBuilder builder, string expected)
    {
        var output = builder.BuildMarkdown();

        Assert.Equal(expected.Trim(), output);
    }
}

class CCHBTTheoryData : TheoryData<ChatCommandHelpBuilder, string>
{
    public CCHBTTheoryData()
    {
        One();
        Two();
    }

    void One()
    {
        const string DisplayName = "smoothie";
        const string Description = "This is a description";

        Add(new ChatCommandHelpBuilder("!")
                    .WithDisplayName(DisplayName)
                    .WithDescription(Description)
                    .WithUsage(x => x.Optional("juice").Required("fruit..."))
                    .WithExample("apple")
                    .WithExample("banana")
                    .WithArgument("remove", x =>
                    {
                        x.Description = "Remove the fruit";
                        x.Required("fruit");
                    })
                    .WithArgument("boost", (x) => x.Description = "!boost"),
        $@"
## {DisplayName}

{Description}

### Usage
```
! smoothie [juice] <fruit...>
```

### Examples
```
! smoothie apple
! smoothie banana
```

### Arguments
- `-r <fruit>`, `--remove <fruit>`: Remove the fruit
- `-b`, `--boost`: !boost
"
        );
    }

    void Two()
    {
        const string Displayname = "sandwich";
        const string Description = "Create sandwiches";

        Add(
            new ChatCommandHelpBuilder("!")
                .WithDisplayName(Displayname)
                .WithDescription(Description)
                .WithUsage(x => x.Required("sandwich_type").Optional("filling..."))
                .WithSubcommand("bread", x =>
                {
                    x.WithDisplayName("bread")
                        .WithDescription("Set the bread type")
                        .WithUsage(x => x.Required("type"))
                        .WithExample("white")
                        .WithExample("brown");
                })
                .WithSubcommand("filling", x =>
                {
                    x.WithDisplayName("filling")
                        .WithDescription("Set the filling type")
                        .WithUsage(x => x.Required("type"))
                        .WithExample("cheese")
                        .WithExample("ham");
                })
                .WithArgument("example", x => x.Description = "foo"),

                $@"
## {Displayname}

{Description}

### Usage
```
! sandwich <sandwich_type> [filling...]
```

### Subcommands

<details><summary>bread</summary>

Set the bread type

### Usage
```
! sandwich bread <type>
```

### Examples
```
! sandwich bread white
! sandwich bread brown
```
</details>
<details><summary>filling</summary>

Set the filling type

### Usage
```
! sandwich filling <type>
```

### Examples
```
! sandwich filling cheese
! sandwich filling ham
```
</details>

### Arguments
- `-e`, `--example`: foo");
    }
}