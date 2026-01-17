namespace KeenEyes.TestBridge.Physics;

/// <summary>
/// Statistics about physics simulation state.
/// </summary>
public sealed record PhysicsStatisticsSnapshot
{
    /// <summary>
    /// Gets the total number of physics bodies (dynamic + kinematic).
    /// </summary>
    public required int BodyCount { get; init; }

    /// <summary>
    /// Gets the total number of static bodies.
    /// </summary>
    public required int StaticCount { get; init; }

    /// <summary>
    /// Gets the interpolation alpha for smooth rendering (0-1).
    /// </summary>
    public required float InterpolationAlpha { get; init; }

    /// <summary>
    /// Gets the total count of all physics objects.
    /// </summary>
    public int TotalCount => BodyCount + StaticCount;
}
