using System.Numerics;
using KeenEyes.Navigation.Abstractions;

namespace KeenEyes.Navigation.Abstractions.Tests;

/// <summary>
/// Tests for <see cref="NavPoint"/>.
/// </summary>
public class NavPointTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var position = new Vector3(1f, 2f, 3f);
        var point = new NavPoint(position, NavAreaType.Road, 42);

        Assert.Equal(position, point.Position);
        Assert.Equal(NavAreaType.Road, point.AreaType);
        Assert.Equal(42u, point.PolygonId);
    }

    [Fact]
    public void Constructor_WithDefaults_UsesWalkableAreaType()
    {
        var position = new Vector3(5f, 0f, 10f);
        var point = new NavPoint(position);

        Assert.Equal(NavAreaType.Walkable, point.AreaType);
        Assert.Equal(0u, point.PolygonId);
    }

    [Fact]
    public void At_CreatesNavPointAtPosition()
    {
        var position = new Vector3(10f, 20f, 30f);
        var point = NavPoint.At(position);

        Assert.Equal(position, point.Position);
        Assert.Equal(NavAreaType.Walkable, point.AreaType);
    }

    [Fact]
    public void At_WithCoordinates_CreatesNavPointAtPosition()
    {
        var point = NavPoint.At(1f, 2f, 3f);

        Assert.Equal(new Vector3(1f, 2f, 3f), point.Position);
    }

    [Fact]
    public void DistanceTo_ReturnsCorrectDistance()
    {
        var point1 = NavPoint.At(0f, 0f, 0f);
        var point2 = NavPoint.At(3f, 4f, 0f);

        var distance = point1.DistanceTo(point2);

        Assert.Equal(5f, distance);
    }

    [Fact]
    public void DistanceSquaredTo_ReturnsCorrectSquaredDistance()
    {
        var point1 = NavPoint.At(0f, 0f, 0f);
        var point2 = NavPoint.At(3f, 4f, 0f);

        var distanceSquared = point1.DistanceSquaredTo(point2);

        Assert.Equal(25f, distanceSquared);
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var point1 = new NavPoint(new Vector3(1f, 2f, 3f), NavAreaType.Water, 5);
        var point2 = new NavPoint(new Vector3(1f, 2f, 3f), NavAreaType.Water, 5);

        Assert.Equal(point1, point2);
        Assert.True(point1 == point2);
    }

    [Fact]
    public void Equality_DifferentPosition_AreNotEqual()
    {
        var point1 = new NavPoint(new Vector3(1f, 2f, 3f), NavAreaType.Walkable, 0);
        var point2 = new NavPoint(new Vector3(4f, 5f, 6f), NavAreaType.Walkable, 0);

        Assert.NotEqual(point1, point2);
        Assert.True(point1 != point2);
    }

    [Fact]
    public void Equality_DifferentAreaType_AreNotEqual()
    {
        var point1 = new NavPoint(new Vector3(1f, 2f, 3f), NavAreaType.Walkable, 0);
        var point2 = new NavPoint(new Vector3(1f, 2f, 3f), NavAreaType.Water, 0);

        Assert.NotEqual(point1, point2);
    }

    [Fact]
    public void GetHashCode_SameForEqualPoints()
    {
        var point1 = new NavPoint(new Vector3(1f, 2f, 3f), NavAreaType.Road, 10);
        var point2 = new NavPoint(new Vector3(1f, 2f, 3f), NavAreaType.Road, 10);

        Assert.Equal(point1.GetHashCode(), point2.GetHashCode());
    }

    [Fact]
    public void WithExpression_ModifiesSingleProperty()
    {
        var original = new NavPoint(new Vector3(1f, 2f, 3f), NavAreaType.Walkable, 0);
        var modified = original with { AreaType = NavAreaType.Water };

        Assert.Equal(NavAreaType.Walkable, original.AreaType);
        Assert.Equal(NavAreaType.Water, modified.AreaType);
        Assert.Equal(original.Position, modified.Position);
    }
}
