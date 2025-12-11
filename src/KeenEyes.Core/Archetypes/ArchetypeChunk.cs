using System.Runtime.CompilerServices;

namespace KeenEyes;

/// <summary>
/// A fixed-size block of entity storage within an archetype.
/// Chunks provide cache-friendly memory layout and enable pooling for reduced GC pressure.
/// </summary>
/// <remarks>
/// <para>
/// Each chunk stores up to <see cref="Capacity"/> entities with their components
/// in parallel arrays. The fixed size (typically sized to fit in L2 cache) enables:
/// </para>
/// <list type="bullet">
/// <item>Predictable memory layout for cache efficiency</item>
/// <item>Chunk reuse through pooling when entities are removed</item>
/// <item>Parallel processing of different chunks by multiple threads</item>
/// </list>
/// <para>
/// When a chunk becomes empty, it can be returned to the <see cref="ChunkPool"/>
/// for reuse by the same or different archetypes.
/// </para>
/// </remarks>
public sealed class ArchetypeChunk : IDisposable
{
    /// <summary>
    /// Default chunk capacity - sized to keep total chunk memory around 16KB.
    /// </summary>
    public const int DefaultCapacity = 128;

    private readonly Dictionary<Type, IComponentArray> componentArrays;
    private readonly Entity[] entities;
    private readonly Dictionary<int, int> entityIdToIndex;
    private int count;
    private bool isDisposed;

    /// <summary>
    /// Gets the archetype this chunk belongs to.
    /// </summary>
    public ArchetypeId ArchetypeId { get; }

    /// <summary>
    /// Gets the maximum number of entities this chunk can hold.
    /// </summary>
    public int Capacity { get; }

    /// <summary>
    /// Gets the current number of entities in this chunk.
    /// </summary>
    public int Count => count;

    /// <summary>
    /// Gets whether this chunk is full.
    /// </summary>
    public bool IsFull => count >= Capacity;

    /// <summary>
    /// Gets whether this chunk is empty.
    /// </summary>
    public bool IsEmpty => count == 0;

    /// <summary>
    /// Gets the amount of free space in this chunk.
    /// </summary>
    public int FreeSpace => Capacity - count;

    /// <summary>
    /// Creates a new chunk for the specified archetype.
    /// </summary>
    /// <param name="archetypeId">The archetype identifier.</param>
    /// <param name="componentInfos">The component information for this archetype.</param>
    /// <param name="capacity">The maximum entity capacity.</param>
    /// <remarks>
    /// Uses factory delegates from <see cref="ComponentInfo.CreateComponentArray"/> for
    /// AOT-compatible component array creation without reflection.
    /// </remarks>
    internal ArchetypeChunk(ArchetypeId archetypeId, IEnumerable<ComponentInfo> componentInfos, int capacity = DefaultCapacity)
    {
        ArchetypeId = archetypeId;
        Capacity = capacity;
        entities = new Entity[capacity];
        entityIdToIndex = new Dictionary<int, int>(capacity);
        componentArrays = [];

        foreach (var info in componentInfos)
        {
            // Use the factory delegate for AOT-compatible array creation
            var array = info.CreateComponentArray!(capacity);
            componentArrays[info.Type] = array;
        }
    }

    /// <summary>
    /// Adds an entity to this chunk.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <returns>The index where the entity was added.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the chunk is full.</exception>
    internal int AddEntity(Entity entity)
    {
        if (IsFull)
        {
            throw new InvalidOperationException("Chunk is full. Cannot add more entities.");
        }

        var index = count;
        entities[index] = entity;
        entityIdToIndex[entity.Id] = index;
        count++;
        return index;
    }

    /// <summary>
    /// Adds a component value for the most recently added entity.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="component">The component value.</param>
    internal void AddComponent<T>(in T component) where T : struct, IComponent
    {
        GetComponentArray<T>().Add(in component);
    }

    /// <summary>
    /// Adds a component value from a boxed object.
    /// </summary>
    internal void AddComponentBoxed(Type type, object value)
    {
        if (componentArrays.TryGetValue(type, out var array))
        {
            array.AddBoxed(value);
        }
    }

    /// <summary>
    /// Removes an entity from this chunk using swap-back removal.
    /// </summary>
    /// <param name="entity">The entity to remove.</param>
    /// <returns>The entity that was swapped into the removed position, or null.</returns>
    internal Entity? RemoveEntity(Entity entity)
    {
        if (!entityIdToIndex.TryGetValue(entity.Id, out var index))
        {
            return null;
        }

        var lastIndex = count - 1;
        Entity? swappedEntity = null;

        if (index != lastIndex)
        {
            // Swap with last entity
            var lastEntity = entities[lastIndex];
            entities[index] = lastEntity;
            entityIdToIndex[lastEntity.Id] = index;
            swappedEntity = lastEntity;

            // Swap all component arrays
            foreach (var array in componentArrays.Values)
            {
                array.RemoveAtSwapBack(index);
            }
        }
        else
        {
            // Just remove the last element
            foreach (var array in componentArrays.Values)
            {
                array.RemoveAtSwapBack(index);
            }
        }

        entities[lastIndex] = default;
        entityIdToIndex.Remove(entity.Id);
        count--;

        return swappedEntity;
    }

    /// <summary>
    /// Gets a reference to a component for the entity at the specified index.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Get<T>(int index) where T : struct, IComponent
    {
        return ref GetComponentArray<T>().GetRef(index);
    }

    /// <summary>
    /// Gets a readonly reference to a component.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly T GetReadonly<T>(int index) where T : struct, IComponent
    {
        return ref GetComponentArray<T>().GetReadonly(index);
    }

    /// <summary>
    /// Sets a component value at the specified index.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set<T>(int index, in T component) where T : struct, IComponent
    {
        GetComponentArray<T>().Set(index, in component);
    }

    /// <summary>
    /// Gets the boxed component value at the specified index.
    /// </summary>
    internal object GetBoxed(Type type, int index)
    {
        if (componentArrays.TryGetValue(type, out var array))
        {
            return array.GetBoxed(index);
        }
        throw new InvalidOperationException($"Chunk does not contain component type {type.Name}");
    }

    /// <summary>
    /// Sets a boxed component value at the specified index.
    /// </summary>
    internal void SetBoxed(Type type, int index, object value)
    {
        if (componentArrays.TryGetValue(type, out var array))
        {
            array.SetBoxed(index, value);
        }
    }

    /// <summary>
    /// Gets a span of components for efficient iteration.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> GetSpan<T>() where T : struct, IComponent
    {
        return GetComponentArray<T>().AsSpan();
    }

    /// <summary>
    /// Gets a readonly span of components.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<T> GetReadOnlySpan<T>() where T : struct, IComponent
    {
        return GetComponentArray<T>().AsReadOnlySpan();
    }

    /// <summary>
    /// Gets a span of all entities in this chunk.
    /// </summary>
    public ReadOnlySpan<Entity> GetEntities()
    {
        return new ReadOnlySpan<Entity>(entities, 0, count);
    }

    /// <summary>
    /// Gets the entity at the specified index.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Entity GetEntity(int index)
    {
        return entities[index];
    }

    /// <summary>
    /// Gets the index of an entity in this chunk.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetEntityIndex(Entity entity)
    {
        return entityIdToIndex.TryGetValue(entity.Id, out var index) ? index : -1;
    }

    /// <summary>
    /// Checks if this chunk contains the specified entity.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(Entity entity)
    {
        return entityIdToIndex.ContainsKey(entity.Id);
    }

    /// <summary>
    /// Checks if this chunk has a specific component type.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has<T>() where T : struct, IComponent
    {
        return componentArrays.ContainsKey(typeof(T));
    }

    /// <summary>
    /// Checks if this chunk has a specific component type.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has(Type type)
    {
        return componentArrays.ContainsKey(type);
    }

    /// <summary>
    /// Copies all shared components from one index to another chunk.
    /// </summary>
    internal void CopyComponentsTo(int sourceIndex, ArchetypeChunk destination)
    {
        foreach (var (type, sourceArray) in componentArrays)
        {
            if (destination.componentArrays.TryGetValue(type, out var destArray))
            {
                sourceArray.CopyTo(sourceIndex, destArray);
            }
        }
    }

    /// <summary>
    /// Resets this chunk for reuse (clears all data but keeps arrays allocated).
    /// </summary>
    internal void Reset()
    {
        Array.Clear(entities, 0, count);
        entityIdToIndex.Clear();

        foreach (var array in componentArrays.Values)
        {
            array.Clear();
        }

        count = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private FixedComponentArray<T> GetComponentArray<T>() where T : struct, IComponent
    {
        return (FixedComponentArray<T>)componentArrays[typeof(T)];
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        foreach (var array in componentArrays.Values)
        {
            if (array is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        componentArrays.Clear();
        entityIdToIndex.Clear();
        count = 0;
        isDisposed = true;
    }
}
