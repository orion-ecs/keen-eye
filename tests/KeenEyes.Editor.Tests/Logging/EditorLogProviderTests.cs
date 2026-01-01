using KeenEyes.Editor.Logging;
using KeenEyes.Logging;

namespace KeenEyes.Editor.Tests.Logging;

public class EditorLogProviderTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_CreatesEmptyProvider()
    {
        using var provider = new EditorLogProvider();

        Assert.Equal(0, provider.EntryCount);
        Assert.Equal(0, provider.InfoCount);
        Assert.Equal(0, provider.WarningCount);
        Assert.Equal(0, provider.ErrorCount);
    }

    [Fact]
    public void Constructor_SetsDefaultMaxEntries()
    {
        using var provider = new EditorLogProvider();

        // Default max is 10000, fill beyond that and verify it trims
        for (int i = 0; i < 10010; i++)
        {
            provider.Log(LogLevel.Info, "Test", $"Message {i}", null);
        }

        Assert.True(provider.EntryCount <= 10000);
    }

    [Fact]
    public void Constructor_RespectsCustomMaxEntries()
    {
        using var provider = new EditorLogProvider(maxEntries: 100);

        for (int i = 0; i < 150; i++)
        {
            provider.Log(LogLevel.Info, "Test", $"Message {i}", null);
        }

        Assert.True(provider.EntryCount <= 100);
    }

    #endregion

    #region Log Method Tests

    [Fact]
    public void Log_AddsEntry()
    {
        using var provider = new EditorLogProvider();

        provider.Log(LogLevel.Info, "Category", "Test message", null);

        Assert.Equal(1, provider.EntryCount);
    }

    [Fact]
    public void Log_IncrementsInfoCount_ForInfoAndBelow()
    {
        using var provider = new EditorLogProvider();

        provider.Log(LogLevel.Trace, "Test", "Trace", null);
        provider.Log(LogLevel.Debug, "Test", "Debug", null);
        provider.Log(LogLevel.Info, "Test", "Info", null);

        Assert.Equal(3, provider.InfoCount);
        Assert.Equal(0, provider.WarningCount);
        Assert.Equal(0, provider.ErrorCount);
    }

    [Fact]
    public void Log_IncrementsWarningCount_ForWarning()
    {
        using var provider = new EditorLogProvider();

        provider.Log(LogLevel.Warning, "Test", "Warning", null);

        Assert.Equal(0, provider.InfoCount);
        Assert.Equal(1, provider.WarningCount);
        Assert.Equal(0, provider.ErrorCount);
    }

    [Fact]
    public void Log_IncrementsErrorCount_ForErrorAndAbove()
    {
        using var provider = new EditorLogProvider();

        provider.Log(LogLevel.Error, "Test", "Error", null);
        provider.Log(LogLevel.Fatal, "Test", "Fatal", null);

        Assert.Equal(0, provider.InfoCount);
        Assert.Equal(0, provider.WarningCount);
        Assert.Equal(2, provider.ErrorCount);
    }

    [Fact]
    public void Log_RaisesLogAddedEvent()
    {
        using var provider = new EditorLogProvider();
        LogEntry? receivedEntry = null;
        provider.LogAdded += entry => receivedEntry = entry;

        provider.Log(LogLevel.Info, "Category", "Message", null);

        Assert.NotNull(receivedEntry);
        Assert.Equal("Category", receivedEntry.Category);
        Assert.Equal("Message", receivedEntry.Message);
        Assert.Equal(LogLevel.Info, receivedEntry.Level);
    }

    [Fact]
    public void Log_TrimsOldestEntries_WhenOverLimit()
    {
        using var provider = new EditorLogProvider(maxEntries: 5);

        for (int i = 0; i < 10; i++)
        {
            provider.Log(LogLevel.Info, "Test", $"Message {i}", null);
        }

        var entries = provider.GetEntries().ToList();
        Assert.Equal(5, entries.Count);
        // Oldest entries (0-4) should be trimmed, newest (5-9) should remain
        Assert.Contains(entries, e => e.Message == "Message 9");
    }

    #endregion

    #region GetEntries Tests

    [Fact]
    public void GetEntries_ReturnsAllEntries()
    {
        using var provider = new EditorLogProvider();
        provider.Log(LogLevel.Info, "Test", "One", null);
        provider.Log(LogLevel.Warning, "Test", "Two", null);
        provider.Log(LogLevel.Error, "Test", "Three", null);

        var entries = provider.GetEntries().ToList();

        Assert.Equal(3, entries.Count);
    }

    [Fact]
    public void GetEntries_ReturnsSnapshot()
    {
        using var provider = new EditorLogProvider();
        provider.Log(LogLevel.Info, "Test", "Initial", null);

        var snapshot = provider.GetEntries().ToList();
        provider.Log(LogLevel.Info, "Test", "Added after snapshot", null);

        Assert.Single(snapshot);
    }

    #endregion

    #region GetFilteredEntries Tests

    [Fact]
    public void GetFilteredEntries_FiltersInfo()
    {
        using var provider = new EditorLogProvider();
        provider.Log(LogLevel.Info, "Test", "Info", null);
        provider.Log(LogLevel.Warning, "Test", "Warning", null);
        provider.Log(LogLevel.Error, "Test", "Error", null);

        var filtered = provider.GetFilteredEntries(showInfo: true, showWarnings: false, showErrors: false).ToList();

        Assert.Single(filtered);
        Assert.Equal("Info", filtered[0].Message);
    }

    [Fact]
    public void GetFilteredEntries_FiltersWarnings()
    {
        using var provider = new EditorLogProvider();
        provider.Log(LogLevel.Info, "Test", "Info", null);
        provider.Log(LogLevel.Warning, "Test", "Warning", null);
        provider.Log(LogLevel.Error, "Test", "Error", null);

        var filtered = provider.GetFilteredEntries(showInfo: false, showWarnings: true, showErrors: false).ToList();

        Assert.Single(filtered);
        Assert.Equal("Warning", filtered[0].Message);
    }

    [Fact]
    public void GetFilteredEntries_FiltersErrors()
    {
        using var provider = new EditorLogProvider();
        provider.Log(LogLevel.Info, "Test", "Info", null);
        provider.Log(LogLevel.Warning, "Test", "Warning", null);
        provider.Log(LogLevel.Error, "Test", "Error", null);

        var filtered = provider.GetFilteredEntries(showInfo: false, showWarnings: false, showErrors: true).ToList();

        Assert.Single(filtered);
        Assert.Equal("Error", filtered[0].Message);
    }

    [Fact]
    public void GetFilteredEntries_MultipleFilters()
    {
        using var provider = new EditorLogProvider();
        provider.Log(LogLevel.Info, "Test", "Info", null);
        provider.Log(LogLevel.Warning, "Test", "Warning", null);
        provider.Log(LogLevel.Error, "Test", "Error", null);

        var filtered = provider.GetFilteredEntries(showInfo: true, showWarnings: true, showErrors: false).ToList();

        Assert.Equal(2, filtered.Count);
    }

    [Fact]
    public void GetFilteredEntries_WithSearchText_FiltersMessage()
    {
        using var provider = new EditorLogProvider();
        provider.Log(LogLevel.Info, "Test", "Alpha message", null);
        provider.Log(LogLevel.Info, "Test", "Beta message", null);
        provider.Log(LogLevel.Info, "Test", "Gamma message", null);

        var filtered = provider.GetFilteredEntries(true, true, true, "Beta").ToList();

        Assert.Single(filtered);
        Assert.Equal("Beta message", filtered[0].Message);
    }

    [Fact]
    public void GetFilteredEntries_WithSearchText_FiltersCategory()
    {
        using var provider = new EditorLogProvider();
        provider.Log(LogLevel.Info, "Physics", "Message 1", null);
        provider.Log(LogLevel.Info, "Rendering", "Message 2", null);
        provider.Log(LogLevel.Info, "Audio", "Message 3", null);

        var filtered = provider.GetFilteredEntries(true, true, true, "Render").ToList();

        Assert.Single(filtered);
        Assert.Equal("Message 2", filtered[0].Message);
    }

    [Fact]
    public void GetFilteredEntries_SearchIsCaseInsensitive()
    {
        using var provider = new EditorLogProvider();
        provider.Log(LogLevel.Info, "Test", "Hello World", null);

        var filtered = provider.GetFilteredEntries(true, true, true, "HELLO").ToList();

        Assert.Single(filtered);
    }

    [Fact]
    public void GetFilteredEntries_NullSearchText_ReturnsAll()
    {
        using var provider = new EditorLogProvider();
        provider.Log(LogLevel.Info, "Test", "One", null);
        provider.Log(LogLevel.Info, "Test", "Two", null);

        var filtered = provider.GetFilteredEntries(true, true, true, null).ToList();

        Assert.Equal(2, filtered.Count);
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        using var provider = new EditorLogProvider();
        provider.Log(LogLevel.Info, "Test", "One", null);
        provider.Log(LogLevel.Warning, "Test", "Two", null);
        provider.Log(LogLevel.Error, "Test", "Three", null);

        provider.Clear();

        Assert.Equal(0, provider.EntryCount);
        Assert.Equal(0, provider.InfoCount);
        Assert.Equal(0, provider.WarningCount);
        Assert.Equal(0, provider.ErrorCount);
    }

    [Fact]
    public void Clear_RaisesLogsClearedEvent()
    {
        using var provider = new EditorLogProvider();
        provider.Log(LogLevel.Info, "Test", "Message", null);
        var eventRaised = false;
        provider.LogsCleared += () => eventRaised = true;

        provider.Clear();

        Assert.True(eventRaised);
    }

    #endregion

    #region ILogProvider Interface Tests

    [Fact]
    public void Name_ReturnsEditorConsole()
    {
        using var provider = new EditorLogProvider();

        Assert.Equal("EditorConsole", provider.Name);
    }

    [Fact]
    public void MinimumLevel_DefaultsToTrace()
    {
        using var provider = new EditorLogProvider();

        Assert.Equal(LogLevel.Trace, provider.MinimumLevel);
    }

    [Fact]
    public void MinimumLevel_IsSettable()
    {
        using var provider = new EditorLogProvider();

        provider.MinimumLevel = LogLevel.Warning;

        Assert.Equal(LogLevel.Warning, provider.MinimumLevel);
    }

    [Fact]
    public void Flush_DoesNotThrow()
    {
        using var provider = new EditorLogProvider();

        var exception = Record.Exception(() => provider.Flush());

        Assert.Null(exception);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task Log_IsThreadSafe()
    {
        using var provider = new EditorLogProvider();
        const int threadCount = 10;
        const int logsPerThread = 100;

        var tasks = Enumerable.Range(0, threadCount)
            .Select(t => Task.Run(() =>
            {
                for (int i = 0; i < logsPerThread; i++)
                {
                    provider.Log(LogLevel.Info, $"Thread{t}", $"Message {i}", null);
                }
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(threadCount * logsPerThread, provider.EntryCount);
    }

    #endregion
}
