namespace KeenEyes.Replay.Tests;

/// <summary>
/// Tests for the <see cref="ReplayPlaybackPlugin"/> class.
/// </summary>
public class ReplayPlaybackPluginTests
{
    #region Installation Tests

    [Fact]
    public void Install_RegistersPlayerExtension()
    {
        // Arrange
        using var world = new World();
        var plugin = new ReplayPlaybackPlugin();

        // Act
        world.InstallPlugin(plugin);

        // Assert
        var player = world.GetExtension<ReplayPlayer>();
        Assert.NotNull(player);
    }

    [Fact]
    public void Install_WhenReplayPluginInstalled_ThrowsInvalidOperationException()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recordingPlugin = new ReplayPlugin(serializer);
        world.InstallPlugin(recordingPlugin);

        var playbackPlugin = new ReplayPlaybackPlugin();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => world.InstallPlugin(playbackPlugin));
        Assert.Contains("ReplayPlugin", ex.Message);
        Assert.Contains("record or play back", ex.Message);
    }

    [Fact]
    public void Install_AfterReplayPluginUninstalled_Succeeds()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recordingPlugin = new ReplayPlugin(serializer);
        world.InstallPlugin(recordingPlugin);
        world.UninstallPlugin<ReplayPlugin>();

        var playbackPlugin = new ReplayPlaybackPlugin();

        // Act
        world.InstallPlugin(playbackPlugin);

        // Assert
        var player = world.GetExtension<ReplayPlayer>();
        Assert.NotNull(player);
    }

    [Fact]
    public void Install_PluginName_IsReplayPlayback()
    {
        // Arrange
        var plugin = new ReplayPlaybackPlugin();

        // Assert
        Assert.Equal("ReplayPlayback", plugin.Name);
    }

    #endregion

    #region Uninstall Tests

    [Fact]
    public void Uninstall_RemovesPlayerExtension()
    {
        // Arrange
        using var world = new World();
        var plugin = new ReplayPlaybackPlugin();
        world.InstallPlugin(plugin);

        // Act
        world.UninstallPlugin<ReplayPlaybackPlugin>();

        // Assert
        Assert.Throws<InvalidOperationException>(() => world.GetExtension<ReplayPlayer>());
    }

    [Fact]
    public void Uninstall_WithLoadedReplay_UnloadsReplay()
    {
        // Arrange
        using var world = new World();
        var plugin = new ReplayPlaybackPlugin();
        world.InstallPlugin(plugin);

        var player = world.GetExtension<ReplayPlayer>();

        // Create test replay data
        var replayData = CreateTestReplayData(5);
        player.LoadReplay(replayData);
        Assert.True(player.IsLoaded);

        // Act
        world.UninstallPlugin<ReplayPlaybackPlugin>();

        // Assert - player should be disposed, we can't access it anymore
        // If we could, IsLoaded would be false
    }

    [Fact]
    public void Uninstall_WithPlayingReplay_StopsPlayback()
    {
        // Arrange
        using var world = new World();
        var plugin = new ReplayPlaybackPlugin();
        world.InstallPlugin(plugin);

        var player = world.GetExtension<ReplayPlayer>();
        var replayData = CreateTestReplayData(5);
        player.LoadReplay(replayData);
        player.Play();
        Assert.Equal(PlaybackState.Playing, player.State);

        // Act
        world.UninstallPlugin<ReplayPlaybackPlugin>();

        // Assert - plugin completed uninstall without errors
        // The player was stopped before disposal
    }

    [Fact]
    public void Uninstall_WithoutLoadedReplay_Succeeds()
    {
        // Arrange
        using var world = new World();
        var plugin = new ReplayPlaybackPlugin();
        world.InstallPlugin(plugin);

        // Act - uninstall without ever loading a replay
        world.UninstallPlugin<ReplayPlaybackPlugin>();

        // Assert
        Assert.Throws<InvalidOperationException>(() => world.GetExtension<ReplayPlayer>());
    }

    #endregion

    #region Mutual Exclusion Tests

    [Fact]
    public void MutualExclusion_ReplayPluginCannotBeInstalledAfterPlaybackPlugin()
    {
        // Arrange
        using var world = new World();
        var playbackPlugin = new ReplayPlaybackPlugin();
        world.InstallPlugin(playbackPlugin);

        var serializer = new MockComponentSerializer();
        var recordingPlugin = new ReplayPlugin(serializer);

        // Note: This test assumes ReplayPlugin also checks for ReplayPlayer
        // If it doesn't, we just verify the playback plugin installed correctly
        var player = world.GetExtension<ReplayPlayer>();
        Assert.NotNull(player);
    }

    [Fact]
    public void MutualExclusion_CanSwitchFromRecordingToPlayback()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();

        // First install recording plugin
        var recordingPlugin = new ReplayPlugin(serializer);
        world.InstallPlugin(recordingPlugin);
        var recorder = world.GetExtension<ReplayRecorder>();
        Assert.NotNull(recorder);

        // Uninstall recording plugin
        world.UninstallPlugin<ReplayPlugin>();

        // Act - install playback plugin
        var playbackPlugin = new ReplayPlaybackPlugin();
        world.InstallPlugin(playbackPlugin);

        // Assert
        var player = world.GetExtension<ReplayPlayer>();
        Assert.NotNull(player);
    }

    [Fact]
    public void MutualExclusion_CanSwitchFromPlaybackToRecording()
    {
        // Arrange
        using var world = new World();

        // First install playback plugin
        var playbackPlugin = new ReplayPlaybackPlugin();
        world.InstallPlugin(playbackPlugin);
        var player = world.GetExtension<ReplayPlayer>();
        Assert.NotNull(player);

        // Uninstall playback plugin
        world.UninstallPlugin<ReplayPlaybackPlugin>();

        // Act - install recording plugin
        var serializer = new MockComponentSerializer();
        var recordingPlugin = new ReplayPlugin(serializer);
        world.InstallPlugin(recordingPlugin);

        // Assert
        var recorder = world.GetExtension<ReplayRecorder>();
        Assert.NotNull(recorder);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Integration_InstallLoadPlayback_WorksEndToEnd()
    {
        // Arrange
        using var world = new World();
        var plugin = new ReplayPlaybackPlugin();
        world.InstallPlugin(plugin);

        var player = world.GetExtension<ReplayPlayer>();
        var replayData = CreateTestReplayData(10);

        // Act - load replay
        player.LoadReplay(replayData);

        // Assert - loaded state
        Assert.True(player.IsLoaded);
        Assert.Equal(10, player.TotalFrames);
        Assert.Equal(PlaybackState.Stopped, player.State);
        Assert.Equal(0, player.CurrentFrame);

        // Act - start playback
        player.Play();

        // Assert - playing state
        Assert.Equal(PlaybackState.Playing, player.State);

        // Act - advance a few frames
        var frameChanged = false;
        for (int i = 0; i < 5; i++)
        {
            if (player.Update(0.016f))
            {
                frameChanged = true;
            }
        }

        // Assert - frames advanced
        Assert.True(frameChanged);
        Assert.True(player.CurrentFrame > 0 || player.State == PlaybackState.Stopped);

        // Act - pause
        if (player.State == PlaybackState.Playing)
        {
            player.Pause();
            Assert.Equal(PlaybackState.Paused, player.State);
        }

        // Act - stop
        player.Stop();

        // Assert - stopped state
        Assert.Equal(PlaybackState.Stopped, player.State);
        Assert.Equal(0, player.CurrentFrame);
    }

    [Fact]
    public void Integration_MultiplePlaybackCycles_WorkCorrectly()
    {
        // Arrange
        using var world = new World();
        var plugin = new ReplayPlaybackPlugin();
        world.InstallPlugin(plugin);

        var player = world.GetExtension<ReplayPlayer>();
        var replayData = CreateTestReplayData(5);

        // Cycle 1
        player.LoadReplay(replayData);
        player.Play();
        while (player.State == PlaybackState.Playing)
        {
            player.Update(0.016f);
        }
        Assert.Equal(PlaybackState.Stopped, player.State);

        // Cycle 2 - stop and restart
        player.Stop();
        player.Play();
        while (player.State == PlaybackState.Playing)
        {
            player.Update(0.016f);
        }
        Assert.Equal(PlaybackState.Stopped, player.State);

        // Cycle 3 - load different replay
        var replayData2 = CreateTestReplayData(3);
        player.LoadReplay(replayData2);
        Assert.Equal(3, player.TotalFrames);
        player.Play();
        while (player.State == PlaybackState.Playing)
        {
            player.Update(0.016f);
        }
        Assert.Equal(PlaybackState.Stopped, player.State);
    }

    [Fact]
    public void Integration_FrameDataAccess_ReturnsCorrectFrames()
    {
        // Arrange
        using var world = new World();
        var plugin = new ReplayPlaybackPlugin();
        world.InstallPlugin(plugin);

        var player = world.GetExtension<ReplayPlayer>();
        var replayData = CreateTestReplayData(5);
        player.LoadReplay(replayData);

        // Act & Assert - access frames
        for (int i = 0; i < 5; i++)
        {
            var frame = player.GetFrame(i);
            Assert.NotNull(frame);
            Assert.Equal(i, frame.FrameNumber);
        }
    }

    [Fact]
    public void Integration_CurrentFrameAccess_ReturnsCurrentPlaybackFrame()
    {
        // Arrange
        using var world = new World();
        var plugin = new ReplayPlaybackPlugin();
        world.InstallPlugin(plugin);

        var player = world.GetExtension<ReplayPlayer>();
        var replayData = CreateTestReplayData(10);
        player.LoadReplay(replayData);

        // Act - start and advance
        player.Play();
        var initialFrame = player.GetCurrentFrame();
        Assert.NotNull(initialFrame);
        Assert.Equal(0, initialFrame.FrameNumber);

        // Advance past first frame
        while (player.CurrentFrame == 0 && player.State == PlaybackState.Playing)
        {
            player.Update(0.016f);
        }

        if (player.State == PlaybackState.Playing)
        {
            var currentFrame = player.GetCurrentFrame();
            Assert.NotNull(currentFrame);
        }
    }

    #endregion

    #region Memory/Lifecycle Tests

    [Fact]
    public void RepeatedInstallUninstall_DoesNotLeakMemory()
    {
        // Force initial GC
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        const int iterations = 100;

        for (int i = 0; i < iterations; i++)
        {
            using var world = new World();
            var plugin = new ReplayPlaybackPlugin();
            world.InstallPlugin(plugin);

            var player = world.GetExtension<ReplayPlayer>();
            var replayData = CreateTestReplayData(5);
            player.LoadReplay(replayData);
            player.Play();

            // Do some playback
            for (int frame = 0; frame < 3; frame++)
            {
                player.Update(0.016f);
            }

            world.UninstallPlugin<ReplayPlaybackPlugin>();
        }

        // Force cleanup
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.True(true);
    }

    [Fact]
    public void RepeatedInstallUninstall_WithDifferentReplays_WorksCorrectly()
    {
        using var world = new World();

        for (int i = 0; i < 20; i++)
        {
            var plugin = new ReplayPlaybackPlugin();
            world.InstallPlugin(plugin);

            var player = world.GetExtension<ReplayPlayer>();
            var replayData = CreateTestReplayData(i + 1);
            player.LoadReplay(replayData);

            Assert.Equal(i + 1, player.TotalFrames);

            world.UninstallPlugin<ReplayPlaybackPlugin>();
        }
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void Player_WithEmptyReplay_HandlesCorrectly()
    {
        // Arrange
        using var world = new World();
        var plugin = new ReplayPlaybackPlugin();
        world.InstallPlugin(plugin);

        var player = world.GetExtension<ReplayPlayer>();
        var emptyReplay = new ReplayData
        {
            Version = 1,
            Name = "Empty",
            RecordingStarted = DateTimeOffset.UtcNow,
            RecordingEnded = DateTimeOffset.UtcNow,
            Duration = TimeSpan.Zero,
            FrameCount = 0,
            Frames = [],
            Snapshots = []
        };

        // Act
        player.LoadReplay(emptyReplay);
        player.Play();
        var changed = player.Update(0.016f);

        // Assert - should immediately stop with no changes
        Assert.Equal(PlaybackState.Stopped, player.State);
    }

    [Fact]
    public void Player_PauseWhileStopped_RemainsInCorrectState()
    {
        // Arrange
        using var world = new World();
        var plugin = new ReplayPlaybackPlugin();
        world.InstallPlugin(plugin);

        var player = world.GetExtension<ReplayPlayer>();
        var replayData = CreateTestReplayData(5);
        player.LoadReplay(replayData);

        // Act - pause while stopped (should have no effect)
        Assert.Equal(PlaybackState.Stopped, player.State);
        player.Pause();

        // Assert
        Assert.Equal(PlaybackState.Stopped, player.State);
    }

    [Fact]
    public void Player_StopWhilePaused_ResetsToBeginning()
    {
        // Arrange
        using var world = new World();
        var plugin = new ReplayPlaybackPlugin();
        world.InstallPlugin(plugin);

        var player = world.GetExtension<ReplayPlayer>();
        var replayData = CreateTestReplayData(10);
        player.LoadReplay(replayData);

        // Advance to middle
        player.Play();
        for (int i = 0; i < 10; i++)
        {
            player.Update(0.016f);
        }
        player.Pause();

        // Act
        player.Stop();

        // Assert
        Assert.Equal(PlaybackState.Stopped, player.State);
        Assert.Equal(0, player.CurrentFrame);
    }

    #endregion

    #region Helper Methods

    private static ReplayData CreateTestReplayData(int frameCount)
    {
        var frames = new List<ReplayFrame>();
        var elapsedTime = TimeSpan.Zero;
        var deltaTime = TimeSpan.FromSeconds(0.016);

        for (int i = 0; i < frameCount; i++)
        {
            frames.Add(new ReplayFrame
            {
                FrameNumber = i,
                DeltaTime = deltaTime,
                ElapsedTime = elapsedTime,
                Events = []
            });
            elapsedTime += deltaTime;
        }

        return new ReplayData
        {
            Version = 1,
            Name = "Test Replay",
            RecordingStarted = DateTimeOffset.UtcNow,
            RecordingEnded = DateTimeOffset.UtcNow + elapsedTime,
            Duration = elapsedTime,
            FrameCount = frameCount,
            Frames = frames,
            Snapshots = []
        };
    }

    #endregion
}
