using System.Numerics;

using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graphics.Tests;

/// <summary>
/// Tests for the <see cref="Rectangle"/> struct.
/// </summary>
public class RectangleTests
{
    #region Constructor and Properties

    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var rect = new Rectangle(10, 20, 100, 50);

        Assert.Equal(10, rect.X);
        Assert.Equal(20, rect.Y);
        Assert.Equal(100, rect.Width);
        Assert.Equal(50, rect.Height);
    }

    [Fact]
    public void Empty_HasZeroValues()
    {
        var empty = Rectangle.Empty;

        Assert.Equal(0, empty.X);
        Assert.Equal(0, empty.Y);
        Assert.Equal(0, empty.Width);
        Assert.Equal(0, empty.Height);
    }

    [Fact]
    public void Left_ReturnsX()
    {
        var rect = new Rectangle(10, 20, 100, 50);
        Assert.Equal(10, rect.Left);
    }

    [Fact]
    public void Top_ReturnsY()
    {
        var rect = new Rectangle(10, 20, 100, 50);
        Assert.Equal(20, rect.Top);
    }

    [Fact]
    public void Right_ReturnsXPlusWidth()
    {
        var rect = new Rectangle(10, 20, 100, 50);
        Assert.Equal(110, rect.Right);
    }

    [Fact]
    public void Bottom_ReturnsYPlusHeight()
    {
        var rect = new Rectangle(10, 20, 100, 50);
        Assert.Equal(70, rect.Bottom);
    }

    [Fact]
    public void Center_ReturnsCenterPoint()
    {
        var rect = new Rectangle(10, 20, 100, 50);
        var center = rect.Center;

        Assert.Equal(60, center.X);
        Assert.Equal(45, center.Y);
    }

    [Fact]
    public void Size_ReturnsWidthAndHeight()
    {
        var rect = new Rectangle(10, 20, 100, 50);
        var size = rect.Size;

        Assert.Equal(100, size.X);
        Assert.Equal(50, size.Y);
    }

    [Fact]
    public void TopLeft_ReturnsTopLeftCorner()
    {
        var rect = new Rectangle(10, 20, 100, 50);
        Assert.Equal(new Vector2(10, 20), rect.TopLeft);
    }

    [Fact]
    public void TopRight_ReturnsTopRightCorner()
    {
        var rect = new Rectangle(10, 20, 100, 50);
        Assert.Equal(new Vector2(110, 20), rect.TopRight);
    }

    [Fact]
    public void BottomLeft_ReturnsBottomLeftCorner()
    {
        var rect = new Rectangle(10, 20, 100, 50);
        Assert.Equal(new Vector2(10, 70), rect.BottomLeft);
    }

    [Fact]
    public void BottomRight_ReturnsBottomRightCorner()
    {
        var rect = new Rectangle(10, 20, 100, 50);
        Assert.Equal(new Vector2(110, 70), rect.BottomRight);
    }

    #endregion

    #region Contains Tests

    [Fact]
    public void Contains_PointInside_ReturnsTrue()
    {
        var rect = new Rectangle(0, 0, 100, 100);

        Assert.True(rect.Contains(new Vector2(50, 50)));
        Assert.True(rect.Contains(new Vector2(0, 0))); // Top-left is inside
        Assert.True(rect.Contains(new Vector2(99, 99))); // Just inside bottom-right
    }

    [Fact]
    public void Contains_PointOutside_ReturnsFalse()
    {
        var rect = new Rectangle(0, 0, 100, 100);

        Assert.False(rect.Contains(new Vector2(-1, 50)));
        Assert.False(rect.Contains(new Vector2(50, -1)));
        Assert.False(rect.Contains(new Vector2(100, 50))); // Right edge is outside
        Assert.False(rect.Contains(new Vector2(50, 100))); // Bottom edge is outside
    }

    [Fact]
    public void Contains_PointOnRightEdge_ReturnsFalse()
    {
        // The right edge is exclusive (point.X < Right)
        var rect = new Rectangle(0, 0, 100, 100);
        Assert.False(rect.Contains(new Vector2(100, 50)));
    }

    [Fact]
    public void Contains_PointOnBottomEdge_ReturnsFalse()
    {
        // The bottom edge is exclusive (point.Y < Bottom)
        var rect = new Rectangle(0, 0, 100, 100);
        Assert.False(rect.Contains(new Vector2(50, 100)));
    }

    #endregion

    #region Intersects Tests

    [Fact]
    public void Intersects_OverlappingRectangles_ReturnsTrue()
    {
        var rect1 = new Rectangle(0, 0, 100, 100);
        var rect2 = new Rectangle(50, 50, 100, 100);

        Assert.True(rect1.Intersects(rect2));
        Assert.True(rect2.Intersects(rect1)); // Symmetric
    }

    [Fact]
    public void Intersects_NonOverlapping_ReturnsFalse()
    {
        var rect1 = new Rectangle(0, 0, 100, 100);
        var rect2 = new Rectangle(200, 200, 100, 100);

        Assert.False(rect1.Intersects(rect2));
        Assert.False(rect2.Intersects(rect1));
    }

    [Fact]
    public void Intersects_TouchingEdges_ReturnsFalse()
    {
        // Rectangles that touch but don't overlap
        var rect1 = new Rectangle(0, 0, 100, 100);
        var rect2 = new Rectangle(100, 0, 100, 100); // Touches right edge

        Assert.False(rect1.Intersects(rect2));
    }

    [Fact]
    public void Intersects_ContainedRectangle_ReturnsTrue()
    {
        var outer = new Rectangle(0, 0, 100, 100);
        var inner = new Rectangle(25, 25, 50, 50);

        Assert.True(outer.Intersects(inner));
        Assert.True(inner.Intersects(outer));
    }

    #endregion

    #region Intersection Tests

    [Fact]
    public void Intersection_OverlappingRectangles_ReturnsIntersection()
    {
        var rect1 = new Rectangle(0, 0, 100, 100);
        var rect2 = new Rectangle(50, 50, 100, 100);

        var intersection = rect1.Intersection(rect2);

        Assert.Equal(50, intersection.X);
        Assert.Equal(50, intersection.Y);
        Assert.Equal(50, intersection.Width);
        Assert.Equal(50, intersection.Height);
    }

    [Fact]
    public void Intersection_NonOverlapping_ReturnsEmpty()
    {
        var rect1 = new Rectangle(0, 0, 100, 100);
        var rect2 = new Rectangle(200, 200, 100, 100);

        var intersection = rect1.Intersection(rect2);

        Assert.Equal(Rectangle.Empty, intersection);
    }

    [Fact]
    public void Intersection_ContainedRectangle_ReturnsInner()
    {
        var outer = new Rectangle(0, 0, 100, 100);
        var inner = new Rectangle(25, 25, 50, 50);

        var intersection = outer.Intersection(inner);

        Assert.Equal(inner, intersection);
    }

    [Fact]
    public void Intersection_IsSymmetric()
    {
        var rect1 = new Rectangle(0, 0, 100, 100);
        var rect2 = new Rectangle(50, 50, 100, 100);

        var intersection1 = rect1.Intersection(rect2);
        var intersection2 = rect2.Intersection(rect1);

        Assert.Equal(intersection1, intersection2);
    }

    #endregion

    #region FromLTRB Tests

    [Fact]
    public void FromLTRB_CreatesCorrectRectangle()
    {
        var rect = Rectangle.FromLTRB(10, 20, 110, 70);

        Assert.Equal(10, rect.X);
        Assert.Equal(20, rect.Y);
        Assert.Equal(100, rect.Width);
        Assert.Equal(50, rect.Height);
    }

    [Fact]
    public void FromLTRB_SameLeftRight_HasZeroWidth()
    {
        var rect = Rectangle.FromLTRB(50, 20, 50, 70);
        Assert.Equal(0, rect.Width);
    }

    [Fact]
    public void FromLTRB_SameTopBottom_HasZeroHeight()
    {
        var rect = Rectangle.FromLTRB(10, 50, 110, 50);
        Assert.Equal(0, rect.Height);
    }

    #endregion
}
