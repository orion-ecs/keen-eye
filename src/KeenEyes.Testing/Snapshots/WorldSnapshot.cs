namespace KeenEyes.Testing.Snapshots;

/// <summary>
/// Represents a complete snapshot of a world's state at a point in time.
/// </summary>
/// <remarks>
/// <para>
/// WorldSnapshot captures the state of all entities and their components,
/// enabling comparison of world states before and after operations.
/// </para>
/// <para>
/// This is particularly useful for regression testing where you want to ensure
/// that operations produce consistent, reproducible results.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var before = WorldSnapshot.Create(world);
/// world.Update(1.0f);
/// var after = WorldSnapshot.Create(world);
///
/// var diff = SnapshotComparer.Compare(before, after);
/// Assert.Empty(diff.Differences);
/// </code>
/// </example>
public sealed class WorldSnapshot
{
    /// <summary>
    /// Gets the timestamp when this snapshot was captured.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the entity snapshots in this world snapshot.
    /// </summary>
    public Dictionary<int, EntitySnapshot> Entities { get; init; } = [];

    /// <summary>
    /// Gets the number of entities in this snapshot.
    /// </summary>
    public int EntityCount => Entities.Count;

    /// <summary>
    /// Gets all entity IDs in this snapshot.
    /// </summary>
    public IReadOnlyList<int> EntityIds => [.. Entities.Keys];

    /// <summary>
    /// Gets an entity snapshot by ID.
    /// </summary>
    /// <param name="entityId">The entity ID.</param>
    /// <returns>The entity snapshot, or null if not found.</returns>
    public EntitySnapshot? GetEntity(int entityId)
    {
        return Entities.TryGetValue(entityId, out var snapshot) ? snapshot : null;
    }

    /// <summary>
    /// Gets an entity snapshot by entity reference.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns>The entity snapshot, or null if not found.</returns>
    public EntitySnapshot? GetEntity(Entity entity)
    {
        return GetEntity(entity.Id);
    }

    /// <summary>
    /// Gets all entities that have the specified component type.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <returns>Entity snapshots with the component.</returns>
    public IEnumerable<EntitySnapshot> EntitiesWithComponent<T>() where T : IComponent
    {
        return Entities.Values.Where(e => e.HasComponent<T>());
    }

    /// <summary>
    /// Gets all entities that have the specified component type.
    /// </summary>
    /// <param name="componentTypeName">The component type name.</param>
    /// <returns>Entity snapshots with the component.</returns>
    public IEnumerable<EntitySnapshot> EntitiesWithComponent(string componentTypeName)
    {
        return Entities.Values.Where(e => e.HasComponent(componentTypeName));
    }

    /// <summary>
    /// Gets all unique component types across all entities.
    /// </summary>
    public IReadOnlyList<string> AllComponentTypes
    {
        get
        {
            return Entities.Values
                .SelectMany(e => e.ComponentTypes)
                .Distinct()
                .Order()
                .ToList();
        }
    }

    /// <summary>
    /// Creates a snapshot of the entire world.
    /// </summary>
    /// <param name="world">The world to snapshot.</param>
    /// <returns>A complete snapshot of the world's state.</returns>
    public static WorldSnapshot Create(World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        var snapshot = new WorldSnapshot();

        foreach (var entity in world.GetAllEntities())
        {
            var entitySnapshot = EntitySnapshot.Create(world, entity);
            snapshot.Entities[entity.Id] = entitySnapshot;
        }

        return snapshot;
    }

    /// <summary>
    /// Creates a snapshot of specific entities in the world.
    /// </summary>
    /// <param name="world">The world containing the entities.</param>
    /// <param name="entities">The entities to snapshot.</param>
    /// <returns>A snapshot containing only the specified entities.</returns>
    public static WorldSnapshot Create(World world, IEnumerable<Entity> entities)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(entities);

        var snapshot = new WorldSnapshot();

        foreach (var entity in entities)
        {
            if (world.IsAlive(entity))
            {
                var entitySnapshot = EntitySnapshot.Create(world, entity);
                snapshot.Entities[entity.Id] = entitySnapshot;
            }
        }

        return snapshot;
    }

    /// <summary>
    /// Returns a string representation of this snapshot.
    /// </summary>
    public override string ToString()
    {
        return $"WorldSnapshot {{ Timestamp = {Timestamp:O}, EntityCount = {EntityCount} }}";
    }
}
