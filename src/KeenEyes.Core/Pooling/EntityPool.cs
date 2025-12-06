namespace KeenEyes;

/// <summary>
/// Manages entity ID recycling with version tracking.
/// Reduces allocations by reusing entity IDs after entities are destroyed.
/// </summary>
/// <remarks>
/// <para>
/// When an entity is destroyed, its ID is returned to the pool for reuse.
/// The version number is incremented to invalidate any stale handles to the
/// previous entity that used that ID.
/// </para>
/// <para>
/// The pool uses a LIFO (stack) strategy for recycling, which improves cache
/// locality since recently freed IDs are more likely to still be warm in cache.
/// </para>
/// </remarks>
public sealed class EntityPool
{
    private readonly Stack<int> recycledIds = new();
    private readonly Dictionary<int, int> versions = [];
    private int nextNewId;

    /// <summary>
    /// Gets the number of entity IDs currently available for reuse.
    /// </summary>
    public int AvailableCount => recycledIds.Count;

    /// <summary>
    /// Gets the total number of entity IDs that have been allocated.
    /// </summary>
    public int TotalAllocated => nextNewId;

    /// <summary>
    /// Gets the number of entities currently in use.
    /// </summary>
    public int ActiveCount => nextNewId - recycledIds.Count;

    /// <summary>
    /// Gets the number of times entity IDs have been recycled.
    /// </summary>
    public long RecycleCount { get; private set; }

    /// <summary>
    /// Acquires an entity ID, either from the recycled pool or by allocating a new one.
    /// </summary>
    /// <returns>A new entity with a unique ID and appropriate version.</returns>
    public Entity Acquire()
    {
        int id;
        int version;

        if (recycledIds.TryPop(out id))
        {
            // Reuse recycled ID - version was already incremented on release
            version = versions[id];
            RecycleCount++;
        }
        else
        {
            // Allocate new ID
            id = Interlocked.Increment(ref nextNewId) - 1;
            version = 1;
            versions[id] = version;
        }

        return new Entity(id, version);
    }

    /// <summary>
    /// Releases an entity ID back to the pool for reuse.
    /// </summary>
    /// <param name="entity">The entity to release.</param>
    /// <returns>True if the entity was released, false if it was already released or invalid.</returns>
    public bool Release(Entity entity)
    {
        if (entity.Id < 0 || entity.Id >= nextNewId)
        {
            return false;
        }

        if (!versions.TryGetValue(entity.Id, out var currentVersion))
        {
            return false;
        }

        // Check if entity is still valid (versions match)
        if (currentVersion != entity.Version)
        {
            return false;
        }

        // Increment version to invalidate stale handles
        versions[entity.Id] = currentVersion + 1;

        // Add to recycled pool
        recycledIds.Push(entity.Id);

        return true;
    }

    /// <summary>
    /// Checks if an entity is currently valid (alive).
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <returns>True if the entity is valid.</returns>
    public bool IsValid(Entity entity)
    {
        if (entity.Id < 0 || entity.Id >= nextNewId)
        {
            return false;
        }

        return versions.TryGetValue(entity.Id, out var version) && version == entity.Version;
    }

    /// <summary>
    /// Gets the current version for an entity ID.
    /// </summary>
    /// <param name="entityId">The entity ID.</param>
    /// <returns>The current version, or -1 if the ID is invalid.</returns>
    public int GetVersion(int entityId)
    {
        return versions.TryGetValue(entityId, out var version) ? version : -1;
    }

    /// <summary>
    /// Clears the pool, resetting all state.
    /// </summary>
    public void Clear()
    {
        recycledIds.Clear();
        versions.Clear();
        nextNewId = 0;
        RecycleCount = 0;
    }
}
