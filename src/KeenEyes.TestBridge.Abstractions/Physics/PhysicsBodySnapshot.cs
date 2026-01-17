namespace KeenEyes.TestBridge.Physics;

/// <summary>
/// Snapshot of a physics body's state.
/// </summary>
public sealed record PhysicsBodySnapshot
{
    /// <summary>
    /// Gets the entity ID.
    /// </summary>
    public required int EntityId { get; init; }

    /// <summary>
    /// Gets the world position.
    /// </summary>
    public required Vector3Snapshot Position { get; init; }

    /// <summary>
    /// Gets the rotation quaternion.
    /// </summary>
    public required QuaternionSnapshot Rotation { get; init; }

    /// <summary>
    /// Gets the linear velocity.
    /// </summary>
    public required Vector3Snapshot LinearVelocity { get; init; }

    /// <summary>
    /// Gets the angular velocity.
    /// </summary>
    public required Vector3Snapshot AngularVelocity { get; init; }

    /// <summary>
    /// Gets the mass in kilograms.
    /// </summary>
    public required float Mass { get; init; }

    /// <summary>
    /// Gets the body type (Dynamic, Kinematic, Static).
    /// </summary>
    public required string BodyType { get; init; }

    /// <summary>
    /// Gets whether the body is currently awake (not sleeping).
    /// </summary>
    public required bool IsAwake { get; init; }
}
