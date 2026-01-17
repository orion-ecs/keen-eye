namespace KeenEyes.TestBridge.Physics;

/// <summary>
/// Snapshot of a 3D vector for serialization.
/// </summary>
public sealed record Vector3Snapshot
{
    /// <summary>
    /// Gets the X component.
    /// </summary>
    public required float X { get; init; }

    /// <summary>
    /// Gets the Y component.
    /// </summary>
    public required float Y { get; init; }

    /// <summary>
    /// Gets the Z component.
    /// </summary>
    public required float Z { get; init; }
}
