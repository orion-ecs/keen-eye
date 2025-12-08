using KeenEyes.Logging.Providers;

namespace KeenEyes.Logging.Tests.Providers;

public class DebugLogProviderTests
{
    [Fact]
    public void Name_ReturnsDebug()
    {
        using var provider = new DebugLogProvider();

        provider.Name.ShouldBe("Debug");
    }

    [Fact]
    public void MinimumLevel_DefaultsToTrace()
    {
        using var provider = new DebugLogProvider();

        provider.MinimumLevel.ShouldBe(LogLevel.Trace);
    }

    [Fact]
    public void MinimumLevel_CanBeSet()
    {
        using var provider = new DebugLogProvider { MinimumLevel = LogLevel.Warning };

        provider.MinimumLevel.ShouldBe(LogLevel.Warning);
    }

    [Fact]
    public void IncludeProperties_DefaultsToTrue()
    {
        using var provider = new DebugLogProvider();

        provider.IncludeProperties.ShouldBeTrue();
    }

    [Fact]
    public void Log_DoesNotThrow()
    {
        using var provider = new DebugLogProvider();

        Should.NotThrow(() => provider.Log(LogLevel.Info, "Test", "Message", null));
    }

    [Fact]
    public void Log_WithProperties_DoesNotThrow()
    {
        using var provider = new DebugLogProvider();
        var properties = new Dictionary<string, object?> { ["Key"] = "Value" };

        Should.NotThrow(() => provider.Log(LogLevel.Info, "Test", "Message", properties));
    }

    [Fact]
    public void Log_BelowMinimumLevel_DoesNotThrow()
    {
        using var provider = new DebugLogProvider { MinimumLevel = LogLevel.Error };

        Should.NotThrow(() => provider.Log(LogLevel.Debug, "Test", "Message", null));
    }

    [Fact]
    public void Flush_DoesNotThrow()
    {
        using var provider = new DebugLogProvider();

        Should.NotThrow(() => provider.Flush());
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var provider = new DebugLogProvider();

        Should.NotThrow(() => provider.Dispose());
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var provider = new DebugLogProvider();

        Should.NotThrow(() =>
        {
            provider.Dispose();
            provider.Dispose();
        });
    }

    [Fact]
    public void Log_AfterDispose_DoesNotThrow()
    {
        var provider = new DebugLogProvider();
        provider.Dispose();

        Should.NotThrow(() => provider.Log(LogLevel.Info, "Test", "Message", null));
    }

    #region Sanitization Tests

    [Fact]
    public void Log_WithNewlineInMessage_DoesNotThrow()
    {
        using var provider = new DebugLogProvider();

        Should.NotThrow(() => provider.Log(LogLevel.Info, "Test", "Line1\nLine2", null));
    }

    [Fact]
    public void Log_WithCarriageReturnInMessage_DoesNotThrow()
    {
        using var provider = new DebugLogProvider();

        Should.NotThrow(() => provider.Log(LogLevel.Info, "Test", "Line1\rLine2", null));
    }

    [Fact]
    public void Log_WithCrLfInMessage_DoesNotThrow()
    {
        using var provider = new DebugLogProvider();

        Should.NotThrow(() => provider.Log(LogLevel.Info, "Test", "Line1\r\nLine2", null));
    }

    [Fact]
    public void Log_WithNewlineInCategory_DoesNotThrow()
    {
        using var provider = new DebugLogProvider();

        Should.NotThrow(() => provider.Log(LogLevel.Info, "Cat\negory", "Message", null));
    }

    [Fact]
    public void Log_WithNewlineInPropertyValue_DoesNotThrow()
    {
        using var provider = new DebugLogProvider();
        var properties = new Dictionary<string, object?> { ["Key"] = "Value\nWith\nNewlines" };

        Should.NotThrow(() => provider.Log(LogLevel.Info, "Test", "Message", properties));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Log_WithEmptyMessage_DoesNotThrow()
    {
        using var provider = new DebugLogProvider();

        Should.NotThrow(() => provider.Log(LogLevel.Info, "Test", "", null));
    }

    [Fact]
    public void Log_WithEmptyCategory_DoesNotThrow()
    {
        using var provider = new DebugLogProvider();

        Should.NotThrow(() => provider.Log(LogLevel.Info, "", "Message", null));
    }

    [Fact]
    public void Log_WithMultipleProperties_DoesNotThrow()
    {
        using var provider = new DebugLogProvider();
        var properties = new Dictionary<string, object?>
        {
            ["Key1"] = "Value1",
            ["Key2"] = "Value2",
            ["Key3"] = "Value3"
        };

        Should.NotThrow(() => provider.Log(LogLevel.Info, "Test", "Message", properties));
    }

    [Fact]
    public void Log_WithNullPropertyValue_DoesNotThrow()
    {
        using var provider = new DebugLogProvider();
        var properties = new Dictionary<string, object?> { ["NullKey"] = null };

        Should.NotThrow(() => provider.Log(LogLevel.Info, "Test", "Message", properties));
    }

    [Fact]
    public void Log_WithUnknownLevel_DoesNotThrow()
    {
        using var provider = new DebugLogProvider();

        Should.NotThrow(() => provider.Log((LogLevel)99, "Test", "Message", null));
    }

    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Info)]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Error)]
    [InlineData(LogLevel.Fatal)]
    public void Log_AtAllLevels_DoesNotThrow(LogLevel level)
    {
        using var provider = new DebugLogProvider();

        Should.NotThrow(() => provider.Log(level, "Test", "Message", null));
    }

    [Fact]
    public void Log_WithPropertiesDisabled_DoesNotThrow()
    {
        using var provider = new DebugLogProvider { IncludeProperties = false };
        var properties = new Dictionary<string, object?> { ["Key"] = "Value" };

        Should.NotThrow(() => provider.Log(LogLevel.Info, "Test", "Message", properties));
    }

    #endregion
}
