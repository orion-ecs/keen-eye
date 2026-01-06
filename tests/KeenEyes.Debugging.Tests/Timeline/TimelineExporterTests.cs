using System.Text.Json;
using KeenEyes.Debugging.Timeline;

namespace KeenEyes.Debugging.Tests.Timeline;

/// <summary>
/// Unit tests for the <see cref="TimelineExporter"/> class.
/// </summary>
public class TimelineExporterTests
{
    private static readonly TimelineEntry sampleEntry = new()
    {
        FrameNumber = 42,
        SystemName = "MovementSystem",
        Phase = SystemPhase.Update,
        StartTicks = 1000,
        Duration = TimeSpan.FromMilliseconds(1.5),
        DeltaTime = 0.016f
    };

    #region ToJson Tests

    [Fact]
    public void ToJson_EmptyList_ReturnsEmptyArray()
    {
        // Act
        var json = TimelineExporter.ToJson([]);

        // Assert
        Assert.Equal("[]", json);
    }

    [Fact]
    public void ToJson_SingleEntry_ReturnsValidJson()
    {
        // Arrange
        var entries = new List<TimelineEntry> { sampleEntry };

        // Act
        var json = TimelineExporter.ToJson(entries);

        // Assert
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        Assert.Single(root.EnumerateArray());

        var entry = root[0];
        Assert.Equal(42, entry.GetProperty("FrameNumber").GetInt64());
        Assert.Equal("MovementSystem", entry.GetProperty("SystemName").GetString());
        Assert.Equal("Update", entry.GetProperty("Phase").GetString());
        Assert.Equal(1000, entry.GetProperty("StartTicks").GetInt64());
        Assert.True(entry.GetProperty("DurationMs").GetDouble() > 0);
        Assert.True(entry.GetProperty("DeltaTime").GetSingle() > 0);
    }

    [Fact]
    public void ToJson_WithIndentedFalse_ReturnsCompactJson()
    {
        // Arrange
        var entries = new List<TimelineEntry> { sampleEntry };

        // Act
        var json = TimelineExporter.ToJson(entries, indented: false);

        // Assert
        Assert.DoesNotContain("\n", json);
    }

    [Fact]
    public void ToJson_WithIndentedTrue_ReturnsFormattedJson()
    {
        // Arrange
        var entries = new List<TimelineEntry> { sampleEntry };

        // Act
        var json = TimelineExporter.ToJson(entries, indented: true);

        // Assert
        Assert.Contains("\n", json);
    }

    [Fact]
    public void ToJson_MultipleEntries_ReturnsAllEntries()
    {
        // Arrange
        var entries = new List<TimelineEntry>
        {
            sampleEntry,
            new()
            {
                FrameNumber = 43,
                SystemName = "RenderSystem",
                Phase = SystemPhase.Render,
                StartTicks = 2000,
                Duration = TimeSpan.FromMilliseconds(2.5),
                DeltaTime = 0.016f
            }
        };

        // Act
        var json = TimelineExporter.ToJson(entries);

        // Assert
        var doc = JsonDocument.Parse(json);
        Assert.Equal(2, doc.RootElement.GetArrayLength());
    }

    #endregion

    #region ToCsv Tests

    [Fact]
    public void ToCsv_EmptyList_ReturnsHeaderOnly()
    {
        // Act
        var csv = TimelineExporter.ToCsv([]);

        // Assert
        var lines = csv.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Single(lines);
        Assert.Equal("FrameNumber,SystemName,Phase,StartTicks,DurationMs,DeltaTime", lines[0]);
    }

    [Fact]
    public void ToCsv_SingleEntry_ReturnsHeaderAndRow()
    {
        // Arrange
        var entries = new List<TimelineEntry> { sampleEntry };

        // Act
        var csv = TimelineExporter.ToCsv(entries);

        // Assert
        var lines = csv.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Contains("42", lines[1]);
        Assert.Contains("MovementSystem", lines[1]);
        Assert.Contains("Update", lines[1]);
        Assert.Contains("1000", lines[1]);
    }

    [Fact]
    public void ToCsv_SystemNameWithComma_EscapesCorrectly()
    {
        // Arrange
        var entry = new TimelineEntry
        {
            FrameNumber = 1,
            SystemName = "System,WithComma",
            Phase = SystemPhase.Update,
            StartTicks = 100,
            Duration = TimeSpan.FromMilliseconds(1),
            DeltaTime = 0.016f
        };

        // Act
        var csv = TimelineExporter.ToCsv([entry]);

        // Assert
        Assert.Contains("\"System,WithComma\"", csv);
    }

    [Fact]
    public void ToCsv_SystemNameWithQuote_EscapesCorrectly()
    {
        // Arrange
        var entry = new TimelineEntry
        {
            FrameNumber = 1,
            SystemName = "System\"WithQuote",
            Phase = SystemPhase.Update,
            StartTicks = 100,
            Duration = TimeSpan.FromMilliseconds(1),
            DeltaTime = 0.016f
        };

        // Act
        var csv = TimelineExporter.ToCsv([entry]);

        // Assert
        Assert.Contains("\"System\"\"WithQuote\"", csv);
    }

    #endregion

    #region StatsToJson Tests

    [Fact]
    public void StatsToJson_EmptyDictionary_ReturnsEmptyArray()
    {
        // Act
        var json = TimelineExporter.StatsToJson(new Dictionary<string, TimelineSystemStats>());

        // Assert
        Assert.Equal("[]", json);
    }

    [Fact]
    public void StatsToJson_SingleStat_ReturnsValidJson()
    {
        // Arrange
        var stats = new Dictionary<string, TimelineSystemStats>
        {
            ["MovementSystem"] = new TimelineSystemStats
            {
                SystemName = "MovementSystem",
                CallCount = 100,
                TotalTime = TimeSpan.FromMilliseconds(50),
                AverageTime = TimeSpan.FromMilliseconds(0.5),
                MinTime = TimeSpan.FromMilliseconds(0.1),
                MaxTime = TimeSpan.FromMilliseconds(2.0)
            }
        };

        // Act
        var json = TimelineExporter.StatsToJson(stats);

        // Assert
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Single(root.EnumerateArray());

        var stat = root[0];
        Assert.Equal("MovementSystem", stat.GetProperty("SystemName").GetString());
        Assert.Equal(100, stat.GetProperty("CallCount").GetInt32());
        Assert.Equal(50.0, stat.GetProperty("TotalTimeMs").GetDouble());
        Assert.Equal(0.5, stat.GetProperty("AverageTimeMs").GetDouble());
        Assert.Equal(0.1, stat.GetProperty("MinTimeMs").GetDouble());
        Assert.Equal(2.0, stat.GetProperty("MaxTimeMs").GetDouble());
    }

    [Fact]
    public void StatsToJson_WithIndentedFalse_ReturnsCompactJson()
    {
        // Arrange
        var stats = new Dictionary<string, TimelineSystemStats>
        {
            ["System"] = new TimelineSystemStats
            {
                SystemName = "System",
                CallCount = 1,
                TotalTime = TimeSpan.Zero,
                AverageTime = TimeSpan.Zero,
                MinTime = TimeSpan.Zero,
                MaxTime = TimeSpan.Zero
            }
        };

        // Act
        var json = TimelineExporter.StatsToJson(stats, indented: false);

        // Assert
        Assert.DoesNotContain("\n", json);
    }

    #endregion

    #region StatsToCsv Tests

    [Fact]
    public void StatsToCsv_EmptyDictionary_ReturnsHeaderOnly()
    {
        // Act
        var csv = TimelineExporter.StatsToCsv(new Dictionary<string, TimelineSystemStats>());

        // Assert
        var lines = csv.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Single(lines);
        Assert.Equal("SystemName,CallCount,TotalTimeMs,AverageTimeMs,MinTimeMs,MaxTimeMs", lines[0]);
    }

    [Fact]
    public void StatsToCsv_SingleStat_ReturnsHeaderAndRow()
    {
        // Arrange
        var stats = new Dictionary<string, TimelineSystemStats>
        {
            ["MovementSystem"] = new TimelineSystemStats
            {
                SystemName = "MovementSystem",
                CallCount = 100,
                TotalTime = TimeSpan.FromMilliseconds(50),
                AverageTime = TimeSpan.FromMilliseconds(0.5),
                MinTime = TimeSpan.FromMilliseconds(0.1),
                MaxTime = TimeSpan.FromMilliseconds(2.0)
            }
        };

        // Act
        var csv = TimelineExporter.StatsToCsv(stats);

        // Assert
        var lines = csv.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Contains("MovementSystem", lines[1]);
        Assert.Contains("100", lines[1]);
    }

    #endregion

    #region GenerateReport Tests

    [Fact]
    public void GenerateReport_EmptyRecorder_ReturnsBasicReport()
    {
        // Arrange
        var recorder = new TimelineRecorder();

        // Act
        var report = TimelineExporter.GenerateReport(recorder);

        // Assert
        Assert.Contains("=== Timeline Report ===", report);
        Assert.Contains("Current Frame: 0", report);
        Assert.Contains("Total Entries: 0", report);
        Assert.Contains("Recording: Enabled", report);
        Assert.Contains("No timeline entries recorded", report);
    }

    [Fact]
    public void GenerateReport_WithEntries_IncludesStatistics()
    {
        // Arrange
        var recorder = new TimelineRecorder();
        recorder.BeginRecording("System1");
        Thread.Sleep(1);
        recorder.EndRecording("System1", 0.016f);
        recorder.BeginRecording("System2");
        Thread.Sleep(1);
        recorder.EndRecording("System2", 0.016f);

        // Act
        var report = TimelineExporter.GenerateReport(recorder);

        // Assert
        Assert.Contains("System Statistics:", report);
        Assert.Contains("System1", report);
        Assert.Contains("System2", report);
        Assert.Contains("Calls", report);
        Assert.Contains("Avg (ms)", report);
    }

    [Fact]
    public void GenerateReport_DisabledRecording_ShowsDisabled()
    {
        // Arrange
        var recorder = new TimelineRecorder();
        recorder.DisableRecording();

        // Act
        var report = TimelineExporter.GenerateReport(recorder);

        // Assert
        Assert.Contains("Recording: Disabled", report);
    }

    [Fact]
    public void GenerateReport_OrdersSystemsByTotalTime()
    {
        // Arrange
        var recorder = new TimelineRecorder();

        recorder.BeginRecording("FastSystem");
        recorder.EndRecording("FastSystem", 0.016f);

        recorder.BeginRecording("SlowSystem");
        Thread.Sleep(10);
        recorder.EndRecording("SlowSystem", 0.016f);

        // Act
        var report = TimelineExporter.GenerateReport(recorder);

        // Assert
        var slowIndex = report.IndexOf("SlowSystem");
        var fastIndex = report.IndexOf("FastSystem");
        Assert.True(slowIndex < fastIndex, "SlowSystem should appear before FastSystem (sorted by total time descending)");
    }

    #endregion
}
