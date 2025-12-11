using System.Collections.Concurrent;

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
/// <para>
/// This class is fully thread-safe and supports concurrent access from multiple threads.
/// This enables parallel processing of systems that may allocate or release entities
/// simultaneously.
/// </para>
/// </remarks>
public sealed class EntityPool
{
    private readonly ConcurrentStack<int> recycledIds = new();
    private readonly ConcurrentDictionary<int, int> versions = new();
    private int nextNewId;
    private long recycleCount;

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
    public long RecycleCount => Interlocked.Read(ref recycleCount);

    /// <summary>
    /// Acquires an entity ID, either from the recycled pool or by allocating a new one.
    /// </summary>
    /// <returns>A new entity with a unique ID and appropriate version.</returns>
    /// <remarks>
    /// This method is thread-safe and can be called concurrently from multiple threads.
    /// </remarks>
    public Entity Acquire()
    {
        int id;
        int version;

        if (recycledIds.TryPop(out id))
        {
            // Reuse recycled ID - version was already incremented on release
            version = versions[id];
            Interlocked.Increment(ref recycleCount);
        }
        else
        {
            // Allocate new ID
            id = Interlocked.Increment(ref nextNewId) - 1;
            version = 1;
            versions.TryAdd(id, version);
        }

        return new Entity(id, version);
    }

    /// <summary>
    /// Releases an entity ID back to the pool for reuse.
    /// </summary>
    /// <param name="entity">The entity to release.</param>
    /// <returns>True if the entity was released, false if it was already released or invalid.</returns>
    /// <remarks>
    /// This method is thread-safe and can be called concurrently from multiple threads.
    /// If multiple threads attempt to release the same entity, only one will succeed.
    /// </remarks>
    public bool Release(Entity entity)
    {
        if (entity.Id < 0 || entity.Id >= Interlocked.CompareExchange(ref nextNewId, 0, 0))
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

        // Atomically increment version to invalidate stale handles
        // Use CompareExchange to ensure version hasn't changed since we read it
        var newVersion = currentVersion + 1;
        if (!versions.TryUpdate(entity.Id, newVersion, currentVersion))
        {
            // Version changed between read and update, entity was already released
            return false;
        }

        // Add to recycled pool
        recycledIds.Push(entity.Id);

        return true;
    }

    /// <summary>
    /// Checks if an entity is currently valid (alive).
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <returns>True if the entity is valid.</returns>
    /// <remarks>
    /// This method is thread-safe and can be called concurrently from multiple threads.
    /// </remarks>
    public bool IsValid(Entity entity)
    {
        if (entity.Id < 0 || entity.Id >= Interlocked.CompareExchange(ref nextNewId, 0, 0))
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
    /// <remarks>
    /// This method is thread-safe and can be called concurrently from multiple threads.
    /// </remarks>
    public int GetVersion(int entityId)
    {
        return versions.TryGetValue(entityId, out var version) ? version : -1;
    }

    /// <summary>
    /// Clears the pool, resetting all state.
    /// </summary>
    /// <remarks>
    /// This method is NOT thread-safe with respect to other operations.
    /// Ensure no other threads are accessing the pool during Clear.
    /// </remarks>
    public void Clear()
    {
        recycledIds.Clear();
        versions.Clear();
        Interlocked.Exchange(ref nextNewId, 0);
        Interlocked.Exchange(ref recycleCount, 0);
    }
}
