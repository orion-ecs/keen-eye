using System.Numerics;
using KeenEyes.Navigation.Abstractions;
using KeenEyes.Navigation.Abstractions.Components;

namespace KeenEyes.Navigation.Abstractions.Tests;

/// <summary>
/// Tests for <see cref="NavMeshObstacle"/>.
/// </summary>
public class NavMeshObstacleTests
{
    [Fact]
    public void Box_CreatesBoxShapedObstacle()
    {
        var size = new Vector3(2f, 3f, 4f);
        var obstacle = NavMeshObstacle.Box(size);

        Assert.Equal(ObstacleShape.Box, obstacle.Shape);
        Assert.Equal(size, obstacle.Size);
        Assert.True(obstacle.Carving);
    }

    [Fact]
    public void Box_WithoutCarving_DisablesCarving()
    {
        var obstacle = NavMeshObstacle.Box(new Vector3(1f, 1f, 1f), carving: false);

        Assert.False(obstacle.Carving);
    }

    [Fact]
    public void Box_SetsDefaultCarvingThresholds()
    {
        var obstacle = NavMeshObstacle.Box(new Vector3(1f, 1f, 1f));

        Assert.Equal(0.1f, obstacle.CarvingMoveThreshold);
        Assert.Equal(0.5f, obstacle.CarvingTimeToStationary);
    }

    [Fact]
    public void Cylinder_CreatesCylindricalObstacle()
    {
        var obstacle = NavMeshObstacle.Cylinder(1.5f, 2.0f);

        Assert.Equal(ObstacleShape.Cylinder, obstacle.Shape);
        Assert.Equal(1.5f, obstacle.Radius);
        Assert.Equal(2.0f, obstacle.Height);
        Assert.True(obstacle.Carving);
    }

    [Fact]
    public void Cylinder_WithoutCarving_DisablesCarving()
    {
        var obstacle = NavMeshObstacle.Cylinder(1f, 2f, carving: false);

        Assert.False(obstacle.Carving);
    }

    [Fact]
    public void Cylinder_SetsDefaultCarvingThresholds()
    {
        var obstacle = NavMeshObstacle.Cylinder(1f, 2f);

        Assert.Equal(0.1f, obstacle.CarvingMoveThreshold);
        Assert.Equal(0.5f, obstacle.CarvingTimeToStationary);
    }

    [Fact]
    public void Center_DefaultsToZero()
    {
        var obstacle = NavMeshObstacle.Box(new Vector3(1f, 1f, 1f));

        Assert.Equal(Vector3.Zero, obstacle.Center);
    }

    [Fact]
    public void Center_CanBeModified()
    {
        var obstacle = NavMeshObstacle.Box(new Vector3(1f, 1f, 1f));
        obstacle.Center = new Vector3(0f, 1f, 0f);

        Assert.Equal(new Vector3(0f, 1f, 0f), obstacle.Center);
    }
}
