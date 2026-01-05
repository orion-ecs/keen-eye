using System.Numerics;
using KeenEyes.Navigation.Abstractions;

namespace KeenEyes.Navigation.Abstractions.Tests;

/// <summary>
/// Tests for <see cref="NavPath"/>.
/// </summary>
public class NavPathTests
{
    [Fact]
    public void Empty_HasNoWaypoints()
    {
        var path = NavPath.Empty;

        Assert.False(path.IsValid);
        Assert.Empty(path);
        Assert.False(path.IsComplete);
    }

    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var waypoints = new[]
        {
            NavPoint.At(0f, 0f, 0f),
            NavPoint.At(10f, 0f, 0f)
        };
        var path = new NavPath(waypoints, true, 15.5f);

        Assert.True(path.IsValid);
        Assert.True(path.IsComplete);
        Assert.Equal(15.5f, path.TotalCost);
        Assert.Equal(2, path.Count);
    }

    [Fact]
    public void IsValid_WithWaypoints_ReturnsTrue()
    {
        var waypoints = new[] { NavPoint.At(0f, 0f, 0f) };
        var path = new NavPath(waypoints, true, 0f);

        Assert.True(path.IsValid);
    }

    [Fact]
    public void IsValid_WithEmptyWaypoints_ReturnsFalse()
    {
        var path = new NavPath([], false, 0f);

        Assert.False(path.IsValid);
    }

    [Fact]
    public void Start_ReturnsFirstWaypoint()
    {
        var waypoints = new[]
        {
            NavPoint.At(1f, 2f, 3f),
            NavPoint.At(4f, 5f, 6f)
        };
        var path = new NavPath(waypoints, true, 0f);

        Assert.Equal(new Vector3(1f, 2f, 3f), path.Start.Position);
    }

    [Fact]
    public void Start_OnEmptyPath_Throws()
    {
        var path = NavPath.Empty;

        Assert.Throws<InvalidOperationException>(() => path.Start);
    }

    [Fact]
    public void End_ReturnsLastWaypoint()
    {
        var waypoints = new[]
        {
            NavPoint.At(1f, 2f, 3f),
            NavPoint.At(4f, 5f, 6f)
        };
        var path = new NavPath(waypoints, true, 0f);

        Assert.Equal(new Vector3(4f, 5f, 6f), path.End.Position);
    }

    [Fact]
    public void End_OnEmptyPath_Throws()
    {
        var path = NavPath.Empty;

        Assert.Throws<InvalidOperationException>(() => path.End);
    }

    [Fact]
    public void Indexer_ReturnsCorrectWaypoint()
    {
        var waypoints = new[]
        {
            NavPoint.At(0f, 0f, 0f),
            NavPoint.At(5f, 0f, 0f),
            NavPoint.At(10f, 0f, 0f)
        };
        var path = new NavPath(waypoints, true, 0f);

        Assert.Equal(new Vector3(5f, 0f, 0f), path[1].Position);
    }

    [Fact]
    public void Length_CalculatesTotalDistance()
    {
        var waypoints = new[]
        {
            NavPoint.At(0f, 0f, 0f),
            NavPoint.At(3f, 4f, 0f), // 5 units from start
            NavPoint.At(6f, 8f, 0f)  // 5 units from previous
        };
        var path = new NavPath(waypoints, true, 0f);

        Assert.Equal(10f, path.Length);
    }

    [Fact]
    public void Length_WithSingleWaypoint_ReturnsZero()
    {
        var waypoints = new[] { NavPoint.At(5f, 5f, 5f) };
        var path = new NavPath(waypoints, true, 0f);

        Assert.Equal(0f, path.Length);
    }

    [Fact]
    public void Length_WithEmptyPath_ReturnsZero()
    {
        Assert.Equal(0f, NavPath.Empty.Length);
    }

    [Fact]
    public void SamplePosition_AtStart_ReturnsFirstPosition()
    {
        var waypoints = new[]
        {
            NavPoint.At(0f, 0f, 0f),
            NavPoint.At(10f, 0f, 0f)
        };
        var path = new NavPath(waypoints, true, 0f);

        var position = path.SamplePosition(0f);

        Assert.Equal(new Vector3(0f, 0f, 0f), position);
    }

    [Fact]
    public void SamplePosition_AtMiddle_ReturnsInterpolatedPosition()
    {
        var waypoints = new[]
        {
            NavPoint.At(0f, 0f, 0f),
            NavPoint.At(10f, 0f, 0f)
        };
        var path = new NavPath(waypoints, true, 0f);

        var position = path.SamplePosition(5f);

        Assert.Equal(new Vector3(5f, 0f, 0f), position);
    }

    [Fact]
    public void SamplePosition_BeyondEnd_ReturnsLastPosition()
    {
        var waypoints = new[]
        {
            NavPoint.At(0f, 0f, 0f),
            NavPoint.At(10f, 0f, 0f)
        };
        var path = new NavPath(waypoints, true, 0f);

        var position = path.SamplePosition(100f);

        Assert.Equal(new Vector3(10f, 0f, 0f), position);
    }

    [Fact]
    public void SamplePosition_OnEmptyPath_ReturnsZero()
    {
        var position = NavPath.Empty.SamplePosition(5f);

        Assert.Equal(Vector3.Zero, position);
    }

    [Fact]
    public void SamplePosition_AcrossMultipleSegments_ReturnsCorrectPosition()
    {
        var waypoints = new[]
        {
            NavPoint.At(0f, 0f, 0f),
            NavPoint.At(5f, 0f, 0f),
            NavPoint.At(10f, 0f, 0f)
        };
        var path = new NavPath(waypoints, true, 0f);

        var position = path.SamplePosition(7.5f);

        Assert.Equal(new Vector3(7.5f, 0f, 0f), position);
    }

    [Fact]
    public void AsSpan_ReturnsAllWaypoints()
    {
        var waypoints = new[]
        {
            NavPoint.At(0f, 0f, 0f),
            NavPoint.At(5f, 0f, 0f)
        };
        var path = new NavPath(waypoints, true, 0f);

        var span = path.AsSpan();

        Assert.Equal(2, span.Length);
        Assert.Equal(waypoints[0], span[0]);
        Assert.Equal(waypoints[1], span[1]);
    }

    [Fact]
    public void Enumeration_IteratesAllWaypoints()
    {
        var waypoints = new[]
        {
            NavPoint.At(0f, 0f, 0f),
            NavPoint.At(5f, 0f, 0f),
            NavPoint.At(10f, 0f, 0f)
        };
        var path = new NavPath(waypoints, true, 0f);

        var list = path.ToList();

        Assert.Equal(3, list.Count);
        Assert.Equal(waypoints[0], list[0]);
        Assert.Equal(waypoints[1], list[1]);
        Assert.Equal(waypoints[2], list[2]);
    }

    [Fact]
    public void IsComplete_False_IndicatesPartialPath()
    {
        var waypoints = new[]
        {
            NavPoint.At(0f, 0f, 0f),
            NavPoint.At(5f, 0f, 0f)
        };
        var path = new NavPath(waypoints, isComplete: false, totalCost: 5f);

        Assert.True(path.IsValid);
        Assert.False(path.IsComplete);
    }
}
