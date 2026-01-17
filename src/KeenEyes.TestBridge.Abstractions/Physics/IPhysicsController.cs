namespace KeenEyes.TestBridge.Physics;

/// <summary>
/// Controller interface for physics debugging and inspection operations.
/// </summary>
/// <remarks>
/// <para>
/// This controller provides access to physics simulation state including raycasting,
/// overlap queries, body state inspection, and force application. It enables inspection
/// and manipulation of physics bodies for debugging and testing.
/// </para>
/// <para>
/// <strong>Note:</strong> Requires the PhysicsPlugin to be installed on the world
/// for full functionality.
/// </para>
/// </remarks>
public interface IPhysicsController
{
    #region Statistics

    /// <summary>
    /// Gets statistics about the physics simulation.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Statistics about physics bodies and simulation state.</returns>
    Task<PhysicsStatisticsSnapshot> GetStatisticsAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Raycasting and Queries

    /// <summary>
    /// Casts a ray and returns the first hit.
    /// </summary>
    /// <param name="origin">The starting point of the ray.</param>
    /// <param name="direction">The direction of the ray (should be normalized).</param>
    /// <param name="maxDistance">Maximum distance to check for hits.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Information about the hit, or null if nothing was hit.</returns>
    Task<RayHitSnapshot?> RaycastAsync(Vector3Snapshot origin, Vector3Snapshot direction, float maxDistance, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds all entities within a sphere.
    /// </summary>
    /// <param name="center">The center of the sphere.</param>
    /// <param name="radius">The radius of the sphere.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of entity IDs overlapping the sphere.</returns>
    Task<IReadOnlyList<int>> OverlapSphereAsync(Vector3Snapshot center, float radius, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds all entities within a box.
    /// </summary>
    /// <param name="center">The center of the box.</param>
    /// <param name="halfExtents">The half-extents of the box.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of entity IDs overlapping the box.</returns>
    Task<IReadOnlyList<int>> OverlapBoxAsync(Vector3Snapshot center, Vector3Snapshot halfExtents, CancellationToken cancellationToken = default);

    #endregion

    #region Body State

    /// <summary>
    /// Gets the state of a physics body.
    /// </summary>
    /// <param name="entityId">The entity ID to query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The body state, or null if the entity has no physics body.</returns>
    Task<PhysicsBodySnapshot?> GetBodyStateAsync(int entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the linear velocity of an entity.
    /// </summary>
    /// <param name="entityId">The entity ID to query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The linear velocity, or null if the entity has no physics body.</returns>
    Task<Vector3Snapshot?> GetVelocityAsync(int entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the linear velocity of an entity.
    /// </summary>
    /// <param name="entityId">The entity ID to modify.</param>
    /// <param name="velocity">The new velocity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the velocity was set successfully.</returns>
    Task<bool> SetVelocityAsync(int entityId, Vector3Snapshot velocity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a body is awake (not sleeping).
    /// </summary>
    /// <param name="entityId">The entity ID to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if awake, false if sleeping, null if no physics body.</returns>
    Task<bool?> IsAwakeAsync(int entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Wakes up a sleeping body.
    /// </summary>
    /// <param name="entityId">The entity ID to wake.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the entity was woken up successfully.</returns>
    Task<bool> WakeUpAsync(int entityId, CancellationToken cancellationToken = default);

    #endregion

    #region Forces and Impulses

    /// <summary>
    /// Applies a force to an entity at its center of mass.
    /// </summary>
    /// <param name="entityId">The entity ID to apply force to.</param>
    /// <param name="force">The force vector in world space.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the force was applied successfully.</returns>
    Task<bool> ApplyForceAsync(int entityId, Vector3Snapshot force, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies an impulse to an entity at its center of mass.
    /// </summary>
    /// <param name="entityId">The entity ID to apply impulse to.</param>
    /// <param name="impulse">The impulse vector in world space.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the impulse was applied successfully.</returns>
    Task<bool> ApplyImpulseAsync(int entityId, Vector3Snapshot impulse, CancellationToken cancellationToken = default);

    #endregion

    #region Gravity

    /// <summary>
    /// Gets the current gravity vector.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The gravity vector.</returns>
    Task<Vector3Snapshot> GetGravityAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the gravity vector for the simulation.
    /// </summary>
    /// <param name="gravity">The new gravity vector.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if gravity was set successfully.</returns>
    Task<bool> SetGravityAsync(Vector3Snapshot gravity, CancellationToken cancellationToken = default);

    #endregion
}
