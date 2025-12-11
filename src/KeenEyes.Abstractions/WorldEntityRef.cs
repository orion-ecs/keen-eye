namespace KeenEyes;

/// <summary>
/// Reference to an entity in another world.
/// </summary>
/// <remarks>
/// <para>
/// Used for scenarios where entities in different worlds need to reference each other,
/// such as client-server simulations where a client entity tracks its server counterpart.
/// </para>
/// <para>
/// This struct stores the world ID and entity, allowing safe cross-world references
/// that can be validated using <see cref="TryResolve"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Client entity tracking its server counterpart
/// [Component]
/// public partial struct NetworkedEntity
/// {
///     public WorldEntityRef ServerEntity;
/// }
///
/// // Usage
/// var serverEntity = serverWorld.Spawn().Build();
/// var clientEntity = clientWorld.Spawn()
///     .With(new NetworkedEntity
///     {
///         ServerEntity = new WorldEntityRef
///         {
///             WorldId = serverWorld.Id,
///             Entity = serverEntity
///         }
///     })
///     .Build();
///
/// // Later, resolve the reference
/// if (clientWorld.Get&lt;NetworkedEntity&gt;(clientEntity)
///     .ServerEntity
///     .TryResolve(new[] { serverWorld }, out var world, out var entity))
/// {
///     // Access the server entity
///     ref var serverPos = ref world!.Get&lt;Position&gt;(entity);
/// }
/// </code>
/// </example>
public readonly struct WorldEntityRef
{
    /// <summary>
    /// ID of the world containing the referenced entity.
    /// </summary>
    public Guid WorldId { get; init; }

    /// <summary>
    /// The entity within that world.
    /// </summary>
    public Entity Entity { get; init; }

    /// <summary>
    /// Attempts to resolve this reference to an actual entity.
    /// </summary>
    /// <param name="worlds">Collection of available worlds to search.</param>
    /// <param name="world">The world containing the entity, if found.</param>
    /// <param name="entity">The entity, if found and alive.</param>
    /// <returns>True if the reference was successfully resolved.</returns>
    /// <remarks>
    /// This method searches the provided worlds for one matching <see cref="WorldId"/>,
    /// then verifies that <see cref="Entity"/> is still alive in that world.
    /// If both checks pass, the method returns true and populates the out parameters.
    /// </remarks>
    public bool TryResolve(IEnumerable<IWorld> worlds, out IWorld? world, out Entity entity)
    {
        ArgumentNullException.ThrowIfNull(worlds);

        // Copy to local to avoid capturing 'this' in lambda (CS1673)
        var targetWorldId = WorldId;
        var targetEntity = Entity;

        var foundWorld = worlds.FirstOrDefault(w => w.Id == targetWorldId);
        if (foundWorld != null && foundWorld.IsAlive(targetEntity))
        {
            world = foundWorld;
            entity = targetEntity;
            return true;
        }

        world = null;
        entity = default;
        return false;
    }
}
