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
}
