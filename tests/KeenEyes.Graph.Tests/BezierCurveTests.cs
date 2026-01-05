using System.Numerics;
using KeenEyes.Graph;

namespace KeenEyes.Graph.Tests;

public class BezierCurveTests
{
    #region Tessellation Tests

    [Fact]
    public void Tessellate_WithTwoSegments_ReturnsThreePoints()
    {
        var p0 = new Vector2(0, 0);
        var p1 = new Vector2(50, 0);
        var p2 = new Vector2(50, 100);
        var p3 = new Vector2(100, 100);

        var points = BezierCurve.Tessellate(p0, p1, p2, p3, segments: 2);

        Assert.Equal(3, points.Length);
    }

    [Fact]
    public void Tessellate_FirstPoint_IsStartPoint()
    {
        var p0 = new Vector2(0, 0);
        var p1 = new Vector2(50, 0);
        var p2 = new Vector2(50, 100);
        var p3 = new Vector2(100, 100);

        var points = BezierCurve.Tessellate(p0, p1, p2, p3, segments: 10);

        Assert.Equal(p0.X, points[0].X, precision: 4);
        Assert.Equal(p0.Y, points[0].Y, precision: 4);
    }

    [Fact]
    public void Tessellate_LastPoint_IsEndPoint()
    {
        var p0 = new Vector2(0, 0);
        var p1 = new Vector2(50, 0);
        var p2 = new Vector2(50, 100);
        var p3 = new Vector2(100, 100);

        var points = BezierCurve.Tessellate(p0, p1, p2, p3, segments: 10);

        Assert.Equal(p3.X, points[^1].X, precision: 4);
        Assert.Equal(p3.Y, points[^1].Y, precision: 4);
    }

    [Fact]
    public void TessellateInto_WithValidSpan_FillsSpan()
    {
        var p0 = new Vector2(0, 0);
        var p1 = new Vector2(50, 0);
        var p2 = new Vector2(50, 100);
        var p3 = new Vector2(100, 100);
        Span<Vector2> output = stackalloc Vector2[11];

        var count = BezierCurve.TessellateInto(p0, p1, p2, p3, output);

        Assert.Equal(11, count);
        Assert.Equal(p0.X, output[0].X, precision: 4);
        Assert.Equal(p3.X, output[^1].X, precision: 4);
    }

    [Fact]
    public void TessellateInto_WithTooSmallSpan_ReturnsZero()
    {
        var p0 = new Vector2(0, 0);
        var p1 = new Vector2(50, 0);
        var p2 = new Vector2(50, 100);
        var p3 = new Vector2(100, 100);
        Span<Vector2> output = stackalloc Vector2[1];

        var count = BezierCurve.TessellateInto(p0, p1, p2, p3, output);

        Assert.Equal(0, count);
    }

    #endregion

    #region Segment Count Tests

    [Fact]
    public void CalculateSegmentCount_ShortDistance_ReturnsMinimum()
    {
        var start = new Vector2(0, 0);
        var end = new Vector2(10, 0);
        var zoom = 1f;

        var segments = BezierCurve.CalculateSegmentCount(start, end, zoom);

        Assert.Equal(BezierCurve.MinSegments, segments);
    }

    [Fact]
    public void CalculateSegmentCount_LongDistance_ReturnsMoreSegments()
    {
        var start = new Vector2(0, 0);
        var end = new Vector2(500, 0);
        var zoom = 1f;

        var segments = BezierCurve.CalculateSegmentCount(start, end, zoom);

        Assert.True(segments > BezierCurve.MinSegments);
    }

    [Fact]
    public void CalculateSegmentCount_VeryLongDistance_ReturnsMaximum()
    {
        var start = new Vector2(0, 0);
        var end = new Vector2(10000, 0);
        var zoom = 1f;

        var segments = BezierCurve.CalculateSegmentCount(start, end, zoom);

        Assert.Equal(BezierCurve.MaxSegments, segments);
    }

    [Fact]
    public void CalculateSegmentCount_WithHighZoom_ReturnsMoreSegments()
    {
        var start = new Vector2(0, 0);
        var end = new Vector2(100, 0);
        var lowZoom = 0.5f;
        var highZoom = 2f;

        var lowZoomSegments = BezierCurve.CalculateSegmentCount(start, end, lowZoom);
        var highZoomSegments = BezierCurve.CalculateSegmentCount(start, end, highZoom);

        Assert.True(highZoomSegments > lowZoomSegments);
    }

    #endregion

    #region Control Point Tests

    [Fact]
    public void CalculateControlPoints_HorizontalLine_ProducesHorizontalControls()
    {
        var start = new Vector2(0, 50);
        var end = new Vector2(200, 50);

        var (cp1, cp2) = BezierCurve.CalculateControlPoints(start, end);

        // Control points should have same Y as their respective endpoints
        Assert.Equal(start.Y, cp1.Y, precision: 4);
        Assert.Equal(end.Y, cp2.Y, precision: 4);

        // First control point should be to the right of start
        Assert.True(cp1.X > start.X);

        // Second control point should be to the left of end
        Assert.True(cp2.X < end.X);
    }

    [Fact]
    public void CalculateControlPoints_ShortDistance_UsesMinimumOffset()
    {
        var start = new Vector2(0, 0);
        var end = new Vector2(10, 0);

        var (cp1, cp2) = BezierCurve.CalculateControlPoints(start, end);

        // With short distance, offset should be at least 50 pixels
        Assert.True(cp1.X >= 50f);
        Assert.True(cp2.X <= -40f); // end.X - 50 = 10 - 50 = -40
    }

    [Fact]
    public void CalculateControlPoints_ReversedPoints_StillProducesValidCurve()
    {
        var start = new Vector2(200, 50);
        var end = new Vector2(0, 50);

        var (cp1, cp2) = BezierCurve.CalculateControlPoints(start, end);

        // Curve should still work when going right-to-left
        // First control point extends horizontally from start
        Assert.True(cp1.X > start.X);
        // Second control point extends horizontally from end
        Assert.True(cp2.X < end.X);
    }

    [Fact]
    public void CalculateControlPoints_VerticalOffset_ProducesSymmetricCurve()
    {
        var start = new Vector2(0, 0);
        var end = new Vector2(200, 100);

        var (cp1, cp2) = BezierCurve.CalculateControlPoints(start, end);

        // Control points should preserve Y coordinates
        Assert.Equal(start.Y, cp1.Y, precision: 4);
        Assert.Equal(end.Y, cp2.Y, precision: 4);
    }

    #endregion

    #region Evaluation Tests

    [Fact]
    public void EvaluateCubic_AtT0_ReturnsStartPoint()
    {
        var p0 = new Vector2(0, 0);
        var p1 = new Vector2(25, 0);
        var p2 = new Vector2(75, 100);
        var p3 = new Vector2(100, 100);

        var result = BezierCurve.EvaluateCubic(p0, p1, p2, p3, t: 0f);

        Assert.Equal(p0.X, result.X, precision: 4);
        Assert.Equal(p0.Y, result.Y, precision: 4);
    }

    [Fact]
    public void EvaluateCubic_AtT1_ReturnsEndPoint()
    {
        var p0 = new Vector2(0, 0);
        var p1 = new Vector2(25, 0);
        var p2 = new Vector2(75, 100);
        var p3 = new Vector2(100, 100);

        var result = BezierCurve.EvaluateCubic(p0, p1, p2, p3, t: 1f);

        Assert.Equal(p3.X, result.X, precision: 4);
        Assert.Equal(p3.Y, result.Y, precision: 4);
    }

    [Fact]
    public void EvaluateCubic_AtT05_ReturnsMidpoint()
    {
        // For a straight line, midpoint should be at center
        var p0 = new Vector2(0, 0);
        var p1 = new Vector2(33.33f, 33.33f);
        var p2 = new Vector2(66.67f, 66.67f);
        var p3 = new Vector2(100, 100);

        var result = BezierCurve.EvaluateCubic(p0, p1, p2, p3, t: 0.5f);

        Assert.Equal(50f, result.X, precision: 1);
        Assert.Equal(50f, result.Y, precision: 1);
    }

    [Fact]
    public void EvaluateCubic_CollinearControlPoints_ProducesSymmetricCurve()
    {
        // When all points are collinear, the bezier curve stays on the line
        // but parameterization is NOT necessarily linear (t != position/length)
        var p0 = new Vector2(0, 0);
        var p1 = new Vector2(33.33f, 33.33f);
        var p2 = new Vector2(66.67f, 66.67f);
        var p3 = new Vector2(100, 100);

        // Curve should be symmetric around t=0.5
        var result025 = BezierCurve.EvaluateCubic(p0, p1, p2, p3, 0.25f);
        var result075 = BezierCurve.EvaluateCubic(p0, p1, p2, p3, 0.75f);

        // The sum of positions at symmetric t values should equal endpoint sum
        // (result025 + result075) should approximately equal (p0 + p3) = (100, 100)
        var sum = result025 + result075;
        Assert.Equal(100f, sum.X, precision: 1);
        Assert.Equal(100f, sum.Y, precision: 1);
    }

    #endregion
}
