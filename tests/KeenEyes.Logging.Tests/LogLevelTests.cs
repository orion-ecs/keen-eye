namespace KeenEyes.Logging.Tests;

public class LogLevelTests
{
    [Fact]
    public void LogLevel_ValuesAreOrderedByIncreasingPriority()
    {
        // The numeric values should increase with severity
        ((int)LogLevel.Trace).ShouldBeLessThan((int)LogLevel.Debug);
        ((int)LogLevel.Debug).ShouldBeLessThan((int)LogLevel.Info);
        ((int)LogLevel.Info).ShouldBeLessThan((int)LogLevel.Warning);
        ((int)LogLevel.Warning).ShouldBeLessThan((int)LogLevel.Error);
        ((int)LogLevel.Error).ShouldBeLessThan((int)LogLevel.Fatal);
    }

    [Theory]
    [InlineData(LogLevel.Trace, 0)]
    [InlineData(LogLevel.Debug, 1)]
    [InlineData(LogLevel.Info, 2)]
    [InlineData(LogLevel.Warning, 3)]
    [InlineData(LogLevel.Error, 4)]
    [InlineData(LogLevel.Fatal, 5)]
    public void LogLevel_HasExpectedNumericValue(LogLevel level, int expectedValue)
    {
        ((int)level).ShouldBe(expectedValue);
    }

    [Fact]
    public void LogLevel_HasSixValues()
    {
        var values = Enum.GetValues<LogLevel>();
        values.Length.ShouldBe(6);
    }
}
