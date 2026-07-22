using System.Numerics;
using KeenEyes.Replay.Ghost;

namespace KeenEyes.Replay.Tests.Ghost;

/// <summary>
/// Unit tests for ghost trail support: <see cref="GhostVisualConfig"/> trail fields
/// and the trail-point provider on <see cref="GhostPlayer"/> / <see cref="GhostInstance"/>.
/// </summary>
public class GhostTrailTests
{
    // Five frames one unit apart along +X, one second apart.
    private static GhostData CreateTestGhostData()
    {
        return new GhostData
        {
            Name = "Trail Ghost",
            EntityName = "Player",
            RecordingStarted = DateTimeOffset.UtcNow,
            Duration = TimeSpan.FromSeconds(4),
            FrameCount = 5,
            Frames =
            [
                new GhostFrame(new Vector3(0, 0, 0), Quaternion.Identity, TimeSpan.FromSeconds(0)) { Distance = 0f },
                new GhostFrame(new Vector3(1, 0, 0), Quaternion.Identity, TimeSpan.FromSeconds(1)) { Distance = 1f },
                new GhostFrame(new Vector3(2, 0, 0), Quaternion.Identity, TimeSpan.FromSeconds(2)) { Distance = 2f },
                new GhostFrame(new Vector3(3, 0, 0), Quaternion.Identity, TimeSpan.FromSeconds(3)) { Distance = 3f },
                new GhostFrame(new Vector3(4, 0, 0), Quaternion.Identity, TimeSpan.FromSeconds(4)) { Distance = 4f }
            ]
        };
    }

    #region GhostVisualConfig Defaults

    [Fact]
    public void GhostVisualConfig_Default_HasTrailDisabled()
    {
        var config = new GhostVisualConfig();

        Assert.False(config.ShowTrail);
        Assert.Equal(60, config.TrailLength);
        Assert.Equal(0.5f, config.TrailFadeStart);
        Assert.Equal(0.1f, config.TrailWidth);
        Assert.Equal(TrailStyle.Line, config.TrailStyle);
    }

    [Fact]
    public void GhostVisualConfig_WithTrailSettings_RetainsValues()
    {
        var config = new GhostVisualConfig
        {
            ShowTrail = true,
            TrailLength = 30,
            TrailFadeStart = 0.2f,
            TrailWidth = 0.5f,
            TrailStyle = TrailStyle.Dots
        };

        Assert.True(config.ShowTrail);
        Assert.Equal(30, config.TrailLength);
        Assert.Equal(0.2f, config.TrailFadeStart);
        Assert.Equal(0.5f, config.TrailWidth);
        Assert.Equal(TrailStyle.Dots, config.TrailStyle);
    }

    #endregion

    #region Empty Before Playback

    [Fact]
    public void GetTrailPoints_BeforePlayback_ReturnsEmpty()
    {
        using var player = new GhostPlayer();
        player.Load(CreateTestGhostData());

        Span<Vector3> buffer = stackalloc Vector3[8];
        int count = player.GetTrailPoints(buffer);

        Assert.Equal(0, count);
    }

    [Fact]
    public void GetTrailPoints_NoGhostLoaded_ReturnsEmpty()
    {
        using var player = new GhostPlayer();

        Span<Vector3> buffer = stackalloc Vector3[8];
        int count = player.GetTrailPoints(buffer);

        Assert.Equal(0, count);
    }

    [Fact]
    public void GetTrailPoints_AfterStop_ReturnsEmpty()
    {
        using var player = new GhostPlayer();
        player.Load(CreateTestGhostData());
        player.SeekToFrame(3);

        // Sanity: seeking gives a trail.
        Assert.Equal(4, player.GetTrailPoints(64).Length);

        player.Play();
        player.Stop();

        Span<Vector3> buffer = stackalloc Vector3[8];
        Assert.Equal(0, player.GetTrailPoints(buffer));
    }

    #endregion

    #region Points, Order, and Count

    [Fact]
    public void GetTrailPoints_AtFrame_ReturnsPathOldestToNewest()
    {
        using var player = new GhostPlayer();
        player.Load(CreateTestGhostData());

        player.SeekToFrame(3);

        Span<Vector3> buffer = stackalloc Vector3[8];
        int count = player.GetTrailPoints(buffer);

        Assert.Equal(4, count);
        Assert.Equal(new Vector3(0, 0, 0), buffer[0]);
        Assert.Equal(new Vector3(1, 0, 0), buffer[1]);
        Assert.Equal(new Vector3(2, 0, 0), buffer[2]);
        Assert.Equal(new Vector3(3, 0, 0), buffer[3]);
    }

    [Fact]
    public void GetTrailPoints_AsPlaybackAdvances_GrowsWithPlayhead()
    {
        using var player = new GhostPlayer();
        player.Load(CreateTestGhostData());

        player.SeekToFrame(1);
        Assert.Equal(2, player.GetTrailPoints(64).Length);

        player.SeekToFrame(2);
        Assert.Equal(3, player.GetTrailPoints(64).Length);

        player.SeekToFrame(4);
        var trail = player.GetTrailPoints(64);
        Assert.Equal(5, trail.Length);
        Assert.Equal(new Vector3(4, 0, 0), trail[^1]);
    }

    [Fact]
    public void GetTrailPoints_WithFrameSyncedPlayback_TracksAdvancingFrames()
    {
        using var player = new GhostPlayer
        {
            SyncMode = GhostSyncMode.FrameSynced
        };
        player.Load(CreateTestGhostData());
        player.Play();

        player.Update(0f); // -> frame 1
        player.Update(0f); // -> frame 2

        var trail = player.GetTrailPoints(64);

        Assert.Equal(3, trail.Length);
        Assert.Equal(new Vector3(0, 0, 0), trail[0]);
        Assert.Equal(new Vector3(2, 0, 0), trail[^1]);
    }

    #endregion

    #region TrailLength / Buffer Bounds

    [Fact]
    public void GetTrailPoints_BufferSmallerThanHistory_ReturnsMostRecentPoints()
    {
        using var player = new GhostPlayer();
        player.Load(CreateTestGhostData());
        player.SeekToFrame(4);

        Span<Vector3> buffer = stackalloc Vector3[2];
        int count = player.GetTrailPoints(buffer);

        // Only the two most recent points fit.
        Assert.Equal(2, count);
        Assert.Equal(new Vector3(3, 0, 0), buffer[0]);
        Assert.Equal(new Vector3(4, 0, 0), buffer[1]);
    }

    [Fact]
    public void GetTrailPoints_RespectsMaxPoints()
    {
        using var player = new GhostPlayer();
        player.Load(CreateTestGhostData());
        player.SeekToFrame(4);

        var trail = player.GetTrailPoints(maxPoints: 3);

        Assert.Equal(3, trail.Length);
        Assert.Equal(new Vector3(2, 0, 0), trail[0]);
        Assert.Equal(new Vector3(4, 0, 0), trail[^1]);
    }

    [Fact]
    public void GetTrailPoints_MaxPointsZero_ReturnsEmpty()
    {
        using var player = new GhostPlayer();
        player.Load(CreateTestGhostData());
        player.SeekToFrame(4);

        Assert.Empty(player.GetTrailPoints(maxPoints: 0));
    }

    [Fact]
    public void GetTrailPoints_NegativeMaxPoints_ThrowsArgumentOutOfRange()
    {
        using var player = new GhostPlayer();
        player.Load(CreateTestGhostData());

        Assert.Throws<ArgumentOutOfRangeException>(() => player.GetTrailPoints(maxPoints: -1));
    }

    #endregion

    #region Seek Consistency

    [Fact]
    public void GetTrailPoints_AfterSeekToTime_ReturnsPathUpToThatTime()
    {
        using var player = new GhostPlayer();
        player.Load(CreateTestGhostData());

        player.SeekToTime(TimeSpan.FromSeconds(2));

        var trail = player.GetTrailPoints(64);

        // Frames 0..2 are at or before t=2s.
        Assert.Equal(3, trail.Length);
        Assert.Equal(new Vector3(0, 0, 0), trail[0]);
        Assert.Equal(new Vector3(2, 0, 0), trail[^1]);
    }

    [Fact]
    public void GetTrailPoints_SeekBackward_ShrinksTrail()
    {
        using var player = new GhostPlayer();
        player.Load(CreateTestGhostData());

        player.SeekToTime(TimeSpan.FromSeconds(4));
        Assert.Equal(5, player.GetTrailPoints(64).Length);

        player.SeekToTime(TimeSpan.FromSeconds(1));
        Assert.Equal(2, player.GetTrailPoints(64).Length);
    }

    #endregion

    #region Disposal

    [Fact]
    public void GetTrailPoints_AfterDispose_ThrowsObjectDisposed()
    {
        using var player = new GhostPlayer();
        player.Load(CreateTestGhostData());
        player.Dispose();

        Assert.Throws<ObjectDisposedException>(() =>
        {
            Span<Vector3> buffer = stackalloc Vector3[4];
            player.GetTrailPoints(buffer);
        });
    }

    #endregion

    #region GhostInstance Surface

    [Fact]
    public void GhostInstance_GetTrailPoints_MatchesPlayer()
    {
        using var manager = new GhostManager();
        var config = new GhostVisualConfig { ShowTrail = true, TrailLength = 32 };
        manager.AddGhost("ghost", CreateTestGhostData(), config);

        var instance = Assert.Single(manager.ActiveGhosts);
        instance.Player.SeekToFrame(2);

        Assert.True(instance.ShowTrail);

        Span<Vector3> buffer = stackalloc Vector3[8];
        int count = instance.GetTrailPoints(buffer);

        Assert.Equal(3, count);
        Assert.Equal(new Vector3(2, 0, 0), buffer[2]);
    }

    #endregion
}
