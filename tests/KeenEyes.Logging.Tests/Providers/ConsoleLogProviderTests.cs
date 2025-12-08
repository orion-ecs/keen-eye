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

    #region Sanitization Tests

    [Fact]
    public void Log_WithNewlineInMessage_SanitizesNewlines()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        using var provider = new ConsoleLogProvider(output, error) { UseColors = false };

        provider.Log(LogLevel.Info, "Test", "Line1\nLine2", null);

        var written = output.ToString();
        written.ShouldContain("Line1\\nLine2");
        written.ShouldNotContain("Line1\nLine2");
    }

    [Fact]
    public void Log_WithCarriageReturnInMessage_SanitizesCarriageReturns()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        using var provider = new ConsoleLogProvider(output, error) { UseColors = false };

        provider.Log(LogLevel.Info, "Test", "Line1\rLine2", null);

        var written = output.ToString();
        written.ShouldContain("Line1\\rLine2");
    }

    [Fact]
    public void Log_WithCrLfInMessage_SanitizesCrLf()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        using var provider = new ConsoleLogProvider(output, error) { UseColors = false };

        provider.Log(LogLevel.Info, "Test", "Line1\r\nLine2", null);

        var written = output.ToString();
        written.ShouldContain("Line1\\r\\nLine2");
    }

    [Fact]
    public void Log_WithNewlineInCategory_SanitizesCategory()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        using var provider = new ConsoleLogProvider(output, error) { UseColors = false };

        provider.Log(LogLevel.Info, "Cat\negory", "Message", null);

        var written = output.ToString();
        written.ShouldContain("[Cat\\negory]");
    }

    [Fact]
    public void Log_WithNewlineInPropertyKey_SanitizesPropertyKey()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        using var provider = new ConsoleLogProvider(output, error) { UseColors = false };
        var properties = new Dictionary<string, object?> { ["Key\nWith\nNewlines"] = "Value" };

        provider.Log(LogLevel.Info, "Test", "Message", properties);

        var written = output.ToString();
        written.ShouldContain("Key\\nWith\\nNewlines=Value");
    }

    [Fact]
    public void Log_WithNewlineInPropertyValue_SanitizesPropertyValue()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        using var provider = new ConsoleLogProvider(output, error) { UseColors = false };
        var properties = new Dictionary<string, object?> { ["Key"] = "Value\nWith\nNewlines" };

        provider.Log(LogLevel.Info, "Test", "Message", properties);

        var written = output.ToString();
        written.ShouldContain("Key=Value\\nWith\\nNewlines");
    }

    [Fact]
    public void Log_WithEmptyMessage_HandlesGracefully()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        using var provider = new ConsoleLogProvider(output, error) { UseColors = false };

        Should.NotThrow(() => provider.Log(LogLevel.Info, "Test", "", null));
    }

    [Fact]
    public void Log_WithEmptyCategory_HandlesGracefully()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        using var provider = new ConsoleLogProvider(output, error) { UseColors = false };

        Should.NotThrow(() => provider.Log(LogLevel.Info, "", "Message", null));
    }

    #endregion

    #region Multiple Properties Tests

    [Fact]
    public void Log_WithMultipleProperties_FormatsAllProperties()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        using var provider = new ConsoleLogProvider(output, error) { UseColors = false };
        var properties = new Dictionary<string, object?>
        {
            ["Key1"] = "Value1",
            ["Key2"] = "Value2",
            ["Key3"] = "Value3"
        };

        provider.Log(LogLevel.Info, "Test", "Message", properties);

        var written = output.ToString();
        written.ShouldContain("Key1=Value1");
        written.ShouldContain("Key2=Value2");
        written.ShouldContain("Key3=Value3");
    }

    [Fact]
    public void Log_WithNullPropertyValue_FormatsAsNull()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        using var provider = new ConsoleLogProvider(output, error) { UseColors = false };
        var properties = new Dictionary<string, object?> { ["NullKey"] = null };

        provider.Log(LogLevel.Info, "Test", "Message", properties);

        var written = output.ToString();
        written.ShouldContain("NullKey=null");
    }

    #endregion

    #region Unknown Level Tests

    [Fact]
    public void Log_WithUnknownLevel_FormatsAsQuestionMarks()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        using var provider = new ConsoleLogProvider(output, error) { UseColors = false };

        provider.Log((LogLevel)99, "Test", "Message", null);

        // Unknown levels >= Error go to error output
        var written = error.ToString();
        written.ShouldContain("???");
    }

    #endregion

    #region Colored Output Tests

    [Fact]
    public void Log_WithColorsEnabled_UsesActualConsoleOut()
    {
        // Capture the original console output
        var originalOut = Console.Out;
        var originalError = Console.Error;

        try
        {
            using var capturedOut = new StringWriter();
            using var capturedError = new StringWriter();
            Console.SetOut(capturedOut);
            Console.SetError(capturedError);

            // Create provider using actual Console.Out/Console.Error (default constructor)
            using var provider = new ConsoleLogProvider();

            // Log at Info level (goes to stdout with colors)
            provider.Log(LogLevel.Info, "Test", "Colored message", null);

            var written = capturedOut.ToString();
            written.ShouldContain("Colored message");
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    [Fact]
    public void Log_WithColorsEnabled_UsesActualConsoleError()
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;

        try
        {
            using var capturedOut = new StringWriter();
            using var capturedError = new StringWriter();
            Console.SetOut(capturedOut);
            Console.SetError(capturedError);

            using var provider = new ConsoleLogProvider();

            // Log at Error level (goes to stderr with colors)
            provider.Log(LogLevel.Error, "Test", "Error with colors", null);

            var written = capturedError.ToString();
            written.ShouldContain("Error with colors");
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Info)]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Error)]
    [InlineData(LogLevel.Fatal)]
    public void Log_AllLevels_WithColorsEnabled_CoverColorMapping(LogLevel level)
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;

        try
        {
            using var capturedOut = new StringWriter();
            using var capturedError = new StringWriter();
            Console.SetOut(capturedOut);
            Console.SetError(capturedError);

            using var provider = new ConsoleLogProvider(); // Colors enabled by default

            provider.Log(level, "Test", $"Message at {level}", null);

            // Check appropriate stream received the message
            var expectedStream = level >= LogLevel.Error ? capturedError : capturedOut;
            expectedStream.ToString().ShouldContain($"Message at {level}");
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    [Fact]
    public void Log_WithUnknownLevel_WithColorsEnabled_UsesDefaultColor()
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;

        try
        {
            using var capturedOut = new StringWriter();
            using var capturedError = new StringWriter();
            Console.SetOut(capturedOut);
            Console.SetError(capturedError);

            using var provider = new ConsoleLogProvider(); // Colors enabled

            // Unknown level 99 >= Error (4), so goes to error stream
            provider.Log((LogLevel)99, "Test", "Unknown level colored", null);

            var written = capturedError.ToString();
            written.ShouldContain("Unknown level colored");
            written.ShouldContain("???");
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    [Fact]
    public void Log_WithColorsEnabledButCustomWriter_DoesNotUseColors()
    {
        // When using custom writers (not Console.Out/Error), colors should be disabled
        using var output = new StringWriter();
        using var error = new StringWriter();
        using var provider = new ConsoleLogProvider(output, error) { UseColors = true };

        // Even though UseColors is true, custom writers should skip color code
        provider.Log(LogLevel.Info, "Test", "No colors here", null);

        var written = output.ToString();
        written.ShouldContain("No colors here");
    }

    #endregion
}
