namespace KeenEyes.TestBridge.Physics;

/// <summary>
/// Information about a raycast hit.
/// </summary>
public sealed record RayHitSnapshot
{
    /// <summary>
    /// Gets the entity ID that was hit.
    /// </summary>
    public required int EntityId { get; init; }

    /// <summary>
    /// Gets the world position of the hit point.
    /// </summary>
    public required Vector3Snapshot Position { get; init; }

    /// <summary>
    /// Gets the surface normal at the hit point.
    /// </summary>
    public required Vector3Snapshot Normal { get; init; }

    /// <summary>
    /// Gets the distance from the ray origin to the hit point.
    /// </summary>
    public required float Distance { get; init; }
}
