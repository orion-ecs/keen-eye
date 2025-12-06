using System.Runtime.CompilerServices;

namespace KeenEyes;

/// <summary>
/// An archetype stores all entities that share the same set of component types.
/// Entities are stored contiguously in memory for cache-friendly iteration.
/// </summary>
/// <remarks>
/// <para>
/// Each archetype maintains parallel arrays for entity IDs and components.
/// This enables fast iteration over all entities with a specific component combination.
/// </para>
/// <para>
/// When an entity's component set changes (via Add or Remove), the entity must
/// migrate to a different archetype. The <see cref="ArchetypeManager"/> handles
/// these transitions.
/// </para>
/// </remarks>
public sealed class Archetype : IDisposable
{
    private readonly Dictionary<Type, IComponentArray> componentArrays;
    private readonly List<Entity> entities;
    private readonly Dictionary<int, int> entityIdToIndex;

    /// <summary>
    /// Gets the unique identifier for this archetype.
    /// </summary>
    public ArchetypeId Id { get; }

    /// <summary>
    /// Gets the number of entities in this archetype.
    /// </summary>
    public int Count => entities.Count;

    /// <summary>
    /// Gets the component types in this archetype.
    /// </summary>
    public IReadOnlyList<Type> ComponentTypes => [.. Id.ComponentTypes];

    /// <summary>
    /// Gets all entities in this archetype.
    /// </summary>
    public IReadOnlyList<Entity> Entities => entities;

    /// <summary>
    /// Creates a new archetype with the specified component types.
    /// </summary>
    /// <param name="id">The archetype identifier.</param>
    /// <param name="componentInfos">The component information for each type.</param>
    internal Archetype(ArchetypeId id, IEnumerable<ComponentInfo> componentInfos)
    {
        Id = id;
        componentArrays = [];
        entities = [];
        entityIdToIndex = [];

        foreach (var info in componentInfos)
        {
            // Create typed component array using reflection
            var arrayType = typeof(ComponentArray<>).MakeGenericType(info.Type);
            var array = (IComponentArray)Activator.CreateInstance(arrayType)!;
            componentArrays[info.Type] = array;
        }
    }

    /// <summary>
    /// Adds an entity to this archetype.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <returns>The index of the entity in this archetype.</returns>
    internal int AddEntity(Entity entity)
    {
        var index = entities.Count;
        entities.Add(entity);
        entityIdToIndex[entity.Id] = index;
        return index;
    }

    /// <summary>
    /// Adds a component value for the entity at the specified index.
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
    /// Removes an entity from this archetype using swap-back removal.
    /// </summary>
    /// <param name="entity">The entity to remove.</param>
    /// <returns>The entity that was swapped into the removed position, or null if the entity was the last one.</returns>
    internal Entity? RemoveEntity(Entity entity)
    {
        if (!entityIdToIndex.TryGetValue(entity.Id, out var index))
        {
            return null;
        }

        var lastIndex = entities.Count - 1;
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

        entities.RemoveAt(lastIndex);
        entityIdToIndex.Remove(entity.Id);

        return swappedEntity;
    }

    /// <summary>
    /// Gets a reference to a component for the entity at the specified index.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="index">The entity index in this archetype.</param>
    /// <returns>A reference to the component.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Get<T>(int index) where T : struct, IComponent
    {
        return ref GetComponentArray<T>().GetRef(index);
    }

    /// <summary>
    /// Gets a readonly reference to a component for the entity at the specified index.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="index">The entity index in this archetype.</param>
    /// <returns>A readonly reference to the component.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly T GetReadonly<T>(int index) where T : struct, IComponent
    {
        return ref GetComponentArray<T>().GetReadonly(index);
    }

    /// <summary>
    /// Sets a component value for the entity at the specified index.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="index">The entity index in this archetype.</param>
    /// <param name="component">The new component value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set<T>(int index, in T component) where T : struct, IComponent
    {
        GetComponentArray<T>().Set(index, in component);
    }

    /// <summary>
    /// Sets a component value from a boxed object.
    /// </summary>
    internal void SetBoxed(Type type, int index, object value)
    {
        if (componentArrays.TryGetValue(type, out var array))
        {
            array.SetBoxed(index, value);
        }
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
        throw new InvalidOperationException($"Archetype does not contain component type {type.Name}");
    }

    /// <summary>
    /// Gets a span of components for efficient iteration.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <returns>A span over all component values.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> GetSpan<T>() where T : struct, IComponent
    {
        return GetComponentArray<T>().AsSpan();
    }

    /// <summary>
    /// Gets a readonly span of components for efficient iteration.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <returns>A readonly span over all component values.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<T> GetReadOnlySpan<T>() where T : struct, IComponent
    {
        return GetComponentArray<T>().AsReadOnlySpan();
    }

    /// <summary>
    /// Checks if this archetype contains a specific component type.
    /// </summary>
    /// <typeparam name="T">The component type to check.</typeparam>
    /// <returns>True if this archetype contains the component type.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has<T>() where T : struct, IComponent
    {
        return componentArrays.ContainsKey(typeof(T));
    }

    /// <summary>
    /// Checks if this archetype contains a specific component type.
    /// </summary>
    /// <param name="type">The component type to check.</param>
    /// <returns>True if this archetype contains the component type.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has(Type type)
    {
        return componentArrays.ContainsKey(type);
    }

    /// <summary>
    /// Gets the index of an entity in this archetype.
    /// </summary>
    /// <param name="entity">The entity to look up.</param>
    /// <returns>The index, or -1 if not found.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetEntityIndex(Entity entity)
    {
        return entityIdToIndex.TryGetValue(entity.Id, out var index) ? index : -1;
    }

    /// <summary>
    /// Gets the entity at the specified index.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns>The entity at that index.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Entity GetEntity(int index)
    {
        return entities[index];
    }

    /// <summary>
    /// Copies all shared components from one entity to another archetype.
    /// Used during entity migration when adding/removing components.
    /// </summary>
    /// <param name="sourceIndex">The index in this archetype.</param>
    /// <param name="destination">The destination archetype.</param>
    internal void CopySharedComponentsTo(int sourceIndex, Archetype destination)
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
    /// Gets the typed component array for the specified type.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ComponentArray<T> GetComponentArray<T>() where T : struct, IComponent
    {
        return (ComponentArray<T>)componentArrays[typeof(T)];
    }

    /// <summary>
    /// Gets all component arrays in this archetype.
    /// </summary>
    internal IReadOnlyDictionary<Type, IComponentArray> GetAllComponentArrays()
    {
        return componentArrays;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var array in componentArrays.Values)
        {
            if (array is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        componentArrays.Clear();
        entities.Clear();
        entityIdToIndex.Clear();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"Archetype[{Id}] ({Count} entities)";
    }
}
