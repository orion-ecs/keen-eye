namespace KeenEyes.TestBridge.UI;

/// <summary>
/// Snapshot of a UI element's bounds.
/// </summary>
public sealed record UIBoundsSnapshot
{
    /// <summary>
    /// Gets the X position in screen coordinates.
    /// </summary>
    public required float X { get; init; }

    /// <summary>
    /// Gets the Y position in screen coordinates.
    /// </summary>
    public required float Y { get; init; }

    /// <summary>
    /// Gets the width in pixels.
    /// </summary>
    public required float Width { get; init; }

    /// <summary>
    /// Gets the height in pixels.
    /// </summary>
    public required float Height { get; init; }

    /// <summary>
    /// Gets the local Z-index for render ordering.
    /// </summary>
    public required short LocalZIndex { get; init; }
}
