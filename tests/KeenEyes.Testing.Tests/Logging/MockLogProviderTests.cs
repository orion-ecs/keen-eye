using KeenEyes.Logging;
using KeenEyes.Testing.Logging;

namespace KeenEyes.Testing.Tests.Logging;

public class MockLogProviderTests
{
    #region Log Capture

    [Fact]
    public void Log_CapturesEntry()
    {
        using var provider = new MockLogProvider();

        provider.Log(LogLevel.Info, "TestCategory", "Test message", null);

        provider.Entries.Count.ShouldBe(1);
        provider.Entries[0].Level.ShouldBe(LogLevel.Info);
        provider.Entries[0].Category.ShouldBe("TestCategory");
        provider.Entries[0].Message.ShouldBe("Test message");
    }

    [Fact]
    public void Log_CapturesProperties()
    {
        using var provider = new MockLogProvider();
        var props = new Dictionary<string, object?> { ["key"] = "value" };

        provider.Log(LogLevel.Debug, "Category", "Message", props);

        provider.Entries[0].Properties.ShouldNotBeNull();
        provider.Entries[0].Properties!["key"].ShouldBe("value");
    }

    [Fact]
    public void Log_CapturesTimestamp()
    {
        using var provider = new MockLogProvider();
        var before = DateTime.UtcNow;

        provider.Log(LogLevel.Info, "Category", "Message", null);

        var after = DateTime.UtcNow;
        provider.Entries[0].Timestamp.ShouldBeGreaterThanOrEqualTo(before);
        provider.Entries[0].Timestamp.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void Log_BelowMinimumLevel_DoesNotCapture()
    {
        using var provider = new MockLogProvider();
        provider.MinimumLevel = LogLevel.Warning;

        provider.Log(LogLevel.Debug, "Category", "Message", null);
        provider.Log(LogLevel.Info, "Category", "Message", null);

        provider.Entries.ShouldBeEmpty();
    }

    [Fact]
    public void Log_AtMinimumLevel_Captures()
    {
        using var provider = new MockLogProvider();
        provider.MinimumLevel = LogLevel.Warning;

        provider.Log(LogLevel.Warning, "Category", "Message", null);

        provider.Entries.Count.ShouldBe(1);
    }

    #endregion

    #region Level Filtering Properties

    [Fact]
    public void Traces_ReturnsOnlyTraces()
    {
        using var provider = new MockLogProvider();
        provider.Log(LogLevel.Trace, "Cat", "Trace", null);
        provider.Log(LogLevel.Debug, "Cat", "Debug", null);

        provider.Traces.Count().ShouldBe(1);
        provider.Traces.First().Message.ShouldBe("Trace");
    }

    [Fact]
    public void Debugs_ReturnsOnlyDebugs()
    {
        using var provider = new MockLogProvider();
        provider.Log(LogLevel.Debug, "Cat", "Debug", null);
        provider.Log(LogLevel.Info, "Cat", "Info", null);

        provider.Debugs.Count().ShouldBe(1);
        provider.Debugs.First().Message.ShouldBe("Debug");
    }

    [Fact]
    public void Infos_ReturnsOnlyInfos()
    {
        using var provider = new MockLogProvider();
        provider.Log(LogLevel.Info, "Cat", "Info", null);
        provider.Log(LogLevel.Warning, "Cat", "Warning", null);

        provider.Infos.Count().ShouldBe(1);
        provider.Infos.First().Message.ShouldBe("Info");
    }

    [Fact]
    public void Warnings_ReturnsOnlyWarnings()
    {
        using var provider = new MockLogProvider();
        provider.Log(LogLevel.Warning, "Cat", "Warning", null);
        provider.Log(LogLevel.Error, "Cat", "Error", null);

        provider.Warnings.Count().ShouldBe(1);
        provider.Warnings.First().Message.ShouldBe("Warning");
    }

    [Fact]
    public void Errors_ReturnsOnlyErrors()
    {
        using var provider = new MockLogProvider();
        provider.Log(LogLevel.Error, "Cat", "Error", null);
        provider.Log(LogLevel.Fatal, "Cat", "Fatal", null);

        provider.Errors.Count().ShouldBe(1);
        provider.Errors.First().Message.ShouldBe("Error");
    }

    [Fact]
    public void Fatals_ReturnsOnlyFatals()
    {
        using var provider = new MockLogProvider();
        provider.Log(LogLevel.Fatal, "Cat", "Fatal", null);
        provider.Log(LogLevel.Error, "Cat", "Error", null);

        provider.Fatals.Count().ShouldBe(1);
        provider.Fatals.First().Message.ShouldBe("Fatal");
    }

    #endregion

    #region Query Methods

    [Fact]
    public void GetLastEntry_ReturnsLastMatching()
    {
        using var provider = new MockLogProvider();
        provider.Log(LogLevel.Info, "Cat", "First", null);
        provider.Log(LogLevel.Warning, "Cat", "Second", null);
        provider.Log(LogLevel.Info, "Cat", "Third", null);

        var last = provider.GetLastEntry(LogLevel.Info);

        last.ShouldNotBeNull();
        last!.Message.ShouldBe("Third");
    }

    [Fact]
    public void GetLastEntry_WithNoMatching_ReturnsNull()
    {
        using var provider = new MockLogProvider();
        provider.Log(LogLevel.Debug, "Cat", "Debug", null);

        var last = provider.GetLastEntry(LogLevel.Error);

        last.ShouldBeNull();
    }

    [Fact]
    public void GetEntriesWhere_ReturnsMatching()
    {
        using var provider = new MockLogProvider();
        provider.Log(LogLevel.Info, "Cat1", "Message1", null);
        provider.Log(LogLevel.Info, "Cat2", "Message2", null);
        provider.Log(LogLevel.Info, "Cat1", "Message3", null);

        var entries = provider.GetEntriesWhere(e => e.Category == "Cat1");

        entries.Count().ShouldBe(2);
    }

    [Fact]
    public void HasEntry_ReturnsTrue_WhenMatches()
    {
        using var provider = new MockLogProvider();
        provider.Log(LogLevel.Error, "Cat", "Error occurred", null);

        provider.HasEntry(e => e.Level == LogLevel.Error).ShouldBeTrue();
    }

    [Fact]
    public void HasEntry_ReturnsFalse_WhenNoMatch()
    {
        using var provider = new MockLogProvider();
        provider.Log(LogLevel.Info, "Cat", "Info", null);

        provider.HasEntry(e => e.Level == LogLevel.Error).ShouldBeFalse();
    }

    [Fact]
    public void GetEntriesForCategory_ReturnsMatching()
    {
        using var provider = new MockLogProvider();
        provider.Log(LogLevel.Info, "Cat1", "Message1", null);
        provider.Log(LogLevel.Info, "Cat2", "Message2", null);

        var entries = provider.GetEntriesForCategory("Cat1");

        entries.Count().ShouldBe(1);
        entries.First().Category.ShouldBe("Cat1");
    }

    #endregion

    #region Properties

    [Fact]
    public void Name_ReturnsMockLogProvider()
    {
        using var provider = new MockLogProvider();

        provider.Name.ShouldBe("MockLogProvider");
    }

    [Fact]
    public void MinimumLevel_DefaultsToTrace()
    {
        using var provider = new MockLogProvider();

        provider.MinimumLevel.ShouldBe(LogLevel.Trace);
    }

    #endregion

    #region Clear and Reset

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        using var provider = new MockLogProvider();
        provider.Log(LogLevel.Info, "Cat", "Message", null);
        provider.Log(LogLevel.Error, "Cat", "Error", null);

        provider.Clear();

        provider.Entries.ShouldBeEmpty();
    }

    [Fact]
    public void Flush_DoesNotThrow()
    {
        using var provider = new MockLogProvider();

        Should.NotThrow(() => provider.Flush());
    }

    #endregion

    #region Thread Safety

    [Fact]
    public async Task Log_IsThreadSafe()
    {
        using var provider = new MockLogProvider();
        var cancellationToken = TestContext.Current.CancellationToken;
        var tasks = new List<Task>();

        for (int i = 0; i < 100; i++)
        {
            int index = i;
            tasks.Add(Task.Run(() => provider.Log(LogLevel.Info, "Cat", $"Message {index}", null), cancellationToken));
        }

        await Task.WhenAll(tasks);

        provider.Entries.Count.ShouldBe(100);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_ClearsEntries()
    {
        var provider = new MockLogProvider();
        provider.Log(LogLevel.Info, "Cat", "Message", null);

        provider.Dispose();

        provider.Entries.ShouldBeEmpty();
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var provider = new MockLogProvider();

        Should.NotThrow(() =>
        {
            provider.Dispose();
            provider.Dispose();
        });
    }

    #endregion
}
