using System.Collections.Concurrent;

namespace KeenEyes;

/// <summary>
/// Manages pooling and reuse of archetype chunks to reduce GC pressure.
/// </summary>
/// <remarks>
/// <para>
/// When an archetype chunk becomes empty, it can be returned to the pool
/// for reuse. Chunks are pooled per-archetype (by ArchetypeId) since they
/// have the same component layout.
/// </para>
/// <para>
/// The pool uses a LIFO (stack-based) strategy for better cache locality -
/// recently returned chunks are more likely to still be in cache.
/// </para>
/// </remarks>
/// <param name="maxChunksPerArchetype">Maximum chunks to pool per archetype.</param>
public sealed class ChunkPool(int maxChunksPerArchetype = 64)
{
    private readonly ConcurrentDictionary<ArchetypeId, ConcurrentStack<ArchetypeChunk>> pools = new();
    private readonly int maxChunksPerArchetype = maxChunksPerArchetype;

    private long totalRented;
    private long totalReturned;
    private long totalCreated;
    private long totalDiscarded;

    /// <summary>
    /// Gets the total number of chunks rented from this pool.
    /// </summary>
    public long TotalRented => Interlocked.Read(ref totalRented);

    /// <summary>
    /// Gets the total number of chunks returned to this pool.
    /// </summary>
    public long TotalReturned => Interlocked.Read(ref totalReturned);

    /// <summary>
    /// Gets the total number of new chunks created (not from pool).
    /// </summary>
    public long TotalCreated => Interlocked.Read(ref totalCreated);

    /// <summary>
    /// Gets the total number of chunks discarded (pool was full).
    /// </summary>
    public long TotalDiscarded => Interlocked.Read(ref totalDiscarded);

    /// <summary>
    /// Gets the number of chunks currently in the pool (across all archetypes).
    /// </summary>
    public int PooledCount
    {
        get
        {
            var count = 0;
            foreach (var stack in pools.Values)
            {
                count += stack.Count;
            }
            return count;
        }
    }

    /// <summary>
    /// Gets the pool reuse rate (returned / rented).
    /// </summary>
    public double ReuseRate
    {
        get
        {
            var rented = TotalRented;
            var created = TotalCreated;
            if (rented == 0)
            {
                return 0;
            }
            return 1.0 - ((double)created / rented);
        }
    }

    /// <summary>
    /// Rents a chunk for the specified archetype.
    /// </summary>
    /// <param name="archetypeId">The archetype identifier.</param>
    /// <param name="componentTypes">The component types (used if creating new chunk).</param>
    /// <param name="capacity">The chunk capacity.</param>
    /// <returns>A chunk, either from the pool or newly created.</returns>
    public ArchetypeChunk Rent(ArchetypeId archetypeId, IEnumerable<Type> componentTypes, int capacity = ArchetypeChunk.DefaultCapacity)
    {
        Interlocked.Increment(ref totalRented);

        if (pools.TryGetValue(archetypeId, out var stack) && stack.TryPop(out var chunk))
        {
            // Got a pooled chunk - it's already reset
            return chunk;
        }

        // Need to create a new chunk
        Interlocked.Increment(ref totalCreated);
        return new ArchetypeChunk(archetypeId, componentTypes, capacity);
    }

    /// <summary>
    /// Returns a chunk to the pool for reuse.
    /// </summary>
    /// <param name="chunk">The chunk to return.</param>
    /// <returns>True if the chunk was pooled, false if it was discarded.</returns>
    public bool Return(ArchetypeChunk chunk)
    {
        if (chunk.IsEmpty)
        {
            // Reset the chunk for reuse
            chunk.Reset();
        }
        else
        {
            // Chunk must be empty to return - discard it
            Interlocked.Increment(ref totalDiscarded);
            chunk.Dispose();
            return false;
        }

        Interlocked.Increment(ref totalReturned);

        var stack = pools.GetOrAdd(chunk.ArchetypeId, _ => new ConcurrentStack<ArchetypeChunk>());

        if (stack.Count >= maxChunksPerArchetype)
        {
            // Pool is full for this archetype - discard
            Interlocked.Increment(ref totalDiscarded);
            chunk.Dispose();
            return false;
        }

        stack.Push(chunk);
        return true;
    }

    /// <summary>
    /// Clears all pooled chunks for the specified archetype.
    /// </summary>
    /// <param name="archetypeId">The archetype identifier.</param>
    public void ClearArchetype(ArchetypeId archetypeId)
    {
        if (pools.TryRemove(archetypeId, out var stack))
        {
            while (stack.TryPop(out var chunk))
            {
                chunk.Dispose();
            }
        }
    }

    /// <summary>
    /// Clears all pooled chunks.
    /// </summary>
    public void Clear()
    {
        foreach (var kvp in pools)
        {
            while (kvp.Value.TryPop(out var chunk))
            {
                chunk.Dispose();
            }
        }
        pools.Clear();
    }

    /// <summary>
    /// Gets statistics for this pool.
    /// </summary>
    public ChunkPoolStats GetStats()
    {
        return new ChunkPoolStats(
            TotalRented,
            TotalReturned,
            TotalCreated,
            TotalDiscarded,
            PooledCount,
            pools.Count);
    }
}

/// <summary>
/// Statistics for chunk pool usage.
/// </summary>
/// <param name="TotalRented">Total chunks rented.</param>
/// <param name="TotalReturned">Total chunks returned.</param>
/// <param name="TotalCreated">Total new chunks created.</param>
/// <param name="TotalDiscarded">Total chunks discarded.</param>
/// <param name="PooledCount">Current pooled chunk count.</param>
/// <param name="ArchetypeCount">Number of archetypes with pooled chunks.</param>
public readonly record struct ChunkPoolStats(
    long TotalRented,
    long TotalReturned,
    long TotalCreated,
    long TotalDiscarded,
    int PooledCount,
    int ArchetypeCount)
{
    /// <summary>
    /// Gets the pool hit rate (reused / rented).
    /// </summary>
    public double HitRate => TotalRented == 0 ? 0 : 1.0 - ((double)TotalCreated / TotalRented);
}
