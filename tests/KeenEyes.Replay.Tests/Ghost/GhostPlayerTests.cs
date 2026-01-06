using System.Numerics;
using KeenEyes.Replay.Ghost;

namespace KeenEyes.Replay.Tests.Ghost;

/// <summary>
/// Unit tests for the <see cref="GhostPlayer"/> class.
/// </summary>
public class GhostPlayerTests
{
    private static GhostData CreateTestGhostData()
    {
        return new GhostData
        {
            Name = "Test Ghost",
            EntityName = "Player",
            RecordingStarted = DateTimeOffset.UtcNow.AddMinutes(-2),
            Duration = TimeSpan.FromSeconds(3),
            FrameCount = 4,
            Frames =
            [
                new GhostFrame(Vector3.Zero, Quaternion.Identity, TimeSpan.Zero)
                {
                    Scale = Vector3.One,
                    Distance = 0f
                },
                new GhostFrame(new Vector3(1, 0, 0), Quaternion.Identity, TimeSpan.FromSeconds(1))
                {
                    Scale = Vector3.One,
                    Distance = 1f
                },
                new GhostFrame(new Vector3(2, 0, 0), Quaternion.Identity, TimeSpan.FromSeconds(2))
                {
                    Scale = Vector3.One,
                    Distance = 2f
                },
                new GhostFrame(new Vector3(3, 0, 0), Quaternion.Identity, TimeSpan.FromSeconds(3))
                {
                    Scale = Vector3.One,
                    Distance = 3f
                }
            ]
        };
    }

    #region Load Tests

    [Fact]
    public void Load_ValidGhostData_LoadsSuccessfully()
    {
        // Arrange
        using var player = new GhostPlayer();
        var ghostData = CreateTestGhostData();

        // Act
        player.Load(ghostData);

        // Assert
        Assert.True(player.IsLoaded);
        Assert.Equal(ghostData, player.LoadedGhost);
    }

    [Fact]
    public void Load_NullData_ThrowsArgumentNullException()
    {
        // Arrange
        using var player = new GhostPlayer();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => player.Load(null!));
    }

    [Fact]
    public void Load_SetsInitialPosition()
    {
        // Arrange
        using var player = new GhostPlayer();
        var ghostData = CreateTestGhostData();

        // Act
        player.Load(ghostData);

        // Assert
        Assert.Equal(Vector3.Zero, player.Position);
    }

    [Fact]
    public void Load_SetsInitialState()
    {
        // Arrange
        using var player = new GhostPlayer();
        var ghostData = CreateTestGhostData();

        // Act
        player.Load(ghostData);

        // Assert
        Assert.Equal(GhostPlaybackState.Stopped, player.State);
        Assert.Equal(0, player.CurrentFrame);
    }

    #endregion

    #region Unload Tests

    [Fact]
    public void Unload_ClearsGhostData()
    {
        // Arrange
        using var player = new GhostPlayer();
        var ghostData = CreateTestGhostData();
        player.Load(ghostData);

        // Act
        player.Unload();

        // Assert
        Assert.False(player.IsLoaded);
        Assert.Null(player.LoadedGhost);
    }

    [Fact]
    public void Unload_ResetsPosition()
    {
        // Arrange
        using var player = new GhostPlayer();
        var ghostData = CreateTestGhostData();
        player.Load(ghostData);
        player.Play();
        player.Update(1.5f);

        // Act
        player.Unload();

        // Assert
        Assert.Equal(Vector3.Zero, player.Position);
    }

    #endregion

    #region Play/Pause/Stop Tests

    [Fact]
    public void Play_StartsPlayback()
    {
        // Arrange
        using var player = new GhostPlayer();
        player.Load(CreateTestGhostData());

        // Act
        player.Play();

        // Assert
        Assert.Equal(GhostPlaybackState.Playing, player.State);
    }

    [Fact]
    public void Play_WithNoGhost_ThrowsInvalidOperationException()
    {
        // Arrange
        using var player = new GhostPlayer();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => player.Play());
    }

    [Fact]
    public void Play_FiresPlaybackStartedEvent()
    {
        // Arrange
        using var player = new GhostPlayer();
        player.Load(CreateTestGhostData());
        var eventFired = false;
        player.PlaybackStarted += () => eventFired = true;

        // Act
        player.Play();

        // Assert
        Assert.True(eventFired);
    }

    [Fact]
    public void Pause_PausesPlayback()
    {
        // Arrange
        using var player = new GhostPlayer();
        player.Load(CreateTestGhostData());
        player.Play();

        // Act
        player.Pause();

        // Assert
        Assert.Equal(GhostPlaybackState.Paused, player.State);
    }

    [Fact]
    public void Pause_FiresPlaybackPausedEvent()
    {
        // Arrange
        using var player = new GhostPlayer();
        player.Load(CreateTestGhostData());
        player.Play();
        var eventFired = false;
        player.PlaybackPaused += () => eventFired = true;

        // Act
        player.Pause();

        // Assert
        Assert.True(eventFired);
    }

    [Fact]
    public void Stop_StopsPlayback()
    {
        // Arrange
        using var player = new GhostPlayer();
        player.Load(CreateTestGhostData());
        player.Play();

        // Act
        player.Stop();

        // Assert
        Assert.Equal(GhostPlaybackState.Stopped, player.State);
    }

    [Fact]
    public void Stop_ResetsToBeginning()
    {
        // Arrange
        using var player = new GhostPlayer();
        player.Load(CreateTestGhostData());
        player.Play();
        player.Update(1.5f);

        // Act
        player.Stop();

        // Assert
        Assert.Equal(0, player.CurrentFrame);
        Assert.Equal(TimeSpan.Zero, player.CurrentTime);
    }

    [Fact]
    public void Stop_FiresPlaybackStoppedEvent()
    {
        // Arrange
        using var player = new GhostPlayer();
        player.Load(CreateTestGhostData());
        player.Play();
        var eventFired = false;
        player.PlaybackStopped += () => eventFired = true;

        // Act
        player.Stop();

        // Assert
        Assert.True(eventFired);
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_AdvancesPosition()
    {
        // Arrange
        using var player = new GhostPlayer();
        player.Load(CreateTestGhostData());
        player.Play();

        // Act
        player.Update(1.5f);

        // Assert
        Assert.True(player.Position.X > 0);
    }

    [Fact]
    public void Update_InterpolatesPosition()
    {
        // Arrange
        using var player = new GhostPlayer();
        player.Load(CreateTestGhostData());
        player.Play();

        // Act
        player.Update(0.5f); // Halfway between frame 0 and 1

        // Assert
        Assert.True(player.Position.X > 0 && player.Position.X < 1);
    }

    [Fact]
    public void Update_WhenNotPlaying_ReturnsFalse()
    {
        // Arrange
        using var player = new GhostPlayer();
        player.Load(CreateTestGhostData());

        // Act
        var result = player.Update(1f);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Update_AtEnd_StopsPlayback()
    {
        // Arrange
        using var player = new GhostPlayer();
        player.Load(CreateTestGhostData());
        player.Play();

        // Act - advance past the end
        player.Update(10f);

        // Assert
        Assert.Equal(GhostPlaybackState.Stopped, player.State);
    }

    [Fact]
    public void Update_AtEnd_FiresPlaybackEndedEvent()
    {
        // Arrange
        using var player = new GhostPlayer();
        player.Load(CreateTestGhostData());
        player.Play();
        var eventFired = false;
        player.PlaybackEnded += () => eventFired = true;

        // Act - advance past the end
        player.Update(10f);

        // Assert
        Assert.True(eventFired);
    }

    [Fact]
    public void Update_WithFrameSyncMode_AdvancesOneFrame()
    {
        // Arrange
        using var player = new GhostPlayer();
        player.Load(CreateTestGhostData());
        player.SyncMode = GhostSyncMode.FrameSynced;
        player.Play();

        // Act
        player.Update(0.001f); // Very small delta, should still advance one frame

        // Assert
        Assert.Equal(1, player.CurrentFrame);
    }

    #endregion

    #region UpdateByDistance Tests

    [Fact]
    public void UpdateByDistance_SetsPositionByDistance()
    {
        // Arrange
        using var player = new GhostPlayer();
        player.Load(CreateTestGhostData());
        player.SyncMode = GhostSyncMode.DistanceSynced;
        player.Play();

        // Act
        player.UpdateByDistance(1.5f);

        // Assert
        Assert.True(player.Position.X > 1 && player.Position.X < 2);
    }

    [Fact]
    public void UpdateByDistance_InterpolatesCorrectly()
    {
        // Arrange
        using var player = new GhostPlayer();
        player.Load(CreateTestGhostData());
        player.SyncMode = GhostSyncMode.DistanceSynced;
        player.Play();

        // Act - at exactly distance 2
        player.UpdateByDistance(2f);

        // Assert
        Assert.Equal(2f, player.Position.X, 0.01f);
    }

    #endregion

    #region SeekToTime Tests

    [Fact]
    public void SeekToTime_SetsCurrentTime()
    {
        // Arrange
        using var player = new GhostPlayer();
        player.Load(CreateTestGhostData());

        // Act
        player.SeekToTime(TimeSpan.FromSeconds(1.5));

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(1.5), player.CurrentTime);
    }

    [Fact]
    public void SeekToTime_SetsPosition()
    {
        // Arrange
        using var player = new GhostPlayer();
        player.Load(CreateTestGhostData());

        // Act
        player.SeekToTime(TimeSpan.FromSeconds(1.5));

        // Assert
        Assert.True(player.Position.X > 1 && player.Position.X < 2);
    }

    [Fact]
    public void SeekToTime_PausesPlayback()
    {
        // Arrange
        using var player = new GhostPlayer();
        player.Load(CreateTestGhostData());
        player.Play();

        // Act
        player.SeekToTime(TimeSpan.FromSeconds(1));

        // Assert
        Assert.Equal(GhostPlaybackState.Paused, player.State);
    }

    [Fact]
    public void SeekToTime_NegativeTime_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        using var player = new GhostPlayer();
        player.Load(CreateTestGhostData());

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => player.SeekToTime(TimeSpan.FromSeconds(-1)));
    }

    [Fact]
    public void SeekToTime_BeyondDuration_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        using var player = new GhostPlayer();
        player.Load(CreateTestGhostData());

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => player.SeekToTime(TimeSpan.FromSeconds(100)));
    }

    #endregion

    #region SeekToFrame Tests

    [Fact]
    public void SeekToFrame_SetsCurrentFrame()
    {
        // Arrange
        using var player = new GhostPlayer();
        player.Load(CreateTestGhostData());

        // Act
        player.SeekToFrame(2);

        // Assert
        Assert.Equal(2, player.CurrentFrame);
    }

    [Fact]
    public void SeekToFrame_SetsPosition()
    {
        // Arrange
        using var player = new GhostPlayer();
        player.Load(CreateTestGhostData());

        // Act
        player.SeekToFrame(2);

        // Assert
        Assert.Equal(new Vector3(2, 0, 0), player.Position);
    }

    [Fact]
    public void SeekToFrame_NegativeFrame_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        using var player = new GhostPlayer();
        player.Load(CreateTestGhostData());

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => player.SeekToFrame(-1));
    }

    [Fact]
    public void SeekToFrame_BeyondFrameCount_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        using var player = new GhostPlayer();
        player.Load(CreateTestGhostData());

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => player.SeekToFrame(100));
    }

    [Fact]
    public void SeekToFrame_FiresFrameChangedEvent()
    {
        // Arrange
        using var player = new GhostPlayer();
        player.Load(CreateTestGhostData());
        var newFrame = -1;
        player.FrameChanged += frame => newFrame = frame;

        // Act
        player.SeekToFrame(2);

        // Assert
        Assert.Equal(2, newFrame);
    }

    #endregion

    #region GetFrame Tests

    [Fact]
    public void GetFrame_ReturnsCorrectFrame()
    {
        // Arrange
        using var player = new GhostPlayer();
        var ghostData = CreateTestGhostData();
        player.Load(ghostData);

        // Act
        var frame = player.GetFrame(1);

        // Assert
        Assert.Equal(new Vector3(1, 0, 0), frame.Position);
    }

    [Fact]
    public void GetFrame_InvalidIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        using var player = new GhostPlayer();
        player.Load(CreateTestGhostData());

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => player.GetFrame(100));
    }

    #endregion

    #region PlaybackSpeed Tests

    [Fact]
    public void PlaybackSpeed_Default_IsOne()
    {
        // Arrange
        using var player = new GhostPlayer();

        // Assert
        Assert.Equal(1.0f, player.PlaybackSpeed);
    }

    [Fact]
    public void PlaybackSpeed_CanBeSet()
    {
        // Arrange
        using var player = new GhostPlayer();

        // Act
        player.PlaybackSpeed = 2.0f;

        // Assert
        Assert.Equal(2.0f, player.PlaybackSpeed);
    }

    [Fact]
    public void PlaybackSpeed_TooLow_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        using var player = new GhostPlayer();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => player.PlaybackSpeed = 0.1f);
    }

    [Fact]
    public void PlaybackSpeed_TooHigh_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        using var player = new GhostPlayer();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => player.PlaybackSpeed = 5.0f);
    }

    [Fact]
    public void PlaybackSpeed_AffectsPlayback()
    {
        // Arrange
        using var player1 = new GhostPlayer();
        using var player2 = new GhostPlayer();
        player1.Load(CreateTestGhostData());
        player2.Load(CreateTestGhostData());
        player1.PlaybackSpeed = 1.0f;
        player2.PlaybackSpeed = 2.0f;
        player1.Play();
        player2.Play();

        // Act
        player1.Update(1.0f);
        player2.Update(1.0f);

        // Assert
        Assert.True(player2.CurrentTime > player1.CurrentTime);
    }

    #endregion

    #region SyncMode Tests

    [Fact]
    public void SyncMode_Default_IsTimeSynced()
    {
        // Arrange
        using var player = new GhostPlayer();

        // Assert
        Assert.Equal(GhostSyncMode.TimeSynced, player.SyncMode);
    }

    [Fact]
    public void SyncMode_CanBeSet()
    {
        // Arrange
        using var player = new GhostPlayer();

        // Act
        player.SyncMode = GhostSyncMode.FrameSynced;

        // Assert
        Assert.Equal(GhostSyncMode.FrameSynced, player.SyncMode);
    }

    #endregion

    #region Properties Tests

    [Fact]
    public void TotalFrames_ReturnsCorrectCount()
    {
        // Arrange
        using var player = new GhostPlayer();
        player.Load(CreateTestGhostData());

        // Assert
        Assert.Equal(4, player.TotalFrames);
    }

    [Fact]
    public void TotalFrames_WhenNotLoaded_ReturnsZero()
    {
        // Arrange
        using var player = new GhostPlayer();

        // Assert
        Assert.Equal(0, player.TotalFrames);
    }

    [Fact]
    public void TotalDuration_ReturnsCorrectDuration()
    {
        // Arrange
        using var player = new GhostPlayer();
        player.Load(CreateTestGhostData());

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(3), player.TotalDuration);
    }

    [Fact]
    public void CurrentFrame_WhenNotLoaded_ReturnsMinusOne()
    {
        // Arrange
        using var player = new GhostPlayer();

        // Assert
        Assert.Equal(-1, player.CurrentFrame);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var player = new GhostPlayer();
        player.Load(CreateTestGhostData());

        // Act & Assert - should not throw
        player.Dispose();
        player.Dispose();
    }

    [Fact]
    public void Dispose_ThrowsOnSubsequentOperations()
    {
        // Arrange
        var player = new GhostPlayer();
        player.Load(CreateTestGhostData());
        player.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => player.Play());
    }

    #endregion
}
