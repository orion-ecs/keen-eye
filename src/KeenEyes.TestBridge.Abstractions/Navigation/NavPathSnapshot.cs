namespace KeenEyes.TestBridge.Navigation;

/// <summary>
/// Snapshot of a navigation path.
/// </summary>
public sealed record NavPathSnapshot
{
    /// <summary>
    /// Gets whether this path is valid (has waypoints).
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// Gets whether this path reaches the intended destination.
    /// </summary>
    public required bool IsComplete { get; init; }

    /// <summary>
    /// Gets the total traversal cost of the path.
    /// </summary>
    public required float TotalCost { get; init; }

    /// <summary>
    /// Gets the total length of the path in world units.
    /// </summary>
    public required float Length { get; init; }

    /// <summary>
    /// Gets the waypoints comprising the path.
    /// </summary>
    public required IReadOnlyList<NavPointSnapshot> Waypoints { get; init; }
}
