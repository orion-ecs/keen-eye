namespace KeenEyes;

/// <summary>
/// Manages archetype lifecycle and entity-to-archetype mappings.
/// Handles archetype creation, entity migration, and query matching.
/// </summary>
/// <remarks>
/// <para>
/// The ArchetypeManager is the central coordinator for the archetype-based storage system.
/// It maintains a mapping from ArchetypeId to Archetype instances and tracks which archetype
/// each entity belongs to.
/// </para>
/// <para>
/// When an entity's component set changes, the ArchetypeManager handles the migration
/// to a new archetype, copying shared components and updating all bookkeeping.
/// </para>
/// <para>
/// This class is thread-safe: entity operations (add, remove, migrate) can be called
/// concurrently from multiple threads. All mutations are serialized using internal locking.
/// </para>
/// </remarks>
/// <param name="componentRegistry">The component registry for type information.</param>
/// <param name="chunkPool">Optional chunk pool for chunk reuse. If null, a new pool is created.</param>
public sealed class ArchetypeManager(ComponentRegistry componentRegistry, ChunkPool? chunkPool = null) : IDisposable
{
    private readonly Lock syncRoot = new();
    private readonly Dictionary<ArchetypeId, Archetype> archetypes = [];
    private readonly Dictionary<int, (Archetype Archetype, int Index)> entityLocations = [];
    private readonly List<Archetype> archetypeList = [];
    private readonly ChunkPool chunkPool = chunkPool ?? new ChunkPool();

    /// <summary>
    /// Event raised when a new archetype is created.
    /// Used for cache invalidation in the query system.
    /// </summary>
    public event Action<Archetype>? ArchetypeCreated;

    /// <summary>
    /// Gets all archetypes in this manager.
    /// </summary>
    public IReadOnlyList<Archetype> Archetypes => archetypeList;

    /// <summary>
    /// Gets the number of archetypes.
    /// </summary>
    public int ArchetypeCount
    {
        get
        {
            lock (syncRoot)
            {
                return archetypeList.Count;
            }
        }
    }

    /// <summary>
    /// Gets the number of entities tracked by this manager.
    /// </summary>
    public int EntityCount
    {
        get
        {
            lock (syncRoot)
            {
                return entityLocations.Count;
            }
        }
    }

    /// <summary>
    /// Gets the chunk pool used by this manager.
    /// </summary>
    public ChunkPool ChunkPool => chunkPool;

    /// <summary>
    /// Gets or creates an archetype for the specified component types.
    /// </summary>
    /// <param name="componentTypes">The component types.</param>
    /// <returns>The archetype for those component types.</returns>
    public Archetype GetOrCreateArchetype(IEnumerable<Type> componentTypes)
    {
        var id = new ArchetypeId(componentTypes);
        return GetOrCreateArchetype(id);
    }

    /// <summary>
    /// Pre-allocates an archetype for the specified component types with an initial capacity.
    /// </summary>
    /// <param name="componentTypes">The component types for this archetype.</param>
    /// <param name="initialCapacity">Initial capacity for entity storage. Defaults to 16.</param>
    /// <returns>The pre-allocated archetype.</returns>
    /// <remarks>
    /// <para>
    /// Pre-allocating archetypes is useful for reducing the number of archetype transitions
    /// when spawning many entities with the same component set. This is particularly effective
    /// when used with bundles.
    /// </para>
    /// <para>
    /// If an archetype with these component types already exists, this method returns the
    /// existing archetype without creating a new one.
    /// </para>
    /// </remarks>
    public Archetype PreallocateArchetype(IEnumerable<Type> componentTypes, int initialCapacity = 16)
    {
        return GetOrCreateArchetype(componentTypes);
    }

    /// <summary>
    /// Gets or creates an archetype for the specified archetype ID.
    /// </summary>
    /// <param name="id">The archetype identifier.</param>
    /// <returns>The archetype for that ID.</returns>
    public Archetype GetOrCreateArchetype(ArchetypeId id)
    {
        lock (syncRoot)
        {
            if (archetypes.TryGetValue(id, out var existing))
            {
                return existing;
            }

            // Get component info for each type
            var componentInfos = new List<ComponentInfo>();
            foreach (var type in id.ComponentTypes)
            {
                var info = componentRegistry.Get(type);
                if (info is null)
                {
                    throw new InvalidOperationException(
                        $"Component type {type.Name} is not registered. Register it before creating archetypes.");
                }
                componentInfos.Add(info);
            }

            var archetype = new Archetype(id, componentInfos, chunkPool);
            archetypes[id] = archetype;
            archetypeList.Add(archetype);

            // Notify listeners (for query cache invalidation)
            ArchetypeCreated?.Invoke(archetype);

            return archetype;
        }
    }

    /// <summary>
    /// Gets an archetype by its ID, or null if it doesn't exist.
    /// </summary>
    /// <param name="id">The archetype identifier.</param>
    /// <returns>The archetype, or null.</returns>
    public Archetype? GetArchetype(ArchetypeId id)
    {
        lock (syncRoot)
        {
            return archetypes.TryGetValue(id, out var archetype) ? archetype : null;
        }
    }

    /// <summary>
    /// Adds an entity to the appropriate archetype.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="components">The components and their values.</param>
    internal void AddEntity(Entity entity, IEnumerable<(ComponentInfo Info, object Data)> components)
    {
        // Materialize components list outside lock to avoid holding lock during enumeration
        var componentList = components.ToList();

        lock (syncRoot)
        {
            var types = componentList.Select(c => c.Info.Type);
            var archetype = GetOrCreateArchetypeNoLock(types);

            // Add entity to archetype
            var index = archetype.AddEntity(entity);

            // Add all component values
            foreach (var (info, data) in componentList)
            {
                archetype.AddComponentBoxed(info.Type, data);
            }

            // Track entity location
            entityLocations[entity.Id] = (archetype, index);
        }
    }

    /// <summary>
    /// Removes an entity from its archetype.
    /// </summary>
    /// <param name="entity">The entity to remove.</param>
    /// <returns>True if the entity was removed.</returns>
    internal bool RemoveEntity(Entity entity)
    {
        lock (syncRoot)
        {
            if (!entityLocations.TryGetValue(entity.Id, out var location))
            {
                return false;
            }

            var (archetype, _) = location;

            // Remove from archetype (swap-back)
            var swappedEntity = archetype.RemoveEntity(entity);

            // Update swapped entity's index if one was swapped
            if (swappedEntity.HasValue)
            {
                var swappedIndex = archetype.GetEntityIndex(swappedEntity.Value);
                entityLocations[swappedEntity.Value.Id] = (archetype, swappedIndex);
            }

            entityLocations.Remove(entity.Id);
            return true;
        }
    }

    /// <summary>
    /// Adds a component to an entity, migrating it to a new archetype.
    /// </summary>
    /// <typeparam name="T">The component type to add.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <param name="component">The component value.</param>
    internal void AddComponent<T>(Entity entity, in T component) where T : struct, IComponent
    {
        lock (syncRoot)
        {
            if (!entityLocations.TryGetValue(entity.Id, out var location))
            {
                throw new InvalidOperationException($"Entity {entity} is not tracked by this archetype manager.");
            }

            var (currentArchetype, currentIndex) = location;

            // Check if already has the component
            if (currentArchetype.Has<T>())
            {
                throw new InvalidOperationException(
                    $"Entity {entity} already has component {typeof(T).Name}. Use Set<T>() to update.");
            }

            // Get or create target archetype
            var newId = currentArchetype.Id.With<T>();
            var newArchetype = GetOrCreateArchetypeNoLock(newId);

            // Migrate entity
            MigrateEntity(entity, currentArchetype, currentIndex, newArchetype);

            // Add the new component
            newArchetype.AddComponent(in component);
        }
    }

    /// <summary>
    /// Adds a boxed component to an entity, migrating it to a new archetype.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <param name="componentType">The component type.</param>
    /// <param name="value">The boxed component value.</param>
    internal void AddComponentBoxed(Entity entity, Type componentType, object value)
    {
        lock (syncRoot)
        {
            if (!entityLocations.TryGetValue(entity.Id, out var location))
            {
                throw new InvalidOperationException($"Entity {entity} is not tracked by this archetype manager.");
            }

            var (currentArchetype, currentIndex) = location;

            // Check if already has the component
            if (currentArchetype.Has(componentType))
            {
                throw new InvalidOperationException(
                    $"Entity {entity} already has component {componentType.Name}. Use SetBoxed() to update.");
            }

            // Get or create target archetype
            var newId = currentArchetype.Id.With(componentType);
            var newArchetype = GetOrCreateArchetypeNoLock(newId);

            // Migrate entity
            MigrateEntity(entity, currentArchetype, currentIndex, newArchetype);

            // Add the new component
            newArchetype.AddComponentBoxed(componentType, value);
        }
    }

    /// <summary>
    /// Removes a component from an entity, migrating it to a new archetype.
    /// </summary>
    /// <typeparam name="T">The component type to remove.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <returns>True if the component was removed.</returns>
    internal bool RemoveComponent<T>(Entity entity) where T : struct, IComponent
    {
        lock (syncRoot)
        {
            if (!entityLocations.TryGetValue(entity.Id, out var location))
            {
                return false;
            }

            var (currentArchetype, currentIndex) = location;

            // Get or create target archetype
            var newId = currentArchetype.Id.Without<T>();
            var newArchetype = GetOrCreateArchetypeNoLock(newId);

            // Migrate entity (copies shared components, excludes removed one)
            MigrateEntity(entity, currentArchetype, currentIndex, newArchetype);

            return true;
        }
    }

    /// <summary>
    /// Gets a component reference for an entity.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <returns>A reference to the component.</returns>
    /// <remarks>
    /// Note: The returned reference is only valid while the lock is held internally.
    /// Avoid storing the reference for later use in concurrent scenarios.
    /// </remarks>
    internal ref T Get<T>(Entity entity) where T : struct, IComponent
    {
        lock (syncRoot)
        {
            var (archetype, index) = GetEntityLocationNoLock(entity);
            return ref archetype.Get<T>(index);
        }
    }

    /// <summary>
    /// Checks if an entity has a component.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <returns>True if the entity has the component.</returns>
    internal bool Has<T>(Entity entity) where T : struct, IComponent
    {
        lock (syncRoot)
        {
            if (!entityLocations.TryGetValue(entity.Id, out var location))
            {
                return false;
            }
            return location.Archetype.Has<T>();
        }
    }

    /// <summary>
    /// Checks if an entity has a specific component type by runtime type.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <param name="componentType">The component type to check for.</param>
    /// <returns>True if the entity has the component.</returns>
    internal bool Has(Entity entity, Type componentType)
    {
        lock (syncRoot)
        {
            if (!entityLocations.TryGetValue(entity.Id, out var location))
            {
                return false;
            }
            return location.Archetype.Has(componentType);
        }
    }

    /// <summary>
    /// Sets a component value for an entity.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <param name="component">The new component value.</param>
    internal void Set<T>(Entity entity, in T component) where T : struct, IComponent
    {
        lock (syncRoot)
        {
            var (archetype, index) = GetEntityLocationNoLock(entity);
            archetype.Set(index, in component);
        }
    }

    /// <summary>
    /// Sets a component value for an entity using a boxed value.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <param name="componentType">The component type.</param>
    /// <param name="value">The boxed component value.</param>
    internal void SetBoxed(Entity entity, Type componentType, object value)
    {
        lock (syncRoot)
        {
            var (archetype, index) = GetEntityLocationNoLock(entity);
            archetype.SetBoxed(componentType, index, value);
        }
    }

    /// <summary>
    /// Removes a component from an entity by type, migrating it to a new archetype.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <param name="componentType">The component type to remove.</param>
    /// <returns>True if the component was removed.</returns>
    internal bool RemoveComponent(Entity entity, Type componentType)
    {
        lock (syncRoot)
        {
            if (!entityLocations.TryGetValue(entity.Id, out var location))
            {
                return false;
            }

            var (currentArchetype, currentIndex) = location;

            if (!currentArchetype.Has(componentType))
            {
                return false;
            }

            // Get or create target archetype without the component
            var newId = currentArchetype.Id.Without(componentType);
            var newArchetype = GetOrCreateArchetypeNoLock(newId);

            // Migrate entity (copies shared components, excludes removed one)
            MigrateEntity(entity, currentArchetype, currentIndex, newArchetype);

            return true;
        }
    }

    /// <summary>
    /// Gets the archetype and index for an entity.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns>The archetype and index tuple.</returns>
    internal (Archetype Archetype, int Index) GetEntityLocation(Entity entity)
    {
        lock (syncRoot)
        {
            return GetEntityLocationNoLock(entity);
        }
    }

    /// <summary>
    /// Tries to get the archetype and index for an entity.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <param name="archetype">The archetype containing the entity.</param>
    /// <param name="index">The index within the archetype.</param>
    /// <returns>True if the entity was found.</returns>
    internal bool TryGetEntityLocation(Entity entity, out Archetype? archetype, out int index)
    {
        lock (syncRoot)
        {
            if (entityLocations.TryGetValue(entity.Id, out var location))
            {
                archetype = location.Archetype;
                index = location.Index;
                return true;
            }

            archetype = null;
            index = -1;
            return false;
        }
    }

    /// <summary>
    /// Gets all archetypes that match the specified query.
    /// </summary>
    /// <param name="description">The query description.</param>
    /// <returns>Matching archetypes.</returns>
    public IEnumerable<Archetype> GetMatchingArchetypes(QueryDescription description)
    {
        // Take a snapshot of archetypes under lock to avoid enumeration issues
        Archetype[] snapshot;
        lock (syncRoot)
        {
            snapshot = [.. archetypeList];
        }

        foreach (var archetype in snapshot)
        {
            if (MatchesQuery(archetype, description))
            {
                yield return archetype;
            }
        }
    }

    /// <summary>
    /// Gets all component types for an entity.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns>The component types.</returns>
    internal IEnumerable<Type> GetComponentTypes(Entity entity)
    {
        lock (syncRoot)
        {
            if (!entityLocations.TryGetValue(entity.Id, out var location))
            {
                return [];
            }
            // Return a copy since ComponentTypes is immutable array
            return location.Archetype.ComponentTypes;
        }
    }

    /// <summary>
    /// Gets all components for an entity (boxed).
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns>The component types and values.</returns>
    internal IEnumerable<(Type Type, object Value)> GetComponents(Entity entity)
    {
        // Take a snapshot under lock
        List<(Type Type, object Value)> result;
        lock (syncRoot)
        {
            if (!entityLocations.TryGetValue(entity.Id, out var location))
            {
                return [];
            }

            var (archetype, index) = location;
            result = [];
            foreach (var type in archetype.ComponentTypes)
            {
                result.Add((type, archetype.GetBoxed(type, index)));
            }
        }
        return result;
    }

    /// <summary>
    /// Checks if an entity is tracked by this manager.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns>True if tracked.</returns>
    internal bool IsTracked(Entity entity)
    {
        lock (syncRoot)
        {
            return entityLocations.ContainsKey(entity.Id);
        }
    }

    // Called from within a lock - do not acquire lock here
    private void MigrateEntity(Entity entity, Archetype source, int sourceIndex, Archetype destination)
    {
        // Add entity to new archetype
        var newIndex = destination.AddEntity(entity);

        // Copy shared components
        source.CopySharedComponentsTo(sourceIndex, destination);

        // Remove from old archetype
        var swappedEntity = source.RemoveEntity(entity);

        // Update swapped entity's index
        if (swappedEntity.HasValue)
        {
            var swappedIndex = source.GetEntityIndex(swappedEntity.Value);
            entityLocations[swappedEntity.Value.Id] = (source, swappedIndex);
        }

        // Update entity location
        entityLocations[entity.Id] = (destination, newIndex);
    }

    private static bool MatchesQuery(Archetype archetype, QueryDescription description)
    {
        // Check all required components are present
        foreach (var required in description.AllRequired)
        {
            if (!archetype.Has(required))
            {
                return false;
            }
        }

        // Check no excluded components are present
        foreach (var excluded in description.Without)
        {
            if (archetype.Has(excluded))
            {
                return false;
            }
        }

        return true;
    }

    // Lock-free version for use within locked sections
    private Archetype GetOrCreateArchetypeNoLock(IEnumerable<Type> componentTypes)
    {
        var id = new ArchetypeId(componentTypes);
        return GetOrCreateArchetypeNoLock(id);
    }

    // Lock-free version for use within locked sections
    private Archetype GetOrCreateArchetypeNoLock(ArchetypeId id)
    {
        if (archetypes.TryGetValue(id, out var existing))
        {
            return existing;
        }

        // Get component info for each type
        var componentInfos = new List<ComponentInfo>();
        foreach (var type in id.ComponentTypes)
        {
            var info = componentRegistry.Get(type);
            if (info is null)
            {
                throw new InvalidOperationException(
                    $"Component type {type.Name} is not registered. Register it before creating archetypes.");
            }
            componentInfos.Add(info);
        }

        var archetype = new Archetype(id, componentInfos, chunkPool);
        archetypes[id] = archetype;
        archetypeList.Add(archetype);

        // Notify listeners (for query cache invalidation)
        ArchetypeCreated?.Invoke(archetype);

        return archetype;
    }

    // Lock-free version for use within locked sections
    private (Archetype Archetype, int Index) GetEntityLocationNoLock(Entity entity)
    {
        if (!entityLocations.TryGetValue(entity.Id, out var location))
        {
            throw new InvalidOperationException($"Entity {entity} is not tracked by this archetype manager.");
        }
        return location;
    }

    /// <summary>
    /// Clears all archetypes and entity locations, returning all chunks to the pool.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method resets the archetype manager to its initial state while keeping
    /// the chunk pool for reuse. This is useful for scenarios like restoring from
    /// a snapshot where the world state needs to be cleared before recreation.
    /// </para>
    /// <para>
    /// Unlike <see cref="Dispose"/>, this method leaves the manager in a usable state.
    /// </para>
    /// </remarks>
    public void Clear()
    {
        lock (syncRoot)
        {
            foreach (var archetype in archetypeList)
            {
                archetype.Dispose();
            }
            archetypes.Clear();
            archetypeList.Clear();
            entityLocations.Clear();
            // Keep the chunk pool - chunks will be returned to it by archetype.Dispose()
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (syncRoot)
        {
            foreach (var archetype in archetypeList)
            {
                archetype.Dispose();
            }
            archetypes.Clear();
            archetypeList.Clear();
            entityLocations.Clear();
        }
        chunkPool.Clear();
    }
}
