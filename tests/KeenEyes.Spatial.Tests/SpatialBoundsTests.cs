using System.Numerics;
using KeenEyes.Common;

namespace KeenEyes.Spatial.Tests;

/// <summary>
/// Tests for the SpatialBounds component.
/// </summary>
public class SpatialBoundsTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithMinAndMax_SetsProperties()
    {
        var min = new Vector3(-5, -5, -5);
        var max = new Vector3(5, 5, 5);

        var bounds = new SpatialBounds(min, max);

        Assert.Equal(min, bounds.Min);
        Assert.Equal(max, bounds.Max);
    }

    [Fact]
    public void Constructor_WithZeroSize_CreatesValidBounds()
    {
        var point = new Vector3(10, 20, 30);

        var bounds = new SpatialBounds(point, point);

        Assert.Equal(point, bounds.Min);
        Assert.Equal(point, bounds.Max);
    }

    [Fact]
    public void Constructor_WithNegativeCoordinates_CreatesValidBounds()
    {
        var min = new Vector3(-100, -200, -300);
        var max = new Vector3(-50, -100, -150);

        var bounds = new SpatialBounds(min, max);

        Assert.Equal(min, bounds.Min);
        Assert.Equal(max, bounds.Max);
    }

    #endregion

    #region FromCenterAndExtents Tests

    [Fact]
    public void FromCenterAndExtents_WithOriginCenter_CalculatesCorrectBounds()
    {
        var center = Vector3.Zero;
        var extents = new Vector3(5, 5, 5);

        var bounds = SpatialBounds.FromCenterAndExtents(center, extents);

        Assert.Equal(new Vector3(-5, -5, -5), bounds.Min);
        Assert.Equal(new Vector3(5, 5, 5), bounds.Max);
    }

    [Fact]
    public void FromCenterAndExtents_WithNonOriginCenter_CalculatesCorrectBounds()
    {
        var center = new Vector3(100, 200, 300);
        var extents = new Vector3(10, 20, 30);

        var bounds = SpatialBounds.FromCenterAndExtents(center, extents);

        Assert.Equal(new Vector3(90, 180, 270), bounds.Min);
        Assert.Equal(new Vector3(110, 220, 330), bounds.Max);
    }

    [Fact]
    public void FromCenterAndExtents_WithZeroExtents_CreatesPointBounds()
    {
        var center = new Vector3(50, 50, 50);
        var extents = Vector3.Zero;

        var bounds = SpatialBounds.FromCenterAndExtents(center, extents);

        Assert.Equal(center, bounds.Min);
        Assert.Equal(center, bounds.Max);
    }

    [Fact]
    public void FromCenterAndExtents_WithAsymmetricExtents_CalculatesCorrectBounds()
    {
        var center = new Vector3(0, 0, 0);
        var extents = new Vector3(10, 5, 2);

        var bounds = SpatialBounds.FromCenterAndExtents(center, extents);

        Assert.Equal(new Vector3(-10, -5, -2), bounds.Min);
        Assert.Equal(new Vector3(10, 5, 2), bounds.Max);
    }

    [Fact]
    public void FromCenterAndExtents_WithNegativeCenter_CalculatesCorrectBounds()
    {
        var center = new Vector3(-100, -50, -25);
        var extents = new Vector3(5, 5, 5);

        var bounds = SpatialBounds.FromCenterAndExtents(center, extents);

        Assert.Equal(new Vector3(-105, -55, -30), bounds.Min);
        Assert.Equal(new Vector3(-95, -45, -20), bounds.Max);
    }

    [Fact]
    public void FromCenterAndExtents_WithLargeExtents_CalculatesCorrectBounds()
    {
        var center = new Vector3(0, 0, 0);
        var extents = new Vector3(1000, 1000, 1000);

        var bounds = SpatialBounds.FromCenterAndExtents(center, extents);

        Assert.Equal(new Vector3(-1000, -1000, -1000), bounds.Min);
        Assert.Equal(new Vector3(1000, 1000, 1000), bounds.Max);
    }

    #endregion

    #region Mutation Tests

    [Fact]
    public void Min_CanBeMutated()
    {
        var bounds = new SpatialBounds(Vector3.Zero, Vector3.One)
        {
            Min = new Vector3(-10, -10, -10)
        };

        Assert.Equal(new Vector3(-10, -10, -10), bounds.Min);
    }

    [Fact]
    public void Max_CanBeMutated()
    {
        var bounds = new SpatialBounds(Vector3.Zero, Vector3.One)
        {
            Max = new Vector3(20, 20, 20)
        };

        Assert.Equal(new Vector3(20, 20, 20), bounds.Max);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void SpatialBounds_WorksWithEntity()
    {
        using var world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var bounds = new SpatialBounds(new Vector3(-1, -1, -1), new Vector3(1, 1, 1));

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(bounds)
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        Assert.True(world.Has<SpatialBounds>(entity));
        var retrievedBounds = world.Get<SpatialBounds>(entity);
        Assert.Equal(bounds.Min, retrievedBounds.Min);
        Assert.Equal(bounds.Max, retrievedBounds.Max);
    }

    [Fact]
    public void SpatialBounds_CanBeAddedDynamically()
    {
        using var world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        // Add bounds after creation
        world.Add(entity, new SpatialBounds(new Vector3(-5, -5, -5), new Vector3(5, 5, 5)));

        Assert.True(world.Has<SpatialBounds>(entity));
    }

    #endregion
}
