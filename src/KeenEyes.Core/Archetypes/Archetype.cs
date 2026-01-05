using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace KeenEyes;

/// <summary>
/// An archetype stores all entities that share the same set of component types.
/// Entities are stored in fixed-size chunks for cache-friendly iteration and reduced GC pressure.
/// </summary>
/// <remarks>
/// <para>
/// Each archetype maintains a list of <see cref="ArchetypeChunk"/> instances, each holding
/// up to a fixed number of entities. This chunked storage enables:
/// </para>
/// <list type="bullet">
/// <item>Cache-friendly iteration (chunks sized for L2 cache)</item>
/// <item>Chunk pooling for reduced allocations</item>
/// <item>Potential parallel processing of different chunks</item>
/// </list>
/// <para>
/// When an entity's component set changes (via Add or Remove), the entity must
/// migrate to a different archetype. The <see cref="ArchetypeManager"/> handles
/// these transitions.
/// </para>
/// </remarks>
public sealed class Archetype : IDisposable
{
    private readonly List<ArchetypeChunk> chunks;
    private readonly Dictionary<int, (int ChunkIndex, int IndexInChunk)> entityLocations;
    private readonly ImmutableArray<Type> componentTypesList;
    private readonly ImmutableArray<ComponentInfo> componentInfosList;
    private readonly ChunkPool? chunkPool;
    private int totalCount;

    /// <summary>
    /// Gets the unique identifier for this archetype.
    /// </summary>
    public ArchetypeId Id { get; }

    /// <summary>
    /// Gets the number of entities in this archetype.
    /// </summary>
    public int Count => totalCount;

    /// <summary>
    /// Gets the component types in this archetype.
    /// </summary>
    public IReadOnlyList<Type> ComponentTypes => componentTypesList;

    /// <summary>
    /// Gets all chunks in this archetype.
    /// </summary>
    public IReadOnlyList<ArchetypeChunk> Chunks => chunks;

    /// <summary>
    /// Gets the number of chunks in this archetype.
    /// </summary>
    public int ChunkCount => chunks.Count;

    /// <summary>
    /// Gets all entities in this archetype.
    /// </summary>
    public IEnumerable<Entity> Entities
    {
        get
        {
            foreach (var chunk in chunks)
            {
                for (var i = 0; i < chunk.Count; i++)
                {
                    yield return chunk.GetEntity(i);
                }
            }
        }
    }

    /// <summary>
    /// Creates a new archetype with the specified component types.
    /// </summary>
    /// <param name="id">The archetype identifier.</param>
    /// <param name="componentInfos">The component information for each type.</param>
    /// <param name="chunkPool">Optional chunk pool for chunk reuse.</param>
    internal Archetype(ArchetypeId id, IEnumerable<ComponentInfo> componentInfos, ChunkPool? chunkPool = null)
    {
        Id = id;
        this.chunkPool = chunkPool;
        chunks = [];
        entityLocations = [];
        componentInfosList = componentInfos.ToImmutableArray();
        componentTypesList = componentInfosList.Select(c => c.Type).ToImmutableArray();
    }

    /// <summary>
    /// Adds an entity to this archetype.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <returns>The index of the entity in this archetype (global index across all chunks).</returns>
    internal int AddEntity(Entity entity)
    {
        var chunk = GetOrCreateChunkWithSpace();
        var chunkIndex = chunks.IndexOf(chunk);
        var indexInChunk = chunk.AddEntity(entity);

        entityLocations[entity.Id] = (chunkIndex, indexInChunk);
        totalCount++;

        return (chunkIndex * ArchetypeChunk.DefaultCapacity) + indexInChunk;
    }

    /// <summary>
    /// Adds a component value for the most recently added entity.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="component">The component value.</param>
    internal void AddComponent<T>(in T component) where T : struct, IComponent
    {
        if (chunks.Count == 0)
        {
            throw new InvalidOperationException("Cannot add component: archetype has no chunks");
        }

        // Add to the last chunk (where the entity was just added)
        var lastChunk = chunks[^1];
        lastChunk.AddComponent(in component);
    }

    /// <summary>
    /// Adds a component value from a boxed object.
    /// </summary>
    internal void AddComponentBoxed(Type type, object value)
    {
        if (chunks.Count == 0)
        {
            throw new InvalidOperationException("Cannot add component: archetype has no chunks");
        }

        var lastChunk = chunks[^1];
        lastChunk.AddComponentBoxed(type, value);
    }

    /// <summary>
    /// Removes an entity from this archetype using swap-back removal.
    /// </summary>
    /// <param name="entity">The entity to remove.</param>
    /// <returns>The entity that was swapped into the removed position, or null.</returns>
    internal Entity? RemoveEntity(Entity entity)
    {
        if (!entityLocations.TryGetValue(entity.Id, out var location))
        {
            return null;
        }

        var (chunkIndex, _) = location;
        var chunk = chunks[chunkIndex];

        // Remove from chunk
        var swappedEntity = chunk.RemoveEntity(entity);

        // Update swapped entity's location if one was swapped
        if (swappedEntity.HasValue)
        {
            var newIndex = chunk.GetEntityIndex(swappedEntity.Value);
            entityLocations[swappedEntity.Value.Id] = (chunkIndex, newIndex);
        }

        entityLocations.Remove(entity.Id);
        totalCount--;

        // If chunk is now empty, return it to the pool
        if (chunk.IsEmpty && chunks.Count > 1)
        {
            chunks.RemoveAt(chunkIndex);

            // Update locations for entities in chunks after the removed one
            UpdateLocationsAfterChunkRemoval(chunkIndex);

            if (chunkPool != null)
            {
                chunkPool.Return(chunk);
            }
            else
            {
                chunk.Dispose();
            }
        }

        return swappedEntity;
    }

    /// <summary>
    /// Gets a reference to a component for the entity at the specified global index.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Get<T>(int globalIndex) where T : struct, IComponent
    {
        var (chunkIndex, indexInChunk) = GetChunkLocation(globalIndex);
        return ref chunks[chunkIndex].Get<T>(indexInChunk);
    }

    /// <summary>
    /// Gets a reference to a component for an entity.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetByEntity<T>(Entity entity) where T : struct, IComponent
    {
        var (chunkIndex, indexInChunk) = entityLocations[entity.Id];
        return ref chunks[chunkIndex].Get<T>(indexInChunk);
    }

    /// <summary>
    /// Gets a readonly reference to a component.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly T GetReadonly<T>(int globalIndex) where T : struct, IComponent
    {
        var (chunkIndex, indexInChunk) = GetChunkLocation(globalIndex);
        return ref chunks[chunkIndex].GetReadonly<T>(indexInChunk);
    }

    /// <summary>
    /// Sets a component value at the specified global index.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set<T>(int globalIndex, in T component) where T : struct, IComponent
    {
        var (chunkIndex, indexInChunk) = GetChunkLocation(globalIndex);
        chunks[chunkIndex].Set(indexInChunk, in component);
    }

    /// <summary>
    /// Sets a component value from a boxed object.
    /// </summary>
    internal void SetBoxed(Type type, int globalIndex, object value)
    {
        var (chunkIndex, indexInChunk) = GetChunkLocation(globalIndex);
        chunks[chunkIndex].SetBoxed(type, indexInChunk, value);
    }

    /// <summary>
    /// Gets the boxed component value at the specified global index.
    /// </summary>
    internal object GetBoxed(Type type, int globalIndex)
    {
        var (chunkIndex, indexInChunk) = GetChunkLocation(globalIndex);
        return chunks[chunkIndex].GetBoxed(type, indexInChunk);
    }

    /// <summary>
    /// Gets a span of components from a specific chunk for efficient iteration.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="chunkIndex">The chunk index.</param>
    /// <returns>A span over component values in that chunk.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> GetChunkSpan<T>(int chunkIndex) where T : struct, IComponent
    {
        return chunks[chunkIndex].GetSpan<T>();
    }

    /// <summary>
    /// Gets a readonly span of components from a specific chunk.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<T> GetChunkReadOnlySpan<T>(int chunkIndex) where T : struct, IComponent
    {
        return chunks[chunkIndex].GetReadOnlySpan<T>();
    }

    /// <summary>
    /// Gets a span of components for efficient iteration.
    /// Note: With chunked storage, this only returns the first chunk's span.
    /// For full iteration, use chunk-based methods.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Obsolete("Use GetChunkSpan for chunked archetypes. This method only returns the first chunk.")]
    public Span<T> GetSpan<T>() where T : struct, IComponent
    {
        if (chunks.Count == 0)
        {
            return Span<T>.Empty;
        }
        return chunks[0].GetSpan<T>();
    }

    /// <summary>
    /// Gets a readonly span of components.
    /// Note: With chunked storage, this only returns the first chunk's span.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Obsolete("Use GetChunkReadOnlySpan for chunked archetypes. This method only returns the first chunk.")]
    public ReadOnlySpan<T> GetReadOnlySpan<T>() where T : struct, IComponent
    {
        if (chunks.Count == 0)
        {
            return ReadOnlySpan<T>.Empty;
        }
        return chunks[0].GetReadOnlySpan<T>();
    }

    /// <summary>
    /// Checks if this archetype contains a specific component type.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has<T>() where T : struct, IComponent
    {
        return Has(typeof(T));
    }

    /// <summary>
    /// Checks if this archetype contains a specific component type.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has(Type type)
    {
        // Binary search since componentTypesList is sorted by FullName
        var left = 0;
        var right = componentTypesList.Length - 1;
        var targetName = type.FullName ?? type.Name;

        while (left <= right)
        {
            var mid = left + (right - left) / 2;
            var comparison = string.CompareOrdinal(componentTypesList[mid].FullName, targetName);

            if (comparison == 0)
            {
                return true;
            }

            if (comparison < 0)
            {
                left = mid + 1;
            }
            else
            {
                right = mid - 1;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the global index of an entity in this archetype.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetEntityIndex(Entity entity)
    {
        if (!entityLocations.TryGetValue(entity.Id, out var location))
        {
            return -1;
        }
        return (location.ChunkIndex * ArchetypeChunk.DefaultCapacity) + location.IndexInChunk;
    }

    /// <summary>
    /// Gets the entity location (chunk index and index within chunk).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (int ChunkIndex, int IndexInChunk) GetEntityLocation(Entity entity)
    {
        return entityLocations.TryGetValue(entity.Id, out var location) ? location : (-1, -1);
    }

    /// <summary>
    /// Gets the entity at the specified global index.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Entity GetEntity(int globalIndex)
    {
        var (chunkIndex, indexInChunk) = GetChunkLocation(globalIndex);
        return chunks[chunkIndex].GetEntity(indexInChunk);
    }

    /// <summary>
    /// Copies all shared components from one entity to another archetype.
    /// </summary>
    internal void CopySharedComponentsTo(int globalIndex, Archetype destination)
    {
        var (chunkIndex, indexInChunk) = GetChunkLocation(globalIndex);
        var sourceChunk = chunks[chunkIndex];
        var destChunk = destination.chunks[^1]; // Destination's last chunk

        sourceChunk.CopyComponentsTo(indexInChunk, destChunk);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var chunk in chunks)
        {
            if (chunkPool != null)
            {
                chunk.Reset();
                chunkPool.Return(chunk);
            }
            else
            {
                chunk.Dispose();
            }
        }
        chunks.Clear();
        entityLocations.Clear();
        totalCount = 0;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"Archetype[{Id}] ({Count} entities in {ChunkCount} chunks)";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private (int ChunkIndex, int IndexInChunk) GetChunkLocation(int globalIndex)
    {
        var chunkIndex = globalIndex / ArchetypeChunk.DefaultCapacity;
        var indexInChunk = globalIndex % ArchetypeChunk.DefaultCapacity;
        return (chunkIndex, indexInChunk);
    }

    private ArchetypeChunk GetOrCreateChunkWithSpace()
    {
        // Find a chunk with space
        foreach (var chunk in chunks)
        {
            if (!chunk.IsFull)
            {
                return chunk;
            }
        }

        // Need a new chunk
        ArchetypeChunk newChunk;
        if (chunkPool != null)
        {
            newChunk = chunkPool.Rent(Id, componentInfosList);
        }
        else
        {
            newChunk = new ArchetypeChunk(Id, componentInfosList);
        }

        chunks.Add(newChunk);
        return newChunk;
    }

    private void UpdateLocationsAfterChunkRemoval(int removedChunkIndex)
    {
        // Collect entity IDs that need their chunk index decremented.
        // We must not modify dictionary values during enumeration as it's undefined behavior.
        var entityIdsToUpdate = new List<int>();
        foreach (var kvp in entityLocations)
        {
            if (kvp.Value.ChunkIndex > removedChunkIndex)
            {
                entityIdsToUpdate.Add(kvp.Key);
            }
        }

        // Now update the locations for collected entities
        foreach (var entityId in entityIdsToUpdate)
        {
            var (chunkIndex, indexInChunk) = entityLocations[entityId];
            entityLocations[entityId] = (chunkIndex - 1, indexInChunk);
        }
    }
}
