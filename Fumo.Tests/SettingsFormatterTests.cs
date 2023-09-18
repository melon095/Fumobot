using Fumo.Shared.Utils;

namespace Fumo.Tests;

public class SettingsFormatterTests
{
    [Theory]
    [InlineData(302, "5m, 2s")]
    [InlineData(52, "52s")]
    [InlineData(6222, "1h, 43m")]
    [InlineData(96231, "1d, 2h")]
    [InlineData(int.MaxValue, "68y, 1mo")]
    [InlineData(int.MinValue, "")]
    [InlineData(0, "")]
    public void SecondsFmt_SimpleShouldFormatInteger(int input, string expected)
    {
        string actual = new SecondsFormatter().SecondsFmt(input);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(302, "5m, 2s")]
    [InlineData(52, "52s")]
    [InlineData(6222, "1h, 43m, 42s")]
    [InlineData(96231, "1d, 2h, 43m, 51s")]
    public void SecondsFmt_LargeLimit_ShouldFormatInteger(int input, string expected)
    {
        string actual = new SecondsFormatter().SecondsFmt(input, limit: 4);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(302, "5m | 2s")]
    [InlineData(52, "52s")]
    public void SecondsFmt_CustomJoin_ShouldFormatInteger(int input, string expected)
    {
        string actual = new SecondsFormatter().SecondsFmt(input, join: " | ");
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(302.5, "5m, 2s")]
    [InlineData(302.2, "5m, 2s")]
    [InlineData(302.8, "5m, 2s")]
    public void SecondsFmt_ShouldFormatDouble(double input, string expected)
    {
        string actual = new SecondsFormatter().SecondsFmt(input);
        Assert.Equal(expected, actual);
    }
}
