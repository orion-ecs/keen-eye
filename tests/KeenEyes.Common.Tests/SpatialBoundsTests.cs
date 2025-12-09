using System.Numerics;

namespace KeenEyes.Common.Tests;

/// <summary>
/// Tests for the SpatialBounds component.
/// </summary>
public class SpatialBoundsTests
{
    private const float Epsilon = 1e-5f;

    #region Constructor Tests

    [Fact]
    public void Constructor_SetsMinAndMax()
    {
        var min = new Vector3(1, 2, 3);
        var max = new Vector3(4, 5, 6);

        var bounds = new SpatialBounds(min, max);

        Assert.Equal(min, bounds.Min);
        Assert.Equal(max, bounds.Max);
    }

    #endregion

    #region FromCenterAndExtents Tests

    [Fact]
    public void FromCenterAndExtents_CreatesCorrectBounds()
    {
        var center = new Vector3(5, 5, 5);
        var extents = new Vector3(2, 3, 4);

        var bounds = SpatialBounds.FromCenterAndExtents(center, extents);

        Assert.Equal(new Vector3(3, 2, 1), bounds.Min);
        Assert.Equal(new Vector3(7, 8, 9), bounds.Max);
    }

    [Fact]
    public void FromCenterAndExtents_WithZeroExtents_CreatesPoint()
    {
        var center = new Vector3(1, 2, 3);
        var extents = Vector3.Zero;

        var bounds = SpatialBounds.FromCenterAndExtents(center, extents);

        Assert.Equal(center, bounds.Min);
        Assert.Equal(center, bounds.Max);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Center_ReturnsMiddlePoint()
    {
        var bounds = new SpatialBounds(new Vector3(0, 0, 0), new Vector3(10, 20, 30));

        var center = bounds.Center();

        Assert.Equal(5f, center.X, Epsilon);
        Assert.Equal(10f, center.Y, Epsilon);
        Assert.Equal(15f, center.Z, Epsilon);
    }

    [Fact]
    public void Extents_ReturnsHalfSize()
    {
        var bounds = new SpatialBounds(new Vector3(0, 0, 0), new Vector3(10, 20, 30));

        var extents = bounds.Extents();

        Assert.Equal(5f, extents.X, Epsilon);
        Assert.Equal(10f, extents.Y, Epsilon);
        Assert.Equal(15f, extents.Z, Epsilon);
    }

    [Fact]
    public void Size_ReturnsFullSize()
    {
        var bounds = new SpatialBounds(new Vector3(0, 0, 0), new Vector3(10, 20, 30));

        var size = bounds.Size();

        Assert.Equal(10f, size.X, Epsilon);
        Assert.Equal(20f, size.Y, Epsilon);
        Assert.Equal(30f, size.Z, Epsilon);
    }

    #endregion

    #region Contains Tests

    [Fact]
    public void Contains_PointInside_ReturnsTrue()
    {
        var bounds = new SpatialBounds(new Vector3(0, 0, 0), new Vector3(10, 10, 10));
        var point = new Vector3(5, 5, 5);

        Assert.True(bounds.Contains(point));
    }

    [Fact]
    public void Contains_PointOnBoundary_ReturnsTrue()
    {
        var bounds = new SpatialBounds(new Vector3(0, 0, 0), new Vector3(10, 10, 10));
        var point = new Vector3(10, 10, 10);

        Assert.True(bounds.Contains(point));
    }

    [Fact]
    public void Contains_PointOutside_ReturnsFalse()
    {
        var bounds = new SpatialBounds(new Vector3(0, 0, 0), new Vector3(10, 10, 10));
        var point = new Vector3(11, 5, 5);

        Assert.False(bounds.Contains(point));
    }

    [Fact]
    public void Contains_PointBelowMin_ReturnsFalse()
    {
        var bounds = new SpatialBounds(new Vector3(5, 5, 5), new Vector3(10, 10, 10));
        var point = new Vector3(4, 6, 6);

        Assert.False(bounds.Contains(point));
    }

    #endregion

    #region Intersects Tests

    [Fact]
    public void Intersects_OverlappingBounds_ReturnsTrue()
    {
        var bounds1 = new SpatialBounds(new Vector3(0, 0, 0), new Vector3(10, 10, 10));
        var bounds2 = new SpatialBounds(new Vector3(5, 5, 5), new Vector3(15, 15, 15));

        Assert.True(bounds1.Intersects(bounds2));
        Assert.True(bounds2.Intersects(bounds1));
    }

    [Fact]
    public void Intersects_TouchingBounds_ReturnsTrue()
    {
        var bounds1 = new SpatialBounds(new Vector3(0, 0, 0), new Vector3(10, 10, 10));
        var bounds2 = new SpatialBounds(new Vector3(10, 0, 0), new Vector3(20, 10, 10));

        Assert.True(bounds1.Intersects(bounds2));
    }

    [Fact]
    public void Intersects_SeparatedBounds_ReturnsFalse()
    {
        var bounds1 = new SpatialBounds(new Vector3(0, 0, 0), new Vector3(10, 10, 10));
        var bounds2 = new SpatialBounds(new Vector3(11, 0, 0), new Vector3(20, 10, 10));

        Assert.False(bounds1.Intersects(bounds2));
    }

    [Fact]
    public void Intersects_ContainedBounds_ReturnsTrue()
    {
        var outer = new SpatialBounds(new Vector3(0, 0, 0), new Vector3(10, 10, 10));
        var inner = new SpatialBounds(new Vector3(2, 2, 2), new Vector3(8, 8, 8));

        Assert.True(outer.Intersects(inner));
        Assert.True(inner.Intersects(outer));
    }

    #endregion

    #region Encapsulate Point Tests

    [Fact]
    public void EncapsulatePoint_ExpandsMin()
    {
        var bounds = new SpatialBounds(new Vector3(5, 5, 5), new Vector3(10, 10, 10));
        var point = new Vector3(2, 6, 6);

        bounds = bounds.Encapsulate(point);

        Assert.Equal(new Vector3(2, 5, 5), bounds.Min);
        Assert.Equal(new Vector3(10, 10, 10), bounds.Max);
    }

    [Fact]
    public void EncapsulatePoint_ExpandsMax()
    {
        var bounds = new SpatialBounds(new Vector3(5, 5, 5), new Vector3(10, 10, 10));
        var point = new Vector3(6, 6, 15);

        bounds = bounds.Encapsulate(point);

        Assert.Equal(new Vector3(5, 5, 5), bounds.Min);
        Assert.Equal(new Vector3(10, 10, 15), bounds.Max);
    }

    [Fact]
    public void EncapsulatePoint_PointInside_NoChange()
    {
        var bounds = new SpatialBounds(new Vector3(5, 5, 5), new Vector3(10, 10, 10));
        var point = new Vector3(7, 7, 7);

        bounds = bounds.Encapsulate(point);

        Assert.Equal(new Vector3(5, 5, 5), bounds.Min);
        Assert.Equal(new Vector3(10, 10, 10), bounds.Max);
    }

    #endregion

    #region Encapsulate Bounds Tests

    [Fact]
    public void EncapsulateBounds_ExpandsToInclude()
    {
        var bounds1 = new SpatialBounds(new Vector3(5, 5, 5), new Vector3(10, 10, 10));
        var bounds2 = new SpatialBounds(new Vector3(2, 8, 8), new Vector3(12, 15, 9));

        bounds1 = bounds1.Encapsulate(bounds2);

        Assert.Equal(new Vector3(2, 5, 5), bounds1.Min);
        Assert.Equal(new Vector3(12, 15, 10), bounds1.Max);
    }

    [Fact]
    public void EncapsulateBounds_ContainedBounds_NoChange()
    {
        var outer = new SpatialBounds(new Vector3(0, 0, 0), new Vector3(10, 10, 10));
        var inner = new SpatialBounds(new Vector3(2, 2, 2), new Vector3(8, 8, 8));

        outer = outer.Encapsulate(inner);

        Assert.Equal(new Vector3(0, 0, 0), outer.Min);
        Assert.Equal(new Vector3(10, 10, 10), outer.Max);
    }

    #endregion
}
