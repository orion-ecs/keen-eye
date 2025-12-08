using KeenEyes.Logging.Providers;

namespace KeenEyes.Logging.Tests.Providers;

public class NullLogProviderTests
{
    [Fact]
    public void Name_ReturnsNull()
    {
        using var provider = new NullLogProvider();

        provider.Name.ShouldBe("Null");
    }

    [Fact]
    public void MinimumLevel_DefaultsToTrace()
    {
        using var provider = new NullLogProvider();

        provider.MinimumLevel.ShouldBe(LogLevel.Trace);
    }

    [Fact]
    public void MinimumLevel_CanBeSet()
    {
        using var provider = new NullLogProvider { MinimumLevel = LogLevel.Fatal };

        provider.MinimumLevel.ShouldBe(LogLevel.Fatal);
    }

    [Fact]
    public void Log_DoesNotThrow()
    {
        using var provider = new NullLogProvider();

        Should.NotThrow(() => provider.Log(LogLevel.Fatal, "Test", "Critical message", null));
    }

    [Fact]
    public void Log_WithProperties_DoesNotThrow()
    {
        using var provider = new NullLogProvider();
        var properties = new Dictionary<string, object?>
        {
            ["Key1"] = "Value1",
            ["Key2"] = 42,
            ["Key3"] = null
        };

        Should.NotThrow(() => provider.Log(LogLevel.Info, "Test", "Message", properties));
    }

    [Fact]
    public void Log_WithNullMessage_DoesNotThrow()
    {
        using var provider = new NullLogProvider();

        Should.NotThrow(() => provider.Log(LogLevel.Info, "Test", null!, null));
    }

    [Fact]
    public void Log_WithNullCategory_DoesNotThrow()
    {
        using var provider = new NullLogProvider();

        Should.NotThrow(() => provider.Log(LogLevel.Info, null!, "Message", null));
    }

    [Fact]
    public void Flush_DoesNotThrow()
    {
        using var provider = new NullLogProvider();

        Should.NotThrow(() => provider.Flush());
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var provider = new NullLogProvider();

        Should.NotThrow(() => provider.Dispose());
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var provider = new NullLogProvider();

        Should.NotThrow(() =>
        {
            provider.Dispose();
            provider.Dispose();
            provider.Dispose();
        });
    }

    [Fact]
    public void Log_AtAllLevels_DoesNotThrow()
    {
        using var provider = new NullLogProvider();

        foreach (var level in Enum.GetValues<LogLevel>())
        {
            Should.NotThrow(() => provider.Log(level, "Test", $"Message at {level}", null));
        }
    }
}
