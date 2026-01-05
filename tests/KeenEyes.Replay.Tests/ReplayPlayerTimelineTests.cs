using KeenEyes.Serialization;

namespace KeenEyes.Replay.Tests;

/// <summary>
/// Unit tests for ReplayPlayer timeline navigation features.
/// </summary>
public class ReplayPlayerTimelineTests
{
    #region Test Helpers

    /// <summary>
    /// Creates a test replay with the specified number of frames.
    /// Each frame has a delta time of 16ms (~60 FPS).
    /// </summary>
    private static ReplayData CreateTestReplay(int frameCount, int snapshotInterval = 60)
    {
        var frames = new List<ReplayFrame>();
        var snapshots = new List<SnapshotMarker>();
        var elapsed = TimeSpan.Zero;
        var deltaTime = TimeSpan.FromMilliseconds(16);

        for (int i = 0; i < frameCount; i++)
        {
            int? precedingSnapshotIndex = null;

            // Create snapshot at intervals
            if (i % snapshotInterval == 0)
            {
                precedingSnapshotIndex = snapshots.Count;
                snapshots.Add(new SnapshotMarker
                {
                    FrameNumber = i,
                    ElapsedTime = elapsed,
                    Snapshot = CreateEmptySnapshot()
                });
            }

            frames.Add(new ReplayFrame
            {
                FrameNumber = i,
                DeltaTime = deltaTime,
                ElapsedTime = elapsed,
                Events = [],
                PrecedingSnapshotIndex = precedingSnapshotIndex
            });

            elapsed += deltaTime;
        }

        return new ReplayData
        {
            Version = ReplayData.CurrentVersion,
            Name = "Test Replay",
            RecordingStarted = DateTimeOffset.UtcNow,
            Duration = elapsed,
            FrameCount = frameCount,
            Frames = frames,
            Snapshots = snapshots
        };
    }

    private static WorldSnapshot CreateEmptySnapshot()
    {
        return new WorldSnapshot
        {
            Version = 1,
            Timestamp = DateTimeOffset.UtcNow,
            Entities = [],
            Singletons = []
        };
    }

    #endregion

    #region Step Tests

    [Fact]
    public void Step_ForwardOneFrame_AdvancesPosition()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));

        // Act
        player.Step();

        // Assert
        Assert.Equal(1, player.CurrentFrame);
        Assert.Equal(PlaybackState.Stopped, player.State);
    }

    [Fact]
    public void Step_ForwardMultipleFrames_AdvancesPosition()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));

        // Act
        player.Step(10);

        // Assert
        Assert.Equal(10, player.CurrentFrame);
    }

    [Fact]
    public void Step_BackwardOneFrame_MovesBack()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));
        player.SeekToFrame(50);

        // Act
        player.Step(-1);

        // Assert
        Assert.Equal(49, player.CurrentFrame);
    }

    [Fact]
    public void Step_BackwardMultipleFrames_MovesBack()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));
        player.SeekToFrame(50);

        // Act
        player.Step(-10);

        // Assert
        Assert.Equal(40, player.CurrentFrame);
    }

    [Fact]
    public void Step_BeyondEnd_ClampsToLastFrame()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));

        // Act
        player.Step(200);

        // Assert
        Assert.Equal(99, player.CurrentFrame);
    }

    [Fact]
    public void Step_BeyondBeginning_ClampsToFirstFrame()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));
        player.SeekToFrame(10);

        // Act
        player.Step(-20);

        // Assert
        Assert.Equal(0, player.CurrentFrame);
    }

    [Fact]
    public void Step_WhilePlaying_PausesPlayback()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));
        player.Play();
        Assert.Equal(PlaybackState.Playing, player.State);

        // Act
        player.Step();

        // Assert
        Assert.Equal(PlaybackState.Paused, player.State);
    }

    [Fact]
    public void Step_WithZeroFrames_NoChange()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));
        player.SeekToFrame(50);

        // Act
        player.Step(0);

        // Assert
        Assert.Equal(50, player.CurrentFrame);
    }

    [Fact]
    public void Step_UpdatesCurrentTime()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));

        // Act
        player.Step(10);

        // Assert
        var expectedTime = TimeSpan.FromMilliseconds(16 * 10);
        Assert.Equal(expectedTime, player.CurrentTime);
    }

    [Fact]
    public void Step_WhenNoReplayLoaded_ThrowsInvalidOperationException()
    {
        // Arrange
        using var player = new ReplayPlayer();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => player.Step());
    }

    [Fact]
    public void Step_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));
        player.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => player.Step());
    }

    [Fact]
    public void Step_WithEmptyReplay_DoesNotThrow()
    {
        // Arrange
        using var player = new ReplayPlayer();
        var emptyReplay = new ReplayData
        {
            Version = ReplayData.CurrentVersion,
            Name = "Empty",
            RecordingStarted = DateTimeOffset.UtcNow,
            Duration = TimeSpan.Zero,
            FrameCount = 0,
            Frames = [],
            Snapshots = []
        };
        player.LoadReplay(emptyReplay);

        // Act - should not throw
        player.Step();

        // Assert
        Assert.Equal(0, player.CurrentFrame);
    }

    #endregion

    #region SeekToFrame Tests

    [Fact]
    public void SeekToFrame_ToValidFrame_UpdatesPosition()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));

        // Act
        player.SeekToFrame(50);

        // Assert
        Assert.Equal(50, player.CurrentFrame);
    }

    [Fact]
    public void SeekToFrame_ToFirstFrame_UpdatesPosition()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));
        player.SeekToFrame(50);

        // Act
        player.SeekToFrame(0);

        // Assert
        Assert.Equal(0, player.CurrentFrame);
        Assert.Equal(TimeSpan.Zero, player.CurrentTime);
    }

    [Fact]
    public void SeekToFrame_ToLastFrame_UpdatesPosition()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));

        // Act
        player.SeekToFrame(99);

        // Assert
        Assert.Equal(99, player.CurrentFrame);
    }

    [Fact]
    public void SeekToFrame_UpdatesCurrentTime()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));

        // Act
        player.SeekToFrame(30);

        // Assert
        var expectedTime = TimeSpan.FromMilliseconds(16 * 30);
        Assert.Equal(expectedTime, player.CurrentTime);
    }

    [Fact]
    public void SeekToFrame_WhilePlaying_PausesPlayback()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));
        player.Play();

        // Act
        player.SeekToFrame(50);

        // Assert
        Assert.Equal(PlaybackState.Paused, player.State);
    }

    [Fact]
    public void SeekToFrame_NegativeFrame_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));

        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => player.SeekToFrame(-1));
        Assert.Equal("frameNumber", ex.ParamName);
    }

    [Fact]
    public void SeekToFrame_BeyondLastFrame_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));

        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => player.SeekToFrame(100));
        Assert.Equal("frameNumber", ex.ParamName);
    }

    [Fact]
    public void SeekToFrame_WhenNoReplayLoaded_ThrowsInvalidOperationException()
    {
        // Arrange
        using var player = new ReplayPlayer();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => player.SeekToFrame(0));
    }

    [Fact]
    public void SeekToFrame_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));
        player.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => player.SeekToFrame(0));
    }

    [Fact]
    public void SeekToFrame_ResetsAccumulatedTime()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));
        player.Play();
        player.Update(0.008f); // Accumulate some time

        // Act
        player.SeekToFrame(50);
        player.Play();

        // After seeking, the first Update should not advance immediately
        // because accumulated time was reset
        var initialFrame = player.CurrentFrame;
        player.Update(0.001f); // Very small update, shouldn't advance frame

        // Assert
        Assert.Equal(initialFrame, player.CurrentFrame);
    }

    #endregion

    #region SeekToTime Tests

    [Fact]
    public void SeekToTime_ToValidTime_UpdatesPosition()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));

        // Act - Seek to ~500ms (frame 31 at 16ms per frame)
        player.SeekToTime(TimeSpan.FromMilliseconds(500));

        // Assert
        Assert.Equal(31, player.CurrentFrame); // 500/16 = 31.25, floor = 31
    }

    [Fact]
    public void SeekToTime_ToZero_SeeksToFirstFrame()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));
        player.SeekToFrame(50);

        // Act
        player.SeekToTime(TimeSpan.Zero);

        // Assert
        Assert.Equal(0, player.CurrentFrame);
    }

    [Fact]
    public void SeekToTime_ToExactFrameTime_SeeksToThatFrame()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));

        // Act - Seek to exact time of frame 10
        var frameTime = TimeSpan.FromMilliseconds(16 * 10);
        player.SeekToTime(frameTime);

        // Assert
        Assert.Equal(10, player.CurrentFrame);
    }

    [Fact]
    public void SeekToTime_BetweenFrames_SeeksToEarlierFrame()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));

        // Act - Seek to time between frame 10 and 11
        var timeBetweenFrames = TimeSpan.FromMilliseconds(16 * 10 + 8);
        player.SeekToTime(timeBetweenFrames);

        // Assert - Should select frame 10 (at or before the time)
        Assert.Equal(10, player.CurrentFrame);
    }

    [Fact]
    public void SeekToTime_ToDuration_SeeksToLastFrame()
    {
        // Arrange
        using var player = new ReplayPlayer();
        var replay = CreateTestReplay(100);
        player.LoadReplay(replay);

        // Act
        player.SeekToTime(replay.Duration);

        // Assert
        Assert.Equal(99, player.CurrentFrame);
    }

    [Fact]
    public void SeekToTime_WhilePlaying_PausesPlayback()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));
        player.Play();

        // Act
        player.SeekToTime(TimeSpan.FromSeconds(1));

        // Assert
        Assert.Equal(PlaybackState.Paused, player.State);
    }

    [Fact]
    public void SeekToTime_NegativeTime_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));

        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => player.SeekToTime(TimeSpan.FromSeconds(-1)));
        Assert.Equal("time", ex.ParamName);
    }

    [Fact]
    public void SeekToTime_BeyondDuration_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        using var player = new ReplayPlayer();
        var replay = CreateTestReplay(100);
        player.LoadReplay(replay);

        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => player.SeekToTime(replay.Duration + TimeSpan.FromSeconds(1)));
        Assert.Equal("time", ex.ParamName);
    }

    [Fact]
    public void SeekToTime_WhenNoReplayLoaded_ThrowsInvalidOperationException()
    {
        // Arrange
        using var player = new ReplayPlayer();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => player.SeekToTime(TimeSpan.Zero));
    }

    [Fact]
    public void SeekToTime_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));
        player.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => player.SeekToTime(TimeSpan.Zero));
    }

    #endregion

    #region GetNearestSnapshot Tests

    [Fact]
    public void GetNearestSnapshot_AtSnapshotFrame_ReturnsThatSnapshot()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(200, snapshotInterval: 60));

        // Act
        var snapshot = player.GetNearestSnapshot(60);

        // Assert
        Assert.NotNull(snapshot);
        Assert.Equal(60, snapshot.FrameNumber);
    }

    [Fact]
    public void GetNearestSnapshot_BetweenSnapshots_ReturnsEarlierSnapshot()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(200, snapshotInterval: 60));

        // Act - Frame 90 is between snapshots at 60 and 120
        var snapshot = player.GetNearestSnapshot(90);

        // Assert
        Assert.NotNull(snapshot);
        Assert.Equal(60, snapshot.FrameNumber);
    }

    [Fact]
    public void GetNearestSnapshot_BeforeFirstSnapshot_ReturnsFirstSnapshot()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(200, snapshotInterval: 60));

        // Act - Frame 30 is before any snapshot (first is at 0)
        var snapshot = player.GetNearestSnapshot(30);

        // Assert
        Assert.NotNull(snapshot);
        Assert.Equal(0, snapshot.FrameNumber);
    }

    [Fact]
    public void GetNearestSnapshot_AtFirstFrame_ReturnsFirstSnapshot()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(200, snapshotInterval: 60));

        // Act
        var snapshot = player.GetNearestSnapshot(0);

        // Assert
        Assert.NotNull(snapshot);
        Assert.Equal(0, snapshot.FrameNumber);
    }

    [Fact]
    public void GetNearestSnapshot_AtLastFrame_ReturnsLastApplicableSnapshot()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(200, snapshotInterval: 60));

        // Act
        var snapshot = player.GetNearestSnapshot(199);

        // Assert
        Assert.NotNull(snapshot);
        Assert.Equal(180, snapshot.FrameNumber); // Last snapshot before 199
    }

    [Fact]
    public void GetNearestSnapshot_WithNoSnapshots_ReturnsNull()
    {
        // Arrange
        using var player = new ReplayPlayer();
        var replay = new ReplayData
        {
            Version = ReplayData.CurrentVersion,
            Name = "No Snapshots",
            RecordingStarted = DateTimeOffset.UtcNow,
            Duration = TimeSpan.FromSeconds(1),
            FrameCount = 60,
            Frames = Enumerable.Range(0, 60).Select(i => new ReplayFrame
            {
                FrameNumber = i,
                DeltaTime = TimeSpan.FromMilliseconds(16),
                ElapsedTime = TimeSpan.FromMilliseconds(16 * i),
                Events = []
            }).ToList(),
            Snapshots = [] // No snapshots
        };
        player.LoadReplay(replay);

        // Act
        var snapshot = player.GetNearestSnapshot(30);

        // Assert
        Assert.Null(snapshot);
    }

    [Fact]
    public void GetNearestSnapshot_WhenNoReplayLoaded_ThrowsInvalidOperationException()
    {
        // Arrange
        using var player = new ReplayPlayer();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => player.GetNearestSnapshot(0));
    }

    [Fact]
    public void GetNearestSnapshot_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));
        player.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => player.GetNearestSnapshot(0));
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void SeekAndPlay_ContinuesFromSeekPosition()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));

        // Act
        player.SeekToFrame(50);
        player.Play();
        player.Update(0.020f); // 20ms, should advance 1 frame (16ms per frame)

        // Assert
        Assert.Equal(51, player.CurrentFrame);
    }

    [Fact]
    public void StepAndSeek_WorkTogether()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));

        // Act
        player.Step(10);
        player.SeekToFrame(50);
        player.Step(-5);

        // Assert
        Assert.Equal(45, player.CurrentFrame);
    }

    [Fact]
    public void SeekToTime_AndStepBack_WorksCorrectly()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));

        // Act
        player.SeekToTime(TimeSpan.FromMilliseconds(800)); // Frame ~50
        var frameAfterSeek = player.CurrentFrame;
        player.Step(-10);

        // Assert
        Assert.Equal(frameAfterSeek - 10, player.CurrentFrame);
    }

    [Fact]
    public void MultipleOperations_MaintainConsistentState()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(200, snapshotInterval: 30));

        // Act - Perform various operations
        player.Play();
        player.Update(0.050f);
        player.SeekToFrame(100);
        player.Step(10);
        player.Step(-5);
        var snapshot = player.GetNearestSnapshot(player.CurrentFrame);
        player.SeekToTime(TimeSpan.FromMilliseconds(500));
        player.Stop();

        // Assert
        Assert.Equal(0, player.CurrentFrame);
        Assert.Equal(TimeSpan.Zero, player.CurrentTime);
        Assert.Equal(PlaybackState.Stopped, player.State);
    }

    [Fact]
    public void LargeReplay_SeekPerformance()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(10000, snapshotInterval: 60)); // Large replay

        // Act - Multiple seeks should complete quickly
        for (int i = 0; i < 100; i++)
        {
            player.SeekToFrame(i * 100 % 10000);
            player.GetNearestSnapshot(player.CurrentFrame);
        }

        // Assert - Just verify we can seek without issues
        Assert.True(player.IsLoaded);
    }

    [Fact]
    public void SeekToFrame_AfterStop_ResetsToBeginning()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));
        player.SeekToFrame(50);
        player.Stop();

        // Assert
        Assert.Equal(0, player.CurrentFrame);
        Assert.Equal(TimeSpan.Zero, player.CurrentTime);
    }

    [Fact]
    public void SeekToFrame_PreservesLoadedReplay()
    {
        // Arrange
        using var player = new ReplayPlayer();
        var replay = CreateTestReplay(100);
        player.LoadReplay(replay);

        // Act
        player.SeekToFrame(50);
        player.Step(10);
        player.SeekToTime(TimeSpan.FromMilliseconds(200));

        // Assert
        Assert.Same(replay, player.LoadedReplay);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Step_WithSingleFrameReplay_StaysAtFrame()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(1));

        // Act
        player.Step(10);

        // Assert
        Assert.Equal(0, player.CurrentFrame);
    }

    [Fact]
    public void SeekToFrame_WithSingleFrameReplay_SeeksToZero()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(1));

        // Act & Assert - Only frame 0 is valid
        player.SeekToFrame(0);
        Assert.Equal(0, player.CurrentFrame);
    }

    [Fact]
    public void SeekToTime_WithVerySmallDuration_WorksCorrectly()
    {
        // Arrange
        using var player = new ReplayPlayer();
        var replay = CreateTestReplay(2); // 2 frames, 32ms total
        player.LoadReplay(replay);

        // Act
        player.SeekToTime(TimeSpan.FromMilliseconds(16)); // Exactly at frame 1

        // Assert
        Assert.Equal(1, player.CurrentFrame);
    }

    [Fact]
    public void GetNearestSnapshot_WithSingleSnapshot_AlwaysReturnsThatSnapshot()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100, snapshotInterval: 1000)); // Only initial snapshot

        // Act
        var snapshot1 = player.GetNearestSnapshot(0);
        var snapshot50 = player.GetNearestSnapshot(50);
        var snapshot99 = player.GetNearestSnapshot(99);

        // Assert
        Assert.NotNull(snapshot1);
        Assert.NotNull(snapshot50);
        Assert.NotNull(snapshot99);
        Assert.Equal(0, snapshot1.FrameNumber);
        Assert.Equal(0, snapshot50.FrameNumber);
        Assert.Equal(0, snapshot99.FrameNumber);
    }

    #endregion
}
