namespace KeenEyes.TestBridge.Navigation;

/// <summary>
/// Snapshot of a navigation point with position and metadata.
/// </summary>
public sealed record NavPointSnapshot
{
    /// <summary>
    /// Gets the X coordinate.
    /// </summary>
    public required float X { get; init; }

    /// <summary>
    /// Gets the Y coordinate.
    /// </summary>
    public required float Y { get; init; }

    /// <summary>
    /// Gets the Z coordinate.
    /// </summary>
    public required float Z { get; init; }

    /// <summary>
    /// Gets the navigation area type name at this point.
    /// </summary>
    public required string AreaType { get; init; }
}
