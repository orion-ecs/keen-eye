namespace KeenEyes.Replay.Ghost;

/// <summary>
/// Describes how a ghost's motion trail should be drawn by a renderer.
/// </summary>
/// <remarks>
/// <para>
/// The ghost system is data-only: it never renders trails itself. This enum is a
/// hint carried on <see cref="GhostVisualConfig"/> that a renderer reads to decide
/// how to visualize the points returned by
/// <see cref="GhostPlayer.GetTrailPoints(System.Span{System.Numerics.Vector3})"/>.
/// </para>
/// </remarks>
public enum TrailStyle
{
    /// <summary>
    /// A single connected line through the trail points (e.g. a polyline / line strip).
    /// </summary>
    Line,

    /// <summary>
    /// A 3D ribbon oriented along the ghost's facing direction.
    /// </summary>
    /// <remarks>
    /// Ribbons require an oriented 3D line renderer. The 2D reference renderers that
    /// ship with KeenEyes have no such primitive, so they fall back to
    /// <see cref="Line"/> when this style is requested.
    /// </remarks>
    Ribbon,

    /// <summary>
    /// Discrete position markers at each trail point rather than a connected line.
    /// </summary>
    Dots,

    /// <summary>
    /// A connected line whose color/opacity is graded along the path from tail to head.
    /// </summary>
    Gradient
}
