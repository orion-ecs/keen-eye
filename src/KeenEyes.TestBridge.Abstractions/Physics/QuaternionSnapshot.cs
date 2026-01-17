namespace KeenEyes.TestBridge.Physics;

/// <summary>
/// Snapshot of a quaternion for serialization.
/// </summary>
public sealed record QuaternionSnapshot
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

    /// <summary>
    /// Gets the W (scalar) component.
    /// </summary>
    public required float W { get; init; }
}
