using KeenEyes.Serialization;

namespace KeenEyes.Replay.Tests;

/// <summary>
/// Unit tests for ReplayPlayer event system.
/// </summary>
public class ReplayPlayerEventTests
{
    #region Test Helpers

    /// <summary>
    /// Creates a test replay with the specified number of frames.
    /// Each frame has a delta time of 16ms (~60 FPS).
    /// </summary>
    private static ReplayData CreateTestReplay(int frameCount)
    {
        var frames = new List<ReplayFrame>();
        var snapshots = new List<SnapshotMarker>();
        var elapsed = TimeSpan.Zero;
        var deltaTime = TimeSpan.FromMilliseconds(16);

        for (int i = 0; i < frameCount; i++)
        {
            int? precedingSnapshotIndex = null;

            // Create snapshot at first frame
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

    #region PlaybackStarted Event Tests

    [Fact]
    public void Play_FromStopped_FiresPlaybackStarted()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));

        var eventFired = false;
        player.PlaybackStarted += () => eventFired = true;

        // Act
        player.Play();

        // Assert
        Assert.True(eventFired);
    }

    [Fact]
    public void Play_FromPaused_FiresPlaybackStarted()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));
        player.Play();
        player.Pause();

        var eventFired = false;
        player.PlaybackStarted += () => eventFired = true;

        // Act
        player.Play();

        // Assert
        Assert.True(eventFired);
    }

    [Fact]
    public void Play_WhenAlreadyPlaying_DoesNotFirePlaybackStarted()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));
        player.Play();

        var eventCount = 0;
        player.PlaybackStarted += () => eventCount++;

        // Act
        player.Play();
        player.Play();
        player.Play();

        // Assert
        Assert.Equal(0, eventCount);
    }

    #endregion

    #region PlaybackPaused Event Tests

    [Fact]
    public void Pause_WhenPlaying_FiresPlaybackPaused()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));
        player.Play();

        var eventFired = false;
        player.PlaybackPaused += () => eventFired = true;

        // Act
        player.Pause();

        // Assert
        Assert.True(eventFired);
    }

    [Fact]
    public void Pause_WhenAlreadyPaused_DoesNotFirePlaybackPaused()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));
        player.Play();
        player.Pause();

        var eventCount = 0;
        player.PlaybackPaused += () => eventCount++;

        // Act
        player.Pause();
        player.Pause();

        // Assert
        Assert.Equal(0, eventCount);
    }

    [Fact]
    public void Pause_WhenStopped_DoesNotFirePlaybackPaused()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));

        var eventFired = false;
        player.PlaybackPaused += () => eventFired = true;

        // Act
        player.Pause();

        // Assert
        Assert.False(eventFired);
    }

    [Fact]
    public void Step_WhenPlaying_FiresPlaybackPaused()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));
        player.Play();

        var eventFired = false;
        player.PlaybackPaused += () => eventFired = true;

        // Act
        player.Step();

        // Assert
        Assert.True(eventFired);
    }

    [Fact]
    public void SeekToFrame_WhenPlaying_FiresPlaybackPaused()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));
        player.Play();

        var eventFired = false;
        player.PlaybackPaused += () => eventFired = true;

        // Act
        player.SeekToFrame(50);

        // Assert
        Assert.True(eventFired);
    }

    [Fact]
    public void SeekToTime_WhenPlaying_FiresPlaybackPaused()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));
        player.Play();

        var eventFired = false;
        player.PlaybackPaused += () => eventFired = true;

        // Act
        player.SeekToTime(TimeSpan.FromMilliseconds(500));

        // Assert
        Assert.True(eventFired);
    }

    #endregion

    #region PlaybackStopped Event Tests

    [Fact]
    public void Stop_WhenPlaying_FiresPlaybackStopped()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));
        player.Play();

        var eventFired = false;
        player.PlaybackStopped += () => eventFired = true;

        // Act
        player.Stop();

        // Assert
        Assert.True(eventFired);
    }

    [Fact]
    public void Stop_WhenPaused_FiresPlaybackStopped()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));
        player.Play();
        player.Pause();

        var eventFired = false;
        player.PlaybackStopped += () => eventFired = true;

        // Act
        player.Stop();

        // Assert
        Assert.True(eventFired);
    }

    [Fact]
    public void Stop_WhenAlreadyStopped_DoesNotFirePlaybackStopped()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));

        var eventCount = 0;
        player.PlaybackStopped += () => eventCount++;

        // Act
        player.Stop();
        player.Stop();

        // Assert
        Assert.Equal(0, eventCount);
    }

    #endregion

    #region PlaybackEnded Event Tests

    [Fact]
    public void Update_WhenReachesEnd_FiresPlaybackEnded()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(3)); // Small replay
        player.Play();

        var eventFired = false;
        player.PlaybackEnded += () => eventFired = true;

        // Act - Update with enough time to complete all frames
        player.Update(1.0f); // 1 second, way more than 3 frames at 16ms each

        // Assert
        Assert.True(eventFired);
        Assert.Equal(PlaybackState.Stopped, player.State);
    }

    [Fact]
    public void Update_WhenNotAtEnd_DoesNotFirePlaybackEnded()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));
        player.Play();

        var eventFired = false;
        player.PlaybackEnded += () => eventFired = true;

        // Act - Update with small time, won't reach end
        player.Update(0.016f);

        // Assert
        Assert.False(eventFired);
        Assert.Equal(PlaybackState.Playing, player.State);
    }

    [Fact]
    public void PlaybackEnded_DoesNotFireOnExplicitStop()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));
        player.Play();

        var endedFired = false;
        var stoppedFired = false;
        player.PlaybackEnded += () => endedFired = true;
        player.PlaybackStopped += () => stoppedFired = true;

        // Act
        player.Stop();

        // Assert
        Assert.False(endedFired);
        Assert.True(stoppedFired);
    }

    #endregion

    #region FrameChanged Event Tests

    [Fact]
    public void Update_WhenFrameAdvances_FiresFrameChanged()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));
        player.Play();

        var framesReported = new List<int>();
        player.FrameChanged += frame => framesReported.Add(frame);

        // Act - Advance 2 frames (32ms total at 16ms per frame)
        player.Update(0.032f);

        // Assert
        Assert.Equal(2, framesReported.Count);
        Assert.Equal(1, framesReported[0]);
        Assert.Equal(2, framesReported[1]);
    }

    [Fact]
    public void Update_WhenNoFrameAdvances_DoesNotFireFrameChanged()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));
        player.Play();

        var eventFired = false;
        player.FrameChanged += _ => eventFired = true;

        // Act - Update with very small time, shouldn't advance
        player.Update(0.001f);

        // Assert
        Assert.False(eventFired);
    }

    [Fact]
    public void SeekToFrame_FiresFrameChanged()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));

        int? reportedFrame = null;
        player.FrameChanged += frame => reportedFrame = frame;

        // Act
        player.SeekToFrame(50);

        // Assert
        Assert.Equal(50, reportedFrame);
    }

    [Fact]
    public void SeekToFrame_WhenSameFrame_DoesNotFireFrameChanged()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));
        player.SeekToFrame(50);

        var eventFired = false;
        player.FrameChanged += _ => eventFired = true;

        // Act - Seek to same frame
        player.SeekToFrame(50);

        // Assert
        Assert.False(eventFired);
    }

    [Fact]
    public void SeekToTime_FiresFrameChanged()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));

        int? reportedFrame = null;
        player.FrameChanged += frame => reportedFrame = frame;

        // Act - Seek to ~500ms (frame 31 at 16ms per frame)
        player.SeekToTime(TimeSpan.FromMilliseconds(500));

        // Assert
        Assert.NotNull(reportedFrame);
        Assert.Equal(31, reportedFrame.Value);
    }

    [Fact]
    public void Step_FiresFrameChanged()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));

        int? reportedFrame = null;
        player.FrameChanged += frame => reportedFrame = frame;

        // Act
        player.Step();

        // Assert
        Assert.Equal(1, reportedFrame);
    }

    [Fact]
    public void Step_WhenAtLastFrame_DoesNotFireFrameChanged()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));
        player.SeekToFrame(99); // Last frame

        var eventFired = false;
        player.FrameChanged += _ => eventFired = true;

        // Act - Try to step forward at last frame
        player.Step();

        // Assert
        Assert.False(eventFired);
    }

    [Fact]
    public void StepBackward_FiresFrameChanged()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));
        player.SeekToFrame(50);

        int? reportedFrame = null;
        player.FrameChanged += frame => reportedFrame = frame;

        // Act
        player.Step(-5);

        // Assert
        Assert.Equal(45, reportedFrame);
    }

    #endregion

    #region State Transition Tests

    [Fact]
    public void StateTransitions_FollowExpectedPattern()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(10));

        var events = new List<string>();
        player.PlaybackStarted += () => events.Add("Started");
        player.PlaybackPaused += () => events.Add("Paused");
        player.PlaybackStopped += () => events.Add("Stopped");
        player.PlaybackEnded += () => events.Add("Ended");

        // Act - Play through various states
        player.Play();          // Stopped -> Playing
        player.Pause();         // Playing -> Paused
        player.Play();          // Paused -> Playing
        player.Stop();          // Playing -> Stopped

        // Assert
        Assert.Equal(["Started", "Paused", "Started", "Stopped"], events);
    }

    [Fact]
    public void StateTransitions_NaturalEnd_FiresCorrectEvents()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(2)); // Very short replay

        var events = new List<string>();
        player.PlaybackStarted += () => events.Add("Started");
        player.PlaybackEnded += () => events.Add("Ended");
        player.PlaybackStopped += () => events.Add("Stopped");

        // Act
        player.Play();
        player.Update(1.0f); // Enough to complete all frames

        // Assert
        Assert.Equal(["Started", "Ended"], events);
    }

    #endregion

    #region UI Usage Pattern Tests

    [Fact]
    public void UIPattern_ProgressBarUpdate_WorksCorrectly()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));

        int progressValue = 0;
        player.FrameChanged += frame => progressValue = frame;

        // Act
        player.Play();
        // Use 0.085f to ensure we cross the 5-frame boundary despite float precision
        // (5 frames = 80ms at 16ms per frame, but 0.080f has float precision issues)
        player.Update(0.085f);

        // Assert
        Assert.Equal(5, progressValue);
    }

    [Fact]
    public void UIPattern_ButtonTextUpdate_WorksCorrectly()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));

        string buttonText = "Play";
        player.PlaybackStarted += () => buttonText = "Pause";
        player.PlaybackPaused += () => buttonText = "Play";
        player.PlaybackStopped += () => buttonText = "Play";
        player.PlaybackEnded += () => buttonText = "Replay";

        // Act & Assert
        Assert.Equal("Play", buttonText);

        player.Play();
        Assert.Equal("Pause", buttonText);

        player.Pause();
        Assert.Equal("Play", buttonText);

        player.Play();
        Assert.Equal("Pause", buttonText);

        player.Stop();
        Assert.Equal("Play", buttonText);
    }

    [Fact]
    public void UIPattern_EndReached_ShowsReplayButton()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(2));

        string buttonText = "Play";
        player.PlaybackStarted += () => buttonText = "Pause";
        player.PlaybackEnded += () => buttonText = "Replay";

        // Act
        player.Play();
        Assert.Equal("Pause", buttonText);

        player.Update(1.0f); // Complete replay
        Assert.Equal("Replay", buttonText);
    }

    #endregion

    #region Multiple Handler Tests

    [Fact]
    public void Events_SupportMultipleHandlers()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));

        int handler1Count = 0;
        int handler2Count = 0;
        int handler3Count = 0;

        player.PlaybackStarted += () => handler1Count++;
        player.PlaybackStarted += () => handler2Count++;
        player.PlaybackStarted += () => handler3Count++;

        // Act
        player.Play();

        // Assert
        Assert.Equal(1, handler1Count);
        Assert.Equal(1, handler2Count);
        Assert.Equal(1, handler3Count);
    }

    [Fact]
    public void Events_CanRemoveHandlers()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));

        int count = 0;
        Action handler = () => count++;

        player.PlaybackStarted += handler;
        player.Play();
        Assert.Equal(1, count);

        // Act - Remove handler
        player.PlaybackStopped += handler;
        player.PlaybackStarted -= handler;
        player.Stop();
        player.Play(); // Should not increment count via PlaybackStarted

        // Assert - Count should be 2 (1 from first play, 1 from stop)
        Assert.Equal(2, count);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public void Events_FiredOutsideLock_PreventDeadlock()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));

        var completed = false;
        player.PlaybackStarted += () =>
        {
            // Try to access player state from within event handler
            // This would deadlock if events were fired inside the lock
            var state = player.State;
            var frame = player.CurrentFrame;
            completed = true;
        };

        // Act
        player.Play();

        // Assert
        Assert.True(completed);
    }

    [Fact]
    public void FrameChanged_CanAccessPlayerState()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplay(100));
        player.Play();

        var statesDuringFrameChange = new List<PlaybackState>();
        player.FrameChanged += _ =>
        {
            statesDuringFrameChange.Add(player.State);
        };

        // Act
        player.Update(0.032f); // Advance 2 frames

        // Assert
        Assert.Equal(2, statesDuringFrameChange.Count);
        Assert.All(statesDuringFrameChange, s => Assert.Equal(PlaybackState.Playing, s));
    }

    #endregion
}
