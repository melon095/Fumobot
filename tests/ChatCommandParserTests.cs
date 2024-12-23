﻿using Fumo.Shared.Exceptions;
using Fumo.Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace Fumo.Tests;

public class TestCommand : ChatCommand
{
    public override ValueTask BuildHelp(ChatCommandHelpBuilder builder, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public override ValueTask<CommandResult> Execute(CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}

public class ChatCommandParserTests : TestCommand
{
    [Fact]
    public void CommandParser_CanParseString()
    {
        // Arrange
        this.Parameters.Add(MakeParameter<string>("foo"));
        var input = new List<string> { "bad", "data", "--foo", "bar" };
        var expectedInput = new List<string> { "bad", "data" };

        // Act
        this.ParseArguments(input);

        // Assert
        Assert.Equal("bar", this.GetArgument<string>("foo"));
        Assert.Equal(string.Empty, this.GetArgument<string>("bar"));
        Assert.Equal(expectedInput, input);
    }

    [Fact]
    public void CommandParser_CanParseBoolean()
    {
        // Arrange
        this.Parameters.Add(MakeParameter<bool>("foo"));
        var input = new List<string> { "lol", "--foo", "bar" };
        var expectedInput = new List<string> { "lol", "bar" };

        // Act
        this.ParseArguments(input);

        // Assert
        Assert.True(this.GetArgument<bool>("foo"));
        Assert.False(this.GetArgument<bool>("bar"));
        Assert.Equal(expectedInput, input);
    }

    [Fact]
    public void CommandParser_CanParseNumber()
    {
        // Arrange
        this.Parameters.Add(MakeParameter<int>("foo"));
        var input = new List<string> { "--foo", "42" };
        var expectedInput = Array.Empty<string>();

        // Act
        this.ParseArguments(input);

        // Assert
        Assert.Equal(42, this.GetArgument<int>("foo"));
        Assert.Equal(expectedInput, input);
    }

    [Fact]
    public void CommandParser_CanParsMultipleArguments()
    {
        // Arrange
        this.Parameters.Add(MakeParameter<string>("foo"));
        this.Parameters.Add(MakeParameter<bool>("bar"));
        var input = new List<string> { "lol", "--foo", "bar", "--bar", "xd" };
        var expectedInput = new List<string> { "lol", "xd" };

        // Act
        this.ParseArguments(input);

        // Assert
        Assert.Equal("bar", this.GetArgument<string>("foo"));
        Assert.True(this.GetArgument<bool>("bar"));
        Assert.Equal(expectedInput, input);
    }

    [Fact]
    public void CommandParser_IgnoreNotSetArguments()
    {
        // Arrange
        var input = new List<string> { "--foo", "xd" };
        var expectedInput = new List<string> { "--foo", "xd" };

        // Act
        this.ParseArguments(input);

        // Assert
        Assert.Equal(expectedInput, input);
        Assert.Equal(string.Empty, this.GetArgument<string>("foo"));
    }

    [Fact]
    public void CommandParser_CanParseShort()
    {
        // Arrange
        this.Parameters.Add(MakeParameter<string>("foo"));
        var input = new List<string> { "-f", "xd" };
        var expectedInput = new List<string> { };

        // Act
        this.ParseArguments(input);

        // Assert
        Assert.Equal(expectedInput, input);
        Assert.Equal("xd", this.GetArgument<string>("foo"));
    }

    [Fact]
    public void CommandParser_CanParseIndependentlyOfDefinitionOrder()
    {
        // Arrange
        this.Parameters.Add(MakeParameter<string>("baz"));
        this.Parameters.Add(MakeParameter<string>("foo"));
        var input = new List<string>
        {
            "hi", "--foo", "bar", "xd", "--baz", "qux"
        };
        var expectedInput = new List<string>
        {
            "hi", "xd"
        };

        // Act
        this.ParseArguments(input);

        // Assert
        Assert.Equal("bar", this.GetArgument<string>("foo"));
        Assert.Equal("qux", this.GetArgument<string>("baz"));
        Assert.Equal(expectedInput, input);
    }

    [Fact]
    public void CommandParser_CanParseSentences()
    {
        // Arrange
        this.Parameters.Add(MakeParameter<string>("foo"));
        var input = new List<string> { "hi", "--foo", "\"hi", "a", "sentence\"" };
        var expectedInput = new List<string> { "hi" };

        // Act
        this.ParseArguments(input);

        // Assert
        Assert.Equal("hi a sentence", this.GetArgument<string>("foo"));
        Assert.Equal(expectedInput, input);
    }

    [Fact]
    public void CommandParser_CannotParseQuoteMismatch()
    {
        // Arrange
        this.Parameters.Add(MakeParameter<string>("foo"));
        var input = new List<string> { "--foo", "\"hi", "lol'" };

        // Act
        Assert.Throws<InvalidCommandArgumentException>(() => this.ParseArguments(input));
    }

    [Fact]
    public void CommandParser_MissingString_CorrectException()
    {
        // Arrange
        this.Parameters.Add(MakeParameter<string>("foo"));
        var input = new List<string> { "--foo" };

        // Act
        var ex = Assert.Throws<InvalidCommandArgumentException>(() => this.ParseArguments(input));

        // Assert
        Assert.Equal("Parameter foo expected a text value", ex.Message);
    }
}
