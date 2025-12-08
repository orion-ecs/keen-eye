using KeenEyes.Logging.Providers;

namespace KeenEyes.Logging.Tests.Providers;

public class ConsoleLogProviderTests
{
    [Fact]
    public void Name_ReturnsConsole()
    {
        using var provider = new ConsoleLogProvider();

        provider.Name.ShouldBe("Console");
    }

    [Fact]
    public void MinimumLevel_DefaultsToTrace()
    {
        using var provider = new ConsoleLogProvider();

        provider.MinimumLevel.ShouldBe(LogLevel.Trace);
    }

    [Fact]
    public void UseColors_DefaultsToTrue()
    {
        using var provider = new ConsoleLogProvider();

        provider.UseColors.ShouldBeTrue();
    }

    [Fact]
    public void Log_WritesToOutput()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        using var provider = new ConsoleLogProvider(output, error) { UseColors = false };

        provider.Log(LogLevel.Info, "TestCategory", "Test message", null);

        var written = output.ToString();
        written.ShouldContain("INF");
        written.ShouldContain("[TestCategory]");
        written.ShouldContain("Test message");
    }

    [Fact]
    public void Log_ErrorLevel_WritesToErrorOutput()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        using var provider = new ConsoleLogProvider(output, error) { UseColors = false };

        provider.Log(LogLevel.Error, "Test", "Error message", null);

        output.ToString().ShouldBeEmpty();
        error.ToString().ShouldContain("Error message");
    }

    [Fact]
    public void Log_FatalLevel_WritesToErrorOutput()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        using var provider = new ConsoleLogProvider(output, error) { UseColors = false };

        provider.Log(LogLevel.Fatal, "Test", "Fatal message", null);

        output.ToString().ShouldBeEmpty();
        error.ToString().ShouldContain("Fatal message");
    }

    [Fact]
    public void Log_WithProperties_IncludesPropertiesInOutput()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        using var provider = new ConsoleLogProvider(output, error)
        {
            UseColors = false,
            IncludeProperties = true
        };
        var properties = new Dictionary<string, object?> { ["Key"] = "Value" };

        provider.Log(LogLevel.Info, "Test", "Message", properties);

        var written = output.ToString();
        written.ShouldContain("Key=Value");
    }

    [Fact]
    public void Log_WithPropertiesDisabled_ExcludesProperties()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        using var provider = new ConsoleLogProvider(output, error)
        {
            UseColors = false,
            IncludeProperties = false
        };
        var properties = new Dictionary<string, object?> { ["Key"] = "Value" };

        provider.Log(LogLevel.Info, "Test", "Message", properties);

        var written = output.ToString();
        written.ShouldNotContain("Key=Value");
    }

    [Theory]
    [InlineData(LogLevel.Trace, "TRC")]
    [InlineData(LogLevel.Debug, "DBG")]
    [InlineData(LogLevel.Info, "INF")]
    [InlineData(LogLevel.Warning, "WRN")]
    [InlineData(LogLevel.Error, "ERR")]
    [InlineData(LogLevel.Fatal, "FTL")]
    public void Log_FormatsLevelCorrectly(LogLevel level, string expectedAbbreviation)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        using var provider = new ConsoleLogProvider(output, error) { UseColors = false };

        provider.Log(level, "Test", "Message", null);

        var written = level >= LogLevel.Error ? error.ToString() : output.ToString();
        written.ShouldContain(expectedAbbreviation);
    }

    [Fact]
    public void Log_BelowMinimumLevel_DoesNotWrite()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        using var provider = new ConsoleLogProvider(output, error)
        {
            MinimumLevel = LogLevel.Warning
        };

        provider.Log(LogLevel.Debug, "Test", "Debug message", null);

        output.ToString().ShouldBeEmpty();
        error.ToString().ShouldBeEmpty();
    }

    [Fact]
    public void Log_WithNullProperties_DoesNotThrow()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        using var provider = new ConsoleLogProvider(output, error) { UseColors = false };

        Should.NotThrow(() => provider.Log(LogLevel.Info, "Test", "Message", null));
    }

    [Fact]
    public void Constructor_WithNullOutput_ThrowsArgumentNullException()
    {
        using var error = new StringWriter();

        Should.Throw<ArgumentNullException>(() => new ConsoleLogProvider(null!, error));
    }

    [Fact]
    public void Constructor_WithNullError_ThrowsArgumentNullException()
    {
        using var output = new StringWriter();

        Should.Throw<ArgumentNullException>(() => new ConsoleLogProvider(output, null!));
    }

    [Fact]
    public void Flush_DoesNotThrow()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        using var provider = new ConsoleLogProvider(output, error);

        Should.NotThrow(() => provider.Flush());
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var provider = new ConsoleLogProvider(output, error);

        Should.NotThrow(() =>
        {
            provider.Dispose();
            provider.Dispose();
        });
    }

    [Fact]
    public void Log_AfterDispose_DoesNotThrow()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var provider = new ConsoleLogProvider(output, error);
        provider.Dispose();

        Should.NotThrow(() => provider.Log(LogLevel.Info, "Test", "Message", null));
    }
}
