using System.Numerics;
using KeenEyes.Navigation.DotRecast;

namespace KeenEyes.Navigation.DotRecast.Tests;

/// <summary>
/// Tests for <see cref="NavMeshObstacleManager"/> class.
/// </summary>
public class NavMeshObstacleManagerTests
{
    private readonly NavMeshObstacleManager manager;

    public NavMeshObstacleManagerTests()
    {
        manager = new NavMeshObstacleManager();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_CreatesEmptyManager()
    {
        var m = new NavMeshObstacleManager();

        Assert.Equal(0, m.ObstacleCount);
    }

    #endregion

    #region AddBoxObstacle Tests

    [Fact]
    public void AddBoxObstacle_ReturnsValidId()
    {
        int id = manager.AddBoxObstacle(
            new Vector3(0, 0, 0),
            new Vector3(1, 1, 1));

        Assert.True(id > 0);
        Assert.Equal(1, manager.ObstacleCount);
    }

    [Fact]
    public void AddBoxObstacle_MultipleObstacles_UniqueIds()
    {
        int id1 = manager.AddBoxObstacle(new Vector3(0, 0, 0), new Vector3(1, 1, 1));
        int id2 = manager.AddBoxObstacle(new Vector3(5, 0, 5), new Vector3(1, 1, 1));
        int id3 = manager.AddBoxObstacle(new Vector3(10, 0, 10), new Vector3(1, 1, 1));

        Assert.NotEqual(id1, id2);
        Assert.NotEqual(id2, id3);
        Assert.NotEqual(id1, id3);
        Assert.Equal(3, manager.ObstacleCount);
    }

    [Fact]
    public void AddBoxObstacle_WithRotation_AddsObstacle()
    {
        int id = manager.AddBoxObstacle(
            new Vector3(0, 0, 0),
            new Vector3(2, 1, 1),
            rotation: MathF.PI / 4);

        Assert.True(id > 0);
    }

    #endregion

    #region AddCylinderObstacle Tests

    [Fact]
    public void AddCylinderObstacle_ReturnsValidId()
    {
        int id = manager.AddCylinderObstacle(
            new Vector3(0, 0, 0),
            radius: 1f,
            height: 2f);

        Assert.True(id > 0);
        Assert.Equal(1, manager.ObstacleCount);
    }

    #endregion

    #region UpdateObstacle Tests

    [Fact]
    public void UpdateObstacle_ValidId_ReturnsTrue()
    {
        int id = manager.AddBoxObstacle(new Vector3(0, 0, 0), new Vector3(1, 1, 1));

        bool updated = manager.UpdateObstacle(id, new Vector3(5, 0, 5));

        Assert.True(updated);
    }

    [Fact]
    public void UpdateObstacle_InvalidId_ReturnsFalse()
    {
        bool updated = manager.UpdateObstacle(9999, new Vector3(0, 0, 0));

        Assert.False(updated);
    }

    #endregion

    #region RemoveObstacle Tests

    [Fact]
    public void RemoveObstacle_ValidId_ReturnsTrue()
    {
        int id = manager.AddBoxObstacle(new Vector3(0, 0, 0), new Vector3(1, 1, 1));
        Assert.Equal(1, manager.ObstacleCount);

        bool removed = manager.RemoveObstacle(id);

        Assert.True(removed);
        Assert.Equal(0, manager.ObstacleCount);
    }

    [Fact]
    public void RemoveObstacle_InvalidId_ReturnsFalse()
    {
        bool removed = manager.RemoveObstacle(9999);

        Assert.False(removed);
    }

    [Fact]
    public void RemoveObstacle_AlreadyRemoved_ReturnsFalse()
    {
        int id = manager.AddBoxObstacle(new Vector3(0, 0, 0), new Vector3(1, 1, 1));
        manager.RemoveObstacle(id);

        bool removed = manager.RemoveObstacle(id);

        Assert.False(removed);
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_RemovesAllObstacles()
    {
        manager.AddBoxObstacle(new Vector3(0, 0, 0), new Vector3(1, 1, 1));
        manager.AddBoxObstacle(new Vector3(5, 0, 5), new Vector3(1, 1, 1));
        manager.AddCylinderObstacle(new Vector3(10, 0, 10), 1f, 2f);
        Assert.Equal(3, manager.ObstacleCount);

        manager.Clear();

        Assert.Equal(0, manager.ObstacleCount);
    }

    #endregion

    #region IsBlocked Tests

    [Fact]
    public void IsBlocked_InsideBoxObstacle_ReturnsTrue()
    {
        manager.AddBoxObstacle(new Vector3(5, 1, 5), new Vector3(2, 2, 2));

        bool blocked = manager.IsBlocked(new Vector3(5, 1, 5));

        Assert.True(blocked);
    }

    [Fact]
    public void IsBlocked_OutsideBoxObstacle_ReturnsFalse()
    {
        manager.AddBoxObstacle(new Vector3(5, 1, 5), new Vector3(1, 1, 1));

        bool blocked = manager.IsBlocked(new Vector3(15, 1, 15));

        Assert.False(blocked);
    }

    [Fact]
    public void IsBlocked_InsideCylinderObstacle_ReturnsTrue()
    {
        manager.AddCylinderObstacle(new Vector3(5, 0, 5), radius: 2f, height: 3f);

        bool blocked = manager.IsBlocked(new Vector3(5, 1, 5));

        Assert.True(blocked);
    }

    [Fact]
    public void IsBlocked_OutsideCylinderObstacle_ReturnsFalse()
    {
        manager.AddCylinderObstacle(new Vector3(5, 0, 5), radius: 1f, height: 2f);

        bool blocked = manager.IsBlocked(new Vector3(10, 1, 10));

        Assert.False(blocked);
    }

    [Fact]
    public void IsBlocked_AboveCylinderObstacle_ReturnsFalse()
    {
        manager.AddCylinderObstacle(new Vector3(5, 0, 5), radius: 2f, height: 2f);

        bool blocked = manager.IsBlocked(new Vector3(5, 5, 5));

        Assert.False(blocked);
    }

    [Fact]
    public void IsBlocked_NoObstacles_ReturnsFalse()
    {
        bool blocked = manager.IsBlocked(new Vector3(5, 0, 5));

        Assert.False(blocked);
    }

    [Fact]
    public void IsBlocked_AfterRemove_ReturnsFalse()
    {
        int id = manager.AddBoxObstacle(new Vector3(5, 1, 5), new Vector3(2, 2, 2));
        Assert.True(manager.IsBlocked(new Vector3(5, 1, 5)));

        manager.RemoveObstacle(id);

        Assert.False(manager.IsBlocked(new Vector3(5, 1, 5)));
    }

    [Fact]
    public void IsBlocked_RotatedBox_CorrectlyDetects()
    {
        // Add a rotated box
        manager.AddBoxObstacle(
            new Vector3(5, 1, 5),
            new Vector3(3, 1, 0.5f),  // Long and thin
            rotation: MathF.PI / 4);  // 45 degrees

        // Point on the rotated edge should be blocked
        // After 45 degree rotation, point at (5 + 2, 1, 5 + 2) should be inside
        bool blockedInside = manager.IsBlocked(new Vector3(5f, 1f, 5f));
        Assert.True(blockedInside);
    }

    #endregion
}
