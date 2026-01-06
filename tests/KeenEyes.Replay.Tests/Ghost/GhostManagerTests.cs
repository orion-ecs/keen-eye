using System.Numerics;
using KeenEyes.Replay.Ghost;

namespace KeenEyes.Replay.Tests.Ghost;

/// <summary>
/// Unit tests for the <see cref="GhostManager"/> class.
/// </summary>
public class GhostManagerTests
{
    private static GhostData CreateTestGhostData(string name = "Test Ghost")
    {
        return new GhostData
        {
            Name = name,
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

    #region AddGhost Tests

    [Fact]
    public void AddGhost_AddsGhostSuccessfully()
    {
        // Arrange
        using var manager = new GhostManager();
        var ghostData = CreateTestGhostData();

        // Act
        manager.AddGhost("ghost1", ghostData);

        // Assert
        Assert.Equal(1, manager.Count);
        Assert.True(manager.ContainsGhost("ghost1"));
    }

    [Fact]
    public void AddGhost_WithConfig_SetsConfig()
    {
        // Arrange
        using var manager = new GhostManager();
        var ghostData = CreateTestGhostData();
        var config = new GhostVisualConfig { Opacity = 0.75f, Label = "Test" };

        // Act
        manager.AddGhost("ghost1", ghostData, config);

        // Assert
        var retrievedConfig = manager.GetConfig("ghost1");
        Assert.NotNull(retrievedConfig);
        Assert.Equal(0.75f, retrievedConfig.Opacity);
        Assert.Equal("Test", retrievedConfig.Label);
    }

    [Fact]
    public void AddGhost_DuplicateId_ThrowsArgumentException()
    {
        // Arrange
        using var manager = new GhostManager();
        var ghostData = CreateTestGhostData();
        manager.AddGhost("ghost1", ghostData);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => manager.AddGhost("ghost1", ghostData));
    }

    [Fact]
    public void AddGhost_NullId_ThrowsArgumentNullException()
    {
        // Arrange
        using var manager = new GhostManager();
        var ghostData = CreateTestGhostData();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => manager.AddGhost(null!, ghostData));
    }

    [Fact]
    public void AddGhost_NullData_ThrowsArgumentNullException()
    {
        // Arrange
        using var manager = new GhostManager();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => manager.AddGhost("ghost1", null!));
    }

    [Fact]
    public void AddGhost_EmptyId_ThrowsArgumentException()
    {
        // Arrange
        using var manager = new GhostManager();
        var ghostData = CreateTestGhostData();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => manager.AddGhost("", ghostData));
    }

    #endregion

    #region RemoveGhost Tests

    [Fact]
    public void RemoveGhost_ExistingGhost_ReturnsTrue()
    {
        // Arrange
        using var manager = new GhostManager();
        manager.AddGhost("ghost1", CreateTestGhostData());

        // Act
        var result = manager.RemoveGhost("ghost1");

        // Assert
        Assert.True(result);
        Assert.Equal(0, manager.Count);
    }

    [Fact]
    public void RemoveGhost_NonExistentGhost_ReturnsFalse()
    {
        // Arrange
        using var manager = new GhostManager();

        // Act
        var result = manager.RemoveGhost("ghost1");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_RemovesAllGhosts()
    {
        // Arrange
        using var manager = new GhostManager();
        manager.AddGhost("ghost1", CreateTestGhostData("Ghost 1"));
        manager.AddGhost("ghost2", CreateTestGhostData("Ghost 2"));

        // Act
        manager.Clear();

        // Assert
        Assert.Equal(0, manager.Count);
    }

    #endregion

    #region GetPlayer Tests

    [Fact]
    public void GetPlayer_ExistingGhost_ReturnsPlayer()
    {
        // Arrange
        using var manager = new GhostManager();
        manager.AddGhost("ghost1", CreateTestGhostData());

        // Act
        var player = manager.GetPlayer("ghost1");

        // Assert
        Assert.NotNull(player);
        Assert.True(player.IsLoaded);
    }

    [Fact]
    public void GetPlayer_NonExistentGhost_ReturnsNull()
    {
        // Arrange
        using var manager = new GhostManager();

        // Act
        var player = manager.GetPlayer("ghost1");

        // Assert
        Assert.Null(player);
    }

    #endregion

    #region SetConfig Tests

    [Fact]
    public void SetConfig_ExistingGhost_UpdatesConfig()
    {
        // Arrange
        using var manager = new GhostManager();
        manager.AddGhost("ghost1", CreateTestGhostData());
        var newConfig = new GhostVisualConfig { Opacity = 0.9f };

        // Act
        var result = manager.SetConfig("ghost1", newConfig);

        // Assert
        Assert.True(result);
        Assert.Equal(0.9f, manager.GetConfig("ghost1")!.Opacity);
    }

    [Fact]
    public void SetConfig_NonExistentGhost_ReturnsFalse()
    {
        // Arrange
        using var manager = new GhostManager();
        var newConfig = new GhostVisualConfig { Opacity = 0.9f };

        // Act
        var result = manager.SetConfig("ghost1", newConfig);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region PlayAll/PauseAll/StopAll Tests

    [Fact]
    public void PlayAll_StartsAllGhosts()
    {
        // Arrange
        using var manager = new GhostManager();
        manager.AddGhost("ghost1", CreateTestGhostData("Ghost 1"));
        manager.AddGhost("ghost2", CreateTestGhostData("Ghost 2"));

        // Act
        manager.PlayAll();

        // Assert
        Assert.Equal(GhostPlaybackState.Playing, manager.GetPlayer("ghost1")!.State);
        Assert.Equal(GhostPlaybackState.Playing, manager.GetPlayer("ghost2")!.State);
    }

    [Fact]
    public void PauseAll_PausesAllGhosts()
    {
        // Arrange
        using var manager = new GhostManager();
        manager.AddGhost("ghost1", CreateTestGhostData("Ghost 1"));
        manager.AddGhost("ghost2", CreateTestGhostData("Ghost 2"));
        manager.PlayAll();

        // Act
        manager.PauseAll();

        // Assert
        Assert.Equal(GhostPlaybackState.Paused, manager.GetPlayer("ghost1")!.State);
        Assert.Equal(GhostPlaybackState.Paused, manager.GetPlayer("ghost2")!.State);
    }

    [Fact]
    public void StopAll_StopsAllGhosts()
    {
        // Arrange
        using var manager = new GhostManager();
        manager.AddGhost("ghost1", CreateTestGhostData("Ghost 1"));
        manager.AddGhost("ghost2", CreateTestGhostData("Ghost 2"));
        manager.PlayAll();
        manager.Update(1f);

        // Act
        manager.StopAll();

        // Assert
        Assert.Equal(GhostPlaybackState.Stopped, manager.GetPlayer("ghost1")!.State);
        Assert.Equal(GhostPlaybackState.Stopped, manager.GetPlayer("ghost2")!.State);
        Assert.Equal(0, manager.GetPlayer("ghost1")!.CurrentFrame);
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_UpdatesAllGhosts()
    {
        // Arrange
        using var manager = new GhostManager();
        manager.AddGhost("ghost1", CreateTestGhostData("Ghost 1"));
        manager.AddGhost("ghost2", CreateTestGhostData("Ghost 2"));
        manager.PlayAll();

        // Act
        manager.Update(1f);

        // Assert
        Assert.True(manager.GetPlayer("ghost1")!.Position.X > 0);
        Assert.True(manager.GetPlayer("ghost2")!.Position.X > 0);
    }

    #endregion

    #region UpdateByDistance Tests

    [Fact]
    public void UpdateByDistance_UpdatesDistanceSyncedGhosts()
    {
        // Arrange
        using var manager = new GhostManager();
        manager.AddGhost("ghost1", CreateTestGhostData());
        manager.SetAllSyncMode(GhostSyncMode.DistanceSynced);
        manager.PlayAll();

        // Act
        manager.UpdateByDistance(1.5f);

        // Assert
        var player = manager.GetPlayer("ghost1")!;
        Assert.True(player.Position.X > 1 && player.Position.X < 2);
    }

    #endregion

    #region SeekAllToTime Tests

    [Fact]
    public void SeekAllToTime_SeeksAllGhosts()
    {
        // Arrange
        using var manager = new GhostManager();
        manager.AddGhost("ghost1", CreateTestGhostData("Ghost 1"));
        manager.AddGhost("ghost2", CreateTestGhostData("Ghost 2"));

        // Act
        manager.SeekAllToTime(TimeSpan.FromSeconds(1.5));

        // Assert
        var player1 = manager.GetPlayer("ghost1")!;
        var player2 = manager.GetPlayer("ghost2")!;
        Assert.True(player1.Position.X > 1);
        Assert.True(player2.Position.X > 1);
    }

    #endregion

    #region SetAllSyncMode Tests

    [Fact]
    public void SetAllSyncMode_SetsAllGhostSyncModes()
    {
        // Arrange
        using var manager = new GhostManager();
        manager.AddGhost("ghost1", CreateTestGhostData("Ghost 1"));
        manager.AddGhost("ghost2", CreateTestGhostData("Ghost 2"));

        // Act
        manager.SetAllSyncMode(GhostSyncMode.FrameSynced);

        // Assert
        Assert.Equal(GhostSyncMode.FrameSynced, manager.GetPlayer("ghost1")!.SyncMode);
        Assert.Equal(GhostSyncMode.FrameSynced, manager.GetPlayer("ghost2")!.SyncMode);
        Assert.Equal(GhostSyncMode.FrameSynced, manager.DefaultSyncMode);
    }

    #endregion

    #region SetAllPlaybackSpeed Tests

    [Fact]
    public void SetAllPlaybackSpeed_SetsAllGhostSpeeds()
    {
        // Arrange
        using var manager = new GhostManager();
        manager.AddGhost("ghost1", CreateTestGhostData("Ghost 1"));
        manager.AddGhost("ghost2", CreateTestGhostData("Ghost 2"));

        // Act
        manager.SetAllPlaybackSpeed(2.0f);

        // Assert
        Assert.Equal(2.0f, manager.GetPlayer("ghost1")!.PlaybackSpeed);
        Assert.Equal(2.0f, manager.GetPlayer("ghost2")!.PlaybackSpeed);
    }

    [Fact]
    public void SetAllPlaybackSpeed_InvalidSpeed_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        using var manager = new GhostManager();
        manager.AddGhost("ghost1", CreateTestGhostData());

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => manager.SetAllPlaybackSpeed(0.1f));
    }

    #endregion

    #region ActiveGhosts Tests

    [Fact]
    public void ActiveGhosts_ReturnsAllGhosts()
    {
        // Arrange
        using var manager = new GhostManager();
        manager.AddGhost("ghost1", CreateTestGhostData("Ghost 1"));
        manager.AddGhost("ghost2", CreateTestGhostData("Ghost 2"));

        // Act
        var ghosts = manager.ActiveGhosts.ToList();

        // Assert
        Assert.Equal(2, ghosts.Count);
    }

    [Fact]
    public void ActiveGhosts_ReturnsGhostInstances()
    {
        // Arrange
        using var manager = new GhostManager();
        var config = new GhostVisualConfig { Opacity = 0.8f, Label = "Test Label" };
        manager.AddGhost("ghost1", CreateTestGhostData(), config);

        // Act
        var ghost = manager.ActiveGhosts.First();

        // Assert
        Assert.Equal("ghost1", ghost.Id);
        Assert.Equal(0.8f, ghost.Opacity);
        Assert.Equal("Test Label", ghost.Label);
    }

    #endregion

    #region ContainsGhost Tests

    [Fact]
    public void ContainsGhost_ExistingGhost_ReturnsTrue()
    {
        // Arrange
        using var manager = new GhostManager();
        manager.AddGhost("ghost1", CreateTestGhostData());

        // Act & Assert
        Assert.True(manager.ContainsGhost("ghost1"));
    }

    [Fact]
    public void ContainsGhost_NonExistentGhost_ReturnsFalse()
    {
        // Arrange
        using var manager = new GhostManager();

        // Act & Assert
        Assert.False(manager.ContainsGhost("ghost1"));
    }

    #endregion

    #region DefaultSyncMode Tests

    [Fact]
    public void DefaultSyncMode_Default_IsTimeSynced()
    {
        // Arrange
        using var manager = new GhostManager();

        // Assert
        Assert.Equal(GhostSyncMode.TimeSynced, manager.DefaultSyncMode);
    }

    [Fact]
    public void DefaultSyncMode_AppliedToNewGhosts()
    {
        // Arrange
        using var manager = new GhostManager();
        manager.DefaultSyncMode = GhostSyncMode.FrameSynced;

        // Act
        manager.AddGhost("ghost1", CreateTestGhostData());

        // Assert
        Assert.Equal(GhostSyncMode.FrameSynced, manager.GetPlayer("ghost1")!.SyncMode);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_DisposesAllPlayers()
    {
        // Arrange
        var manager = new GhostManager();
        manager.AddGhost("ghost1", CreateTestGhostData());
        var player = manager.GetPlayer("ghost1");

        // Act
        manager.Dispose();

        // Assert
        Assert.Throws<ObjectDisposedException>(() => player!.Play());
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var manager = new GhostManager();
        manager.AddGhost("ghost1", CreateTestGhostData());

        // Act & Assert - should not throw
        manager.Dispose();
        manager.Dispose();
    }

    [Fact]
    public void Dispose_ThrowsOnSubsequentOperations()
    {
        // Arrange
        var manager = new GhostManager();
        manager.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => manager.AddGhost("ghost1", CreateTestGhostData()));
    }

    #endregion
}
