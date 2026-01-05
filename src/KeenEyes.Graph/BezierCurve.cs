using System.Numerics;

namespace KeenEyes.Graph;

/// <summary>
/// Utilities for cubic bezier curve tessellation and evaluation.
/// </summary>
/// <remarks>
/// <para>
/// Provides methods for tessellating bezier curves into line segments for rendering
/// with <c>DrawLineStrip</c>. Uses De Casteljau's algorithm for numerically stable evaluation.
/// </para>
/// <para>
/// Node graph connections typically use horizontal control points to create smooth
/// S-curves that flow naturally from left to right.
/// </para>
/// </remarks>
public static class BezierCurve
{
    /// <summary>
    /// Minimum number of line segments for bezier tessellation.
    /// </summary>
    public const int MinSegments = 8;

    /// <summary>
    /// Maximum number of line segments for bezier tessellation.
    /// </summary>
    public const int MaxSegments = 64;

    /// <summary>
    /// Tessellates a cubic bezier curve into line segments.
    /// </summary>
    /// <param name="p0">Start point.</param>
    /// <param name="p1">First control point.</param>
    /// <param name="p2">Second control point.</param>
    /// <param name="p3">End point.</param>
    /// <param name="segments">Number of line segments (more segments = smoother curve).</param>
    /// <returns>Array of points along the curve (length = segments + 1).</returns>
    public static Vector2[] Tessellate(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, int segments)
    {
        segments = Math.Max(1, segments);
        var points = new Vector2[segments + 1];

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            points[i] = EvaluateCubic(p0, p1, p2, p3, t);
        }

        return points;
    }

    /// <summary>
    /// Tessellates a cubic bezier curve into a preallocated span.
    /// </summary>
    /// <param name="p0">Start point.</param>
    /// <param name="p1">First control point.</param>
    /// <param name="p2">Second control point.</param>
    /// <param name="p3">End point.</param>
    /// <param name="output">Output span (must be at least 2 elements).</param>
    /// <returns>The number of points written (span length).</returns>
    public static int TessellateInto(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, Span<Vector2> output)
    {
        if (output.Length < 2)
        {
            return 0;
        }

        int segments = output.Length - 1;
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            output[i] = EvaluateCubic(p0, p1, p2, p3, t);
        }

        return output.Length;
    }

    /// <summary>
    /// Calculates an adaptive segment count based on curve length and zoom level.
    /// </summary>
    /// <param name="start">The start point.</param>
    /// <param name="end">The end point.</param>
    /// <param name="zoom">The current zoom level.</param>
    /// <returns>A segment count between <see cref="MinSegments"/> and <see cref="MaxSegments"/>.</returns>
    public static int CalculateSegmentCount(Vector2 start, Vector2 end, float zoom)
    {
        var distance = Vector2.Distance(start, end);
        var screenDistance = distance * zoom;

        // Approximately one segment per 20 screen pixels
        var segments = (int)(screenDistance / 20f);
        return Math.Clamp(segments, MinSegments, MaxSegments);
    }

    /// <summary>
    /// Generates horizontal control points for node graph style curves.
    /// </summary>
    /// <remarks>
    /// Creates control points that produce smooth horizontal S-curves typical in
    /// node graph editors. The control points extend horizontally from the start
    /// and end points.
    /// </remarks>
    /// <param name="start">The start point (output port position).</param>
    /// <param name="end">The end point (input port position).</param>
    /// <returns>A tuple of (first control point, second control point).</returns>
    public static (Vector2 Cp1, Vector2 Cp2) CalculateControlPoints(Vector2 start, Vector2 end)
    {
        var dx = MathF.Abs(end.X - start.X);

        // Control point offset: minimum 50 pixels, or half the horizontal distance
        var controlOffset = MathF.Max(dx * 0.5f, 50f);

        return (
            new Vector2(start.X + controlOffset, start.Y),
            new Vector2(end.X - controlOffset, end.Y)
        );
    }

    /// <summary>
    /// Evaluates a point on a cubic bezier curve at parameter t.
    /// </summary>
    /// <param name="p0">Start point.</param>
    /// <param name="p1">First control point.</param>
    /// <param name="p2">Second control point.</param>
    /// <param name="p3">End point.</param>
    /// <param name="t">Parameter value (0 to 1).</param>
    /// <returns>The point on the curve at parameter t.</returns>
    public static Vector2 EvaluateCubic(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        // Use De Casteljau's algorithm for numerical stability
        float u = 1f - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        // B(t) = (1-t)^3 * P0 + 3(1-t)^2 * t * P1 + 3(1-t) * t^2 * P2 + t^3 * P3
        return (uuu * p0) +
               (3f * uu * t * p1) +
               (3f * u * tt * p2) +
               (ttt * p3);
    }
}
