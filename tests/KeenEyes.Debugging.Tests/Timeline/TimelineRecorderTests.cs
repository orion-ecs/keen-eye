using KeenEyes.Debugging.Timeline;

namespace KeenEyes.Debugging.Tests.Timeline;

/// <summary>
/// Unit tests for the <see cref="TimelineRecorder"/> class.
/// </summary>
public class TimelineRecorderTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithDefaultMaxFrames_Creates300FrameBuffer()
    {
        // Act
        var recorder = new TimelineRecorder();

        // Assert
        Assert.NotNull(recorder);
        Assert.Equal(0, recorder.CurrentFrame);
        Assert.Equal(0, recorder.EntryCount);
        Assert.True(recorder.IsRecording);
    }

    [Fact]
    public void Constructor_WithCustomMaxFrames_AcceptsParameter()
    {
        // Act
        var recorder = new TimelineRecorder(maxFramesToKeep: 100);

        // Assert
        Assert.NotNull(recorder);
    }

    #endregion

    #region BeginRecording/EndRecording Tests

    [Fact]
    public void BeginRecording_EndRecording_CreatesEntry()
    {
        // Arrange
        var recorder = new TimelineRecorder();

        // Act
        recorder.BeginRecording("TestSystem");
        Thread.Sleep(1); // Small delay to ensure measurable time
        recorder.EndRecording("TestSystem", 0.016f);

        // Assert
        Assert.Equal(1, recorder.EntryCount);
        var entries = recorder.GetAllEntries();
        Assert.Single(entries);
        Assert.Equal("TestSystem", entries[0].SystemName);
        Assert.Equal(0.016f, entries[0].DeltaTime);
        Assert.True(entries[0].Duration > TimeSpan.Zero);
    }

    [Fact]
    public void EndRecording_WithoutBeginRecording_DoesNothing()
    {
        // Arrange
        var recorder = new TimelineRecorder();

        // Act
        recorder.EndRecording("NonExistentSystem", 0.016f);

        // Assert
        Assert.Equal(0, recorder.EntryCount);
    }

    [Fact]
    public void BeginRecording_Overwrite_ReplacesPreviousRecording()
    {
        // Arrange
        var recorder = new TimelineRecorder();

        // Act
        recorder.BeginRecording("TestSystem");
        Thread.Sleep(50); // Wait a significant amount of time
        recorder.BeginRecording("TestSystem"); // Overwrite - resets the start time
        recorder.EndRecording("TestSystem", 0.016f);

        // Assert
        Assert.Equal(1, recorder.EntryCount);
        var entries = recorder.GetAllEntries();
        // The duration should reflect the second BeginRecording, not the first
        // Since we don't sleep after the second BeginRecording, duration should be very small
        Assert.True(entries[0].Duration < TimeSpan.FromMilliseconds(50),
            $"Expected duration < 50ms, but was {entries[0].Duration.TotalMilliseconds}ms");
    }

    [Fact]
    public void BeginRecording_EndRecording_MultipleSystems_RecordsAll()
    {
        // Arrange
        var recorder = new TimelineRecorder();

        // Act
        recorder.BeginRecording("System1");
        recorder.BeginRecording("System2");
        recorder.EndRecording("System1", 0.016f);
        recorder.EndRecording("System2", 0.016f);

        // Assert
        Assert.Equal(2, recorder.EntryCount);
        var entries = recorder.GetAllEntries();
        Assert.Contains(entries, e => e.SystemName == "System1");
        Assert.Contains(entries, e => e.SystemName == "System2");
    }

    #endregion

    #region Recording Control Tests

    [Fact]
    public void DisableRecording_PreventsNewEntries()
    {
        // Arrange
        var recorder = new TimelineRecorder();

        // Act
        recorder.DisableRecording();
        recorder.BeginRecording("TestSystem");
        recorder.EndRecording("TestSystem", 0.016f);

        // Assert
        Assert.False(recorder.IsRecording);
        Assert.Equal(0, recorder.EntryCount);
    }

    [Fact]
    public void EnableRecording_AllowsNewEntries()
    {
        // Arrange
        var recorder = new TimelineRecorder();
        recorder.DisableRecording();

        // Act
        recorder.EnableRecording();
        recorder.BeginRecording("TestSystem");
        recorder.EndRecording("TestSystem", 0.016f);

        // Assert
        Assert.True(recorder.IsRecording);
        Assert.Equal(1, recorder.EntryCount);
    }

    [Fact]
    public void DisableRecording_PreservesExistingEntries()
    {
        // Arrange
        var recorder = new TimelineRecorder();
        recorder.BeginRecording("ExistingSystem");
        recorder.EndRecording("ExistingSystem", 0.016f);

        // Act
        recorder.DisableRecording();

        // Assert
        Assert.Equal(1, recorder.EntryCount);
    }

    #endregion

    #region Frame Management Tests

    [Fact]
    public void AdvanceFrame_IncrementsFrameCounter()
    {
        // Arrange
        var recorder = new TimelineRecorder();

        // Act
        recorder.AdvanceFrame();
        recorder.AdvanceFrame();
        recorder.AdvanceFrame();

        // Assert
        Assert.Equal(3, recorder.CurrentFrame);
    }

    [Fact]
    public void AdvanceFrame_AssignsCorrectFrameNumber()
    {
        // Arrange
        var recorder = new TimelineRecorder();

        // Act
        recorder.BeginRecording("System1");
        recorder.EndRecording("System1", 0.016f);
        recorder.AdvanceFrame();

        recorder.BeginRecording("System2");
        recorder.EndRecording("System2", 0.016f);

        // Assert
        var entries = recorder.GetAllEntries();
        Assert.Equal(0, entries[0].FrameNumber);
        Assert.Equal(1, entries[1].FrameNumber);
    }

    [Fact]
    public void AdvanceFrame_CleansUpOldEntries()
    {
        // Arrange
        var recorder = new TimelineRecorder(maxFramesToKeep: 3);

        // Act - Create entries across multiple frames
        for (int i = 0; i <= 5; i++)
        {
            recorder.BeginRecording($"System{i}");
            recorder.EndRecording($"System{i}", 0.016f);
            recorder.AdvanceFrame();
        }

        // Assert - Only last 3 frames should remain (frames 3, 4, 5)
        var entries = recorder.GetAllEntries();
        Assert.All(entries, e => Assert.True(e.FrameNumber >= 3));
    }

    #endregion

    #region SetPhase Tests

    [Fact]
    public void SetPhase_AssignsPhaseToSubsequentRecordings()
    {
        // Arrange
        var recorder = new TimelineRecorder();

        // Act
        recorder.SetPhase(SystemPhase.FixedUpdate);
        recorder.BeginRecording("FixedSystem");
        recorder.EndRecording("FixedSystem", 0.016f);

        recorder.SetPhase(SystemPhase.Update);
        recorder.BeginRecording("UpdateSystem");
        recorder.EndRecording("UpdateSystem", 0.016f);

        // Assert
        var entries = recorder.GetAllEntries();
        Assert.Equal(SystemPhase.FixedUpdate, entries[0].Phase);
        Assert.Equal(SystemPhase.Update, entries[1].Phase);
    }

    #endregion

    #region GetEntriesForFrame Tests

    [Fact]
    public void GetEntriesForFrame_ReturnsCorrectEntries()
    {
        // Arrange
        var recorder = new TimelineRecorder();

        recorder.BeginRecording("FrameZeroSystem1");
        recorder.EndRecording("FrameZeroSystem1", 0.016f);
        recorder.BeginRecording("FrameZeroSystem2");
        recorder.EndRecording("FrameZeroSystem2", 0.016f);
        recorder.AdvanceFrame();

        recorder.BeginRecording("FrameOneSystem");
        recorder.EndRecording("FrameOneSystem", 0.016f);

        // Act
        var frameZeroEntries = recorder.GetEntriesForFrame(0);
        var frameOneEntries = recorder.GetEntriesForFrame(1);

        // Assert
        Assert.Equal(2, frameZeroEntries.Count);
        Assert.Single(frameOneEntries);
        Assert.All(frameZeroEntries, e => Assert.Equal(0, e.FrameNumber));
        Assert.All(frameOneEntries, e => Assert.Equal(1, e.FrameNumber));
    }

    [Fact]
    public void GetEntriesForFrame_NonExistentFrame_ReturnsEmpty()
    {
        // Arrange
        var recorder = new TimelineRecorder();

        // Act
        var entries = recorder.GetEntriesForFrame(999);

        // Assert
        Assert.Empty(entries);
    }

    #endregion

    #region GetRecentEntries Tests

    [Fact]
    public void GetRecentEntries_ReturnsLastNEntries()
    {
        // Arrange
        var recorder = new TimelineRecorder();
        for (int i = 0; i < 10; i++)
        {
            recorder.BeginRecording($"System{i}");
            recorder.EndRecording($"System{i}", 0.016f);
        }

        // Act
        var recentEntries = recorder.GetRecentEntries(3);

        // Assert
        Assert.Equal(3, recentEntries.Count);
        Assert.Equal("System7", recentEntries[0].SystemName);
        Assert.Equal("System8", recentEntries[1].SystemName);
        Assert.Equal("System9", recentEntries[2].SystemName);
    }

    [Fact]
    public void GetRecentEntries_RequestMoreThanAvailable_ReturnsAll()
    {
        // Arrange
        var recorder = new TimelineRecorder();
        recorder.BeginRecording("System1");
        recorder.EndRecording("System1", 0.016f);
        recorder.BeginRecording("System2");
        recorder.EndRecording("System2", 0.016f);

        // Act
        var recentEntries = recorder.GetRecentEntries(100);

        // Assert
        Assert.Equal(2, recentEntries.Count);
    }

    #endregion

    #region GetEntriesBySystem Tests

    [Fact]
    public void GetEntriesBySystem_GroupsBySystemName()
    {
        // Arrange
        var recorder = new TimelineRecorder();
        recorder.BeginRecording("SystemA");
        recorder.EndRecording("SystemA", 0.016f);
        recorder.BeginRecording("SystemB");
        recorder.EndRecording("SystemB", 0.016f);
        recorder.BeginRecording("SystemA");
        recorder.EndRecording("SystemA", 0.016f);

        // Act
        var entriesBySystem = recorder.GetEntriesBySystem();

        // Assert
        Assert.Equal(2, entriesBySystem.Count);
        Assert.Equal(2, entriesBySystem["SystemA"].Count);
        Assert.Single(entriesBySystem["SystemB"]);
    }

    #endregion

    #region GetEntriesByPhase Tests

    [Fact]
    public void GetEntriesByPhase_GroupsByPhase()
    {
        // Arrange
        var recorder = new TimelineRecorder();

        recorder.SetPhase(SystemPhase.FixedUpdate);
        recorder.BeginRecording("FixedSystem1");
        recorder.EndRecording("FixedSystem1", 0.016f);
        recorder.BeginRecording("FixedSystem2");
        recorder.EndRecording("FixedSystem2", 0.016f);

        recorder.SetPhase(SystemPhase.Update);
        recorder.BeginRecording("UpdateSystem");
        recorder.EndRecording("UpdateSystem", 0.016f);

        // Act
        var entriesByPhase = recorder.GetEntriesByPhase();

        // Assert
        Assert.Equal(2, entriesByPhase.Count);
        Assert.Equal(2, entriesByPhase[SystemPhase.FixedUpdate].Count);
        Assert.Single(entriesByPhase[SystemPhase.Update]);
    }

    #endregion

    #region GetSystemStats Tests

    [Fact]
    public void GetSystemStats_CalculatesCorrectStatistics()
    {
        // Arrange
        var recorder = new TimelineRecorder();
        for (int i = 0; i < 5; i++)
        {
            recorder.BeginRecording("TestSystem");
            Thread.Sleep(1);
            recorder.EndRecording("TestSystem", 0.016f);
        }

        // Act
        var stats = recorder.GetSystemStats();

        // Assert
        Assert.Single(stats);
        var systemStats = stats["TestSystem"];
        Assert.Equal("TestSystem", systemStats.SystemName);
        Assert.Equal(5, systemStats.CallCount);
        Assert.True(systemStats.TotalTime > TimeSpan.Zero);
        Assert.True(systemStats.AverageTime > TimeSpan.Zero);
        Assert.True(systemStats.MinTime > TimeSpan.Zero);
        Assert.True(systemStats.MaxTime >= systemStats.MinTime);
    }

    [Fact]
    public void GetSystemStats_MultipleSystemsCalculatesIndependently()
    {
        // Arrange
        var recorder = new TimelineRecorder();
        recorder.BeginRecording("FastSystem");
        recorder.EndRecording("FastSystem", 0.016f);

        recorder.BeginRecording("SlowSystem");
        Thread.Sleep(10);
        recorder.EndRecording("SlowSystem", 0.016f);

        // Act
        var stats = recorder.GetSystemStats();

        // Assert
        Assert.Equal(2, stats.Count);
        Assert.True(stats["SlowSystem"].TotalTime > stats["FastSystem"].TotalTime);
    }

    [Fact]
    public void GetSystemStats_NoEntries_ReturnsEmptyDictionary()
    {
        // Arrange
        var recorder = new TimelineRecorder();

        // Act
        var stats = recorder.GetSystemStats();

        // Assert
        Assert.Empty(stats);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ClearsAllEntries()
    {
        // Arrange
        var recorder = new TimelineRecorder();
        recorder.BeginRecording("System1");
        recorder.EndRecording("System1", 0.016f);
        recorder.BeginRecording("System2");
        recorder.EndRecording("System2", 0.016f);
        recorder.AdvanceFrame();
        recorder.AdvanceFrame();

        // Act
        recorder.Reset();

        // Assert
        Assert.Equal(0, recorder.EntryCount);
        Assert.Equal(0, recorder.CurrentFrame);
    }

    [Fact]
    public void Reset_ClearsActiveRecordings()
    {
        // Arrange
        var recorder = new TimelineRecorder();
        recorder.BeginRecording("InProgressSystem");

        // Act
        recorder.Reset();
        recorder.EndRecording("InProgressSystem", 0.016f);

        // Assert
        Assert.Equal(0, recorder.EntryCount);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task ConcurrentRecordings_ThreadSafe()
    {
        // Arrange
        var recorder = new TimelineRecorder();
        var tasks = new List<Task>();
        var ct = TestContext.Current.CancellationToken;

        // Act
        for (int i = 0; i < 10; i++)
        {
            var systemName = $"System{i}";
            tasks.Add(Task.Run(async () =>
            {
                recorder.BeginRecording(systemName);
                await Task.Delay(1, ct);
                recorder.EndRecording(systemName, 0.016f);
            }, ct));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(10, recorder.EntryCount);
    }

    #endregion
}
