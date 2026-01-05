namespace KeenEyes.Graph.Abstractions;

/// <summary>
/// Defines the visual style for connection curves between graph nodes.
/// </summary>
public enum ConnectionStyle
{
    /// <summary>
    /// Cubic bezier curve with horizontal control points (smooth, curved).
    /// </summary>
    Bezier,

    /// <summary>
    /// Straight line directly between ports.
    /// </summary>
    Straight,

    /// <summary>
    /// Stepped horizontal-vertical-horizontal path (circuit-like).
    /// </summary>
    Stepped
}
