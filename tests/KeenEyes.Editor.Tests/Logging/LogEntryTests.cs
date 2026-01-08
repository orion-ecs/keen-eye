using KeenEyes.Editor.Logging;
using KeenEyes.Logging;

namespace KeenEyes.Editor.Tests.Logging;

public class LogEntryTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var timestamp = new DateTime(2024, 1, 15, 10, 30, 45, 123);
        var properties = new Dictionary<string, object?> { { "key", "value" } };

        var entry = new LogEntry(timestamp, LogLevel.Warning, "Category", "Message", properties);

        Assert.Equal(timestamp, entry.Timestamp);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Equal("Category", entry.Category);
        Assert.Equal("Message", entry.Message);
        Assert.Same(properties, entry.Properties);
    }

    [Fact]
    public void Constructor_PropertiesDefaultsToNull()
    {
        var entry = new LogEntry(DateTime.Now, LogLevel.Info, "Cat", "Msg", null);

        Assert.Null(entry.Properties);
    }

    #endregion

    #region GetFormattedTime Extension Tests

    [Fact]
    public void GetFormattedTime_FormatsCorrectly()
    {
        var timestamp = new DateTime(2024, 1, 15, 9, 5, 3, 42);
        var entry = new LogEntry(timestamp, LogLevel.Info, "Cat", "Msg", null);

        Assert.Equal("09:05:03.042", entry.GetFormattedTime());
    }

    [Fact]
    public void GetFormattedTime_IncludesMilliseconds()
    {
        var timestamp = new DateTime(2024, 1, 15, 12, 30, 45, 999);
        var entry = new LogEntry(timestamp, LogLevel.Info, "Cat", "Msg", null);

        Assert.EndsWith(".999", entry.GetFormattedTime());
    }

    #endregion

    #region IsError Extension Tests

    [Fact]
    public void IsError_ReturnsTrue_ForError()
    {
        var entry = new LogEntry(DateTime.Now, LogLevel.Error, "Cat", "Msg", null);

        Assert.True(entry.IsError());
    }

    [Fact]
    public void IsError_ReturnsTrue_ForFatal()
    {
        var entry = new LogEntry(DateTime.Now, LogLevel.Fatal, "Cat", "Msg", null);

        Assert.True(entry.IsError());
    }

    [Fact]
    public void IsError_ReturnsFalse_ForWarning()
    {
        var entry = new LogEntry(DateTime.Now, LogLevel.Warning, "Cat", "Msg", null);

        Assert.False(entry.IsError());
    }

    [Fact]
    public void IsError_ReturnsFalse_ForInfo()
    {
        var entry = new LogEntry(DateTime.Now, LogLevel.Info, "Cat", "Msg", null);

        Assert.False(entry.IsError());
    }

    [Fact]
    public void IsError_ReturnsFalse_ForDebug()
    {
        var entry = new LogEntry(DateTime.Now, LogLevel.Debug, "Cat", "Msg", null);

        Assert.False(entry.IsError());
    }

    [Fact]
    public void IsError_ReturnsFalse_ForTrace()
    {
        var entry = new LogEntry(DateTime.Now, LogLevel.Trace, "Cat", "Msg", null);

        Assert.False(entry.IsError());
    }

    #endregion

    #region IsWarning Extension Tests

    [Fact]
    public void IsWarning_ReturnsTrue_ForWarning()
    {
        var entry = new LogEntry(DateTime.Now, LogLevel.Warning, "Cat", "Msg", null);

        Assert.True(entry.IsWarning());
    }

    [Fact]
    public void IsWarning_ReturnsFalse_ForError()
    {
        var entry = new LogEntry(DateTime.Now, LogLevel.Error, "Cat", "Msg", null);

        Assert.False(entry.IsWarning());
    }

    [Fact]
    public void IsWarning_ReturnsFalse_ForInfo()
    {
        var entry = new LogEntry(DateTime.Now, LogLevel.Info, "Cat", "Msg", null);

        Assert.False(entry.IsWarning());
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void Equality_MatchesWhenAllPropertiesMatch()
    {
        var timestamp = DateTime.Now;
        var entry1 = new LogEntry(timestamp, LogLevel.Info, "Cat", "Msg", null);
        var entry2 = new LogEntry(timestamp, LogLevel.Info, "Cat", "Msg", null);

        Assert.Equal(entry1, entry2);
    }

    [Fact]
    public void Equality_DiffersWhenTimestampDiffers()
    {
        var entry1 = new LogEntry(DateTime.Now, LogLevel.Info, "Cat", "Msg", null);
        var entry2 = new LogEntry(DateTime.Now.AddSeconds(1), LogLevel.Info, "Cat", "Msg", null);

        Assert.NotEqual(entry1, entry2);
    }

    [Fact]
    public void Equality_DiffersWhenLevelDiffers()
    {
        var timestamp = DateTime.Now;
        var entry1 = new LogEntry(timestamp, LogLevel.Info, "Cat", "Msg", null);
        var entry2 = new LogEntry(timestamp, LogLevel.Warning, "Cat", "Msg", null);

        Assert.NotEqual(entry1, entry2);
    }

    [Fact]
    public void Equality_DiffersWhenCategoryDiffers()
    {
        var timestamp = DateTime.Now;
        var entry1 = new LogEntry(timestamp, LogLevel.Info, "Cat1", "Msg", null);
        var entry2 = new LogEntry(timestamp, LogLevel.Info, "Cat2", "Msg", null);

        Assert.NotEqual(entry1, entry2);
    }

    [Fact]
    public void Equality_DiffersWhenMessageDiffers()
    {
        var timestamp = DateTime.Now;
        var entry1 = new LogEntry(timestamp, LogLevel.Info, "Cat", "Msg1", null);
        var entry2 = new LogEntry(timestamp, LogLevel.Info, "Cat", "Msg2", null);

        Assert.NotEqual(entry1, entry2);
    }

    #endregion
}
