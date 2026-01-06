using KeenEyes.Common;
using KeenEyes.Serialization;

namespace KeenEyes.Replay.Tests;

/// <summary>
/// Unit tests for ReplayPlayer playback speed control features.
/// </summary>
public class ReplayPlayerSpeedControlTests
{
    #region Test Helpers

    /// <summary>
    /// Creates a test replay with the specified number of frames.
    /// Each frame has a delta time of 16ms (~60 FPS).
    /// </summary>
    private static ReplayData CreateTestReplay(int frameCount, TimeSpan? frameDelta = null)
    {
        var frames = new List<ReplayFrame>();
        var snapshots = new List<SnapshotMarker>();
        var elapsed = TimeSpan.Zero;
        var deltaTime = frameDelta ?? TimeSpan.FromMilliseconds(16);

        for (int i = 0; i < frameCount; i++)
        {
            int? precedingSnapshotIndex = null;

            // Create snapshot at frame 0
            if (i == 0)
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

    #region PlaybackSpeed Property Tests

    [Fact]
    public void PlaybackSpeed_DefaultValue_IsNormalSpeed()
    {
        // Arrange
        using var player = new ReplayPlayer();

        // Assert
        Assert.Equal(PlaybackSpeeds.NormalSpeed, player.PlaybackSpeed);
    }

    [Fact]
    public void PlaybackSpeed_SetToQuarterSpeed_ReturnsQuarterSpeed()
    {
        // Arrange
        using var player = new ReplayPlayer();

        // Act
        player.PlaybackSpeed = PlaybackSpeeds.QuarterSpeed;

        // Assert
        Assert.Equal(PlaybackSpeeds.QuarterSpeed, player.PlaybackSpeed);
    }

    [Fact]
    public void PlaybackSpeed_SetToHalfSpeed_ReturnsHalfSpeed()
    {
        // Arrange
        using var player = new ReplayPlayer();

        // Act
        player.PlaybackSpeed = PlaybackSpeeds.HalfSpeed;

        // Assert
        Assert.Equal(PlaybackSpeeds.HalfSpeed, player.PlaybackSpeed);
    }

    [Fact]
    public void PlaybackSpeed_SetToDoubleSpeed_ReturnsDoubleSpeed()
    {
        // Arrange
        using var player = new ReplayPlayer();

        // Act
        player.PlaybackSpeed = PlaybackSpeeds.DoubleSpeed;

        // Assert
        Assert.Equal(PlaybackSpeeds.DoubleSpeed, player.PlaybackSpeed);
    }

    [Fact]
    public void PlaybackSpeed_SetToQuadrupleSpeed_ReturnsQuadrupleSpeed()
    {
        // Arrange
        using var player = new ReplayPlayer();

        // Act
        player.PlaybackSpeed = PlaybackSpeeds.QuadrupleSpeed;

        // Assert
        Assert.Equal(PlaybackSpeeds.QuadrupleSpeed, player.PlaybackSpeed);
    }

    [Fact]
    public void PlaybackSpeed_SetToCustomValue_ReturnsCustomValue()
    {
        // Arrange
        using var player = new ReplayPlayer();

        // Act
        player.PlaybackSpeed = 1.5f;

        // Assert
        Assert.Equal(1.5f, player.PlaybackSpeed);
    }

    [Fact]
    public void PlaybackSpeed_BelowMinimum_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        using var player = new ReplayPlayer();

        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => player.PlaybackSpeed = 0.1f);
        Assert.Equal("value", ex.ParamName);
    }

    [Fact]
    public void PlaybackSpeed_AboveMaximum_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        using var player = new ReplayPlayer();

        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => player.PlaybackSpeed = 5.0f);
        Assert.Equal("value", ex.ParamName);
    }

    [Fact]
    public void PlaybackSpeed_ExactlyMinimum_Succeeds()
    {
        // Arrange
        using var player = new ReplayPlayer();

        // Act
        player.PlaybackSpeed = PlaybackSpeeds.MinSpeed;

        // Assert
        Assert.Equal(PlaybackSpeeds.MinSpeed, player.PlaybackSpeed);
    }

    [Fact]
    public void PlaybackSpeed_ExactlyMaximum_Succeeds()
    {
        // Arrange
        using var player = new ReplayPlayer();

        // Act
        player.PlaybackSpeed = PlaybackSpeeds.MaxSpeed;

        // Assert
        Assert.Equal(PlaybackSpeeds.MaxSpeed, player.PlaybackSpeed);
    }

    [Fact]
    public void PlaybackSpeed_Zero_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        using var player = new ReplayPlayer();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => player.PlaybackSpeed = 0f);
    }

    [Fact]
    public void PlaybackSpeed_Negative_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        using var player = new ReplayPlayer();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => player.PlaybackSpeed = -1f);
    }

    #endregion

    #region Slow Motion Tests

    [Fact]
    public void Update_AtHalfSpeed_AdvancesFramesSlower()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100)); // 16ms per frame
        player.PlaybackSpeed = PlaybackSpeeds.HalfSpeed;
        player.Play();

        // Act - 32ms at half speed should advance 1 frame (16ms frame time)
        player.Update(0.032f);

        // Assert
        Assert.Equal(1, player.CurrentFrame);
    }

    [Fact]
    public void Update_AtHalfSpeed_MayNotAdvanceEveryUpdate()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100)); // 16ms per frame
        player.PlaybackSpeed = PlaybackSpeeds.HalfSpeed;
        player.Play();

        // Act - 16ms at half speed = 8ms accumulated, not enough for 16ms frame
        var changed = player.Update(0.016f);

        // Assert
        Assert.False(changed);
        Assert.Equal(0, player.CurrentFrame);
    }

    [Fact]
    public void Update_AtQuarterSpeed_RequiresFourTimesRealTime()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100)); // 16ms per frame
        player.PlaybackSpeed = PlaybackSpeeds.QuarterSpeed;
        player.Play();

        // Act - 64ms at quarter speed = 16ms accumulated
        player.Update(0.064f);

        // Assert
        Assert.Equal(1, player.CurrentFrame);
    }

    [Fact]
    public void Update_AtQuarterSpeed_PartialAccumulation()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100)); // 16ms per frame
        player.PlaybackSpeed = PlaybackSpeeds.QuarterSpeed;
        player.Play();

        // Act - 32ms at quarter speed = 8ms accumulated (not enough)
        player.Update(0.032f);

        // Assert - Should not advance yet
        Assert.Equal(0, player.CurrentFrame);

        // Act - Another 32ms at quarter speed = 8ms more = 16ms total
        player.Update(0.032f);

        // Assert - Now should advance
        Assert.Equal(1, player.CurrentFrame);
    }

    #endregion

    #region Fast Forward Tests

    [Fact]
    public void Update_AtDoubleSpeed_AdvancesMultipleFrames()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100)); // 16ms per frame
        player.PlaybackSpeed = PlaybackSpeeds.DoubleSpeed;
        player.Play();

        // Act - 16ms at double speed = 32ms accumulated = 2 frames
        player.Update(0.016f);

        // Assert
        Assert.Equal(2, player.CurrentFrame);
    }

    [Fact]
    public void Update_AtQuadrupleSpeed_AdvancesFourFrames()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100)); // 16ms per frame
        player.PlaybackSpeed = PlaybackSpeeds.QuadrupleSpeed;
        player.Play();

        // Act - 16ms at 4x speed = 64ms accumulated = 4 frames
        player.Update(0.016f);

        // Assert
        Assert.Equal(4, player.CurrentFrame);
    }

    [Fact]
    public void Update_AtDoubleSpeed_HandlesPartialFrames()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100)); // 16ms per frame
        player.PlaybackSpeed = PlaybackSpeeds.DoubleSpeed;
        player.Play();

        // Act - 12ms at double speed = 24ms accumulated = 1 frame + 8ms leftover
        player.Update(0.012f);

        // Assert
        Assert.Equal(1, player.CurrentFrame);

        // Act - Another 4ms at double speed = 8ms + 8ms = 16ms = 1 more frame
        player.Update(0.004f);

        // Assert
        Assert.Equal(2, player.CurrentFrame);
    }

    [Fact]
    public void Update_AtQuadrupleSpeed_ProcessesFourFramesPerUpdate()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(20)); // 16ms per frame, 20 frames
        player.PlaybackSpeed = PlaybackSpeeds.QuadrupleSpeed;
        player.Play();

        // Act - Multiple updates at 4x speed
        for (int i = 0; i < 5; i++)
        {
            player.Update(0.016f); // Each advances 4 frames
        }

        // Assert - Should be at frame 20 (end) or stopped
        Assert.Equal(PlaybackState.Stopped, player.State);
    }

    #endregion

    #region Normal Speed Tests

    [Fact]
    public void Update_AtNormalSpeed_AdvancesAsRecorded()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100)); // 16ms per frame
        player.PlaybackSpeed = PlaybackSpeeds.NormalSpeed;
        player.Play();

        // Act - 16ms should advance exactly 1 frame
        player.Update(0.016f);

        // Assert
        Assert.Equal(1, player.CurrentFrame);
    }

    [Fact]
    public void Update_AtNormalSpeed_AccumulatesPartialTime()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100)); // 16ms per frame
        player.PlaybackSpeed = PlaybackSpeeds.NormalSpeed;
        player.Play();

        // Act - 8ms should not advance
        player.Update(0.008f);

        // Assert
        Assert.Equal(0, player.CurrentFrame);

        // Act - Another 8ms should complete the frame
        player.Update(0.008f);

        // Assert
        Assert.Equal(1, player.CurrentFrame);
    }

    #endregion

    #region Frame-Perfect Progression Tests

    [Fact]
    public void Update_AtVariousSpeeds_NoFramesSkipped()
    {
        // This test verifies that at any speed, we don't skip frames
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(10)); // 16ms per frame
        player.PlaybackSpeed = PlaybackSpeeds.QuadrupleSpeed;
        player.Play();

        // Act - Track all frames visited
        var visitedFrames = new HashSet<int> { player.CurrentFrame };
        while (player.State == PlaybackState.Playing)
        {
            player.Update(0.004f); // Small updates to catch all frames
            visitedFrames.Add(player.CurrentFrame);
        }

        // Assert - All frames 0-9 should have been visited
        for (int i = 0; i < 10; i++)
        {
            Assert.Contains(i, visitedFrames);
        }
    }

    [Fact]
    public void Update_AtSlowSpeed_NoFramesDuplicated()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(5)); // 16ms per frame
        player.PlaybackSpeed = PlaybackSpeeds.QuarterSpeed;
        player.Play();

        // Act - Track frame changes
        var frameChanges = new List<int>();
        var lastFrame = -1;
        while (player.State == PlaybackState.Playing && frameChanges.Count < 20)
        {
            player.Update(0.016f); // 4ms accumulated at quarter speed
            if (player.CurrentFrame != lastFrame)
            {
                frameChanges.Add(player.CurrentFrame);
                lastFrame = player.CurrentFrame;
            }
        }

        // Assert - Each frame should appear only once in sequence
        Assert.Equal(frameChanges.Distinct().Count(), frameChanges.Count);
    }

    #endregion

    #region Speed Change During Playback Tests

    [Fact]
    public void PlaybackSpeed_ChangedDuringPlayback_TakesEffectImmediately()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100)); // 16ms per frame
        player.Play();
        player.Update(0.008f); // Accumulate 8ms

        // Act - Change to double speed and update
        player.PlaybackSpeed = PlaybackSpeeds.DoubleSpeed;
        player.Update(0.004f); // 4ms * 2 = 8ms accumulated, total 16ms

        // Assert - Should have advanced one frame
        Assert.Equal(1, player.CurrentFrame);
    }

    [Fact]
    public void PlaybackSpeed_ChangedFromSlowToFast_AccumulatesCorrectly()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100)); // 16ms per frame
        player.PlaybackSpeed = PlaybackSpeeds.HalfSpeed;
        player.Play();

        // Act - 16ms at half speed = 8ms accumulated
        player.Update(0.016f);
        Assert.Equal(0, player.CurrentFrame);

        // Change to 4x speed
        player.PlaybackSpeed = PlaybackSpeeds.QuadrupleSpeed;

        // 2ms at 4x = 8ms, total 16ms
        player.Update(0.002f);

        // Assert
        Assert.Equal(1, player.CurrentFrame);
    }

    #endregion

    #region Speed Persistence Tests

    [Fact]
    public void PlaybackSpeed_PersistsAcrossLoadReplay()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.PlaybackSpeed = PlaybackSpeeds.DoubleSpeed;

        // Act - Load a replay
        player.LoadReplay(CreateTestReplay(100));

        // Assert - Speed should persist
        Assert.Equal(PlaybackSpeeds.DoubleSpeed, player.PlaybackSpeed);
    }

    [Fact]
    public void PlaybackSpeed_PersistsAcrossStop()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));
        player.PlaybackSpeed = PlaybackSpeeds.HalfSpeed;
        player.Play();
        player.Update(0.100f);

        // Act
        player.Stop();

        // Assert - Speed should persist
        Assert.Equal(PlaybackSpeeds.HalfSpeed, player.PlaybackSpeed);
    }

    [Fact]
    public void PlaybackSpeed_PersistsAcrossSeek()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));
        player.PlaybackSpeed = PlaybackSpeeds.QuadrupleSpeed;

        // Act
        player.SeekToFrame(50);

        // Assert
        Assert.Equal(PlaybackSpeeds.QuadrupleSpeed, player.PlaybackSpeed);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Update_AtMaxSpeed_CompletesReplayCorrectly()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(10)); // 16ms per frame, 160ms total
        player.PlaybackSpeed = PlaybackSpeeds.MaxSpeed;
        player.Play();

        // Act - 50ms at 4x = 200ms accumulated, enough for all frames
        player.Update(0.050f);

        // Assert
        Assert.Equal(PlaybackState.Stopped, player.State);
    }

    [Fact]
    public void Update_AtMinSpeed_StillAdvancesEventually()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(5)); // 16ms per frame
        player.PlaybackSpeed = PlaybackSpeeds.MinSpeed;
        player.Play();

        // Act - Need 64ms real time for 16ms playback time at 0.25x
        player.Update(0.064f);

        // Assert
        Assert.Equal(1, player.CurrentFrame);
    }

    [Fact]
    public void Update_WithVeryLargeDeltaTime_HandlesCorrectly()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100)); // 16ms per frame
        player.PlaybackSpeed = PlaybackSpeeds.NormalSpeed;
        player.Play();

        // Act - 1 second update at normal speed = ~62 frames
        player.Update(1.0f);

        // Assert - Should advance multiple frames
        Assert.True(player.CurrentFrame > 50);
    }

    [Fact]
    public void Update_WithVerySmallDeltaTime_AccumulatesCorrectly()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100)); // 16ms per frame
        player.PlaybackSpeed = PlaybackSpeeds.NormalSpeed;
        player.Play();

        // Act - Many tiny updates (use 1ms each to avoid floating-point precision issues)
        for (int i = 0; i < 16; i++)
        {
            player.Update(0.001f); // 1ms each = 16ms total
        }

        // Assert
        Assert.Equal(1, player.CurrentFrame);
    }

    [Fact]
    public void PlaybackSpeed_CanBeSetWithoutLoadedReplay()
    {
        // Arrange
        using var player = new ReplayPlayer();

        // Act - Should not throw
        player.PlaybackSpeed = PlaybackSpeeds.DoubleSpeed;

        // Assert
        Assert.Equal(PlaybackSpeeds.DoubleSpeed, player.PlaybackSpeed);
    }

    #endregion

    #region PlaybackSpeeds Constants Tests

    [Fact]
    public void PlaybackSpeeds_Constants_HaveExpectedValues()
    {
        Assert.Equal(0.25f, PlaybackSpeeds.MinSpeed);
        Assert.Equal(4.0f, PlaybackSpeeds.MaxSpeed);
        Assert.Equal(0.25f, PlaybackSpeeds.QuarterSpeed);
        Assert.Equal(0.5f, PlaybackSpeeds.HalfSpeed);
        Assert.Equal(1.0f, PlaybackSpeeds.NormalSpeed);
        Assert.Equal(2.0f, PlaybackSpeeds.DoubleSpeed);
        Assert.Equal(4.0f, PlaybackSpeeds.QuadrupleSpeed);
    }

    [Fact]
    public void PlaybackSpeeds_MinSpeed_EqualsQuarterSpeed()
    {
        Assert.Equal(PlaybackSpeeds.MinSpeed, PlaybackSpeeds.QuarterSpeed);
    }

    [Fact]
    public void PlaybackSpeeds_MaxSpeed_EqualsQuadrupleSpeed()
    {
        Assert.Equal(PlaybackSpeeds.MaxSpeed, PlaybackSpeeds.QuadrupleSpeed);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public void PlaybackSpeed_SetMany_DoesNotDegradePerformance()
    {
        // Arrange
        using var player = new ReplayPlayer();

        // Act - Rapidly change speed many times
        for (int i = 0; i < 10000; i++)
        {
            player.PlaybackSpeed = (i % 2 == 0) ? PlaybackSpeeds.HalfSpeed : PlaybackSpeeds.DoubleSpeed;
        }

        // Assert - Should complete without issues
        Assert.True(player.PlaybackSpeed.ApproximatelyEquals(PlaybackSpeeds.HalfSpeed) ||
                    player.PlaybackSpeed.ApproximatelyEquals(PlaybackSpeeds.DoubleSpeed));
    }

    [Fact]
    public void Update_AtFastSpeed_ProcessesLargeReplayEfficiently()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(1000)); // 1000 frames
        player.PlaybackSpeed = PlaybackSpeeds.QuadrupleSpeed;
        player.Play();

        // Act - Process entire replay quickly
        var startTime = DateTime.UtcNow;
        while (player.State == PlaybackState.Playing)
        {
            player.Update(0.100f); // Large delta times for fast processing
        }
        var elapsed = DateTime.UtcNow - startTime;

        // Assert - Should complete quickly (< 100ms)
        Assert.True(elapsed.TotalMilliseconds < 100, $"Processing took {elapsed.TotalMilliseconds}ms");
    }

    #endregion
}
