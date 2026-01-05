namespace KeenEyes;

public sealed partial class World
{
    #region Entity Management

    /// <summary>
    /// Begins building a new entity.
    /// </summary>
    /// <returns>A fluent builder for adding components.</returns>
    /// <remarks>
    /// <para>
    /// This method uses a thread-local <see cref="EntityBuilder"/> instance for both
    /// thread safety and allocation efficiency. Each thread gets its own builder that
    /// is reused across multiple <c>Spawn()</c> calls, avoiding per-call allocations.
    /// </para>
    /// </remarks>
    IEntityBuilder IWorld.Spawn()
    {
        var builder = threadLocalBuilder.Value!;
        builder.Reset();
        return builder;
    }

    /// <summary>
    /// Begins building a new entity with an optional name.
    /// </summary>
    /// <param name="name">The optional name for the entity.</param>
    /// <returns>A fluent builder for adding components.</returns>
    /// <remarks>
    /// <para>
    /// This method uses a thread-local <see cref="EntityBuilder"/> instance for both
    /// thread safety and allocation efficiency. Each thread gets its own builder that
    /// is reused across multiple <c>Spawn()</c> calls, avoiding per-call allocations.
    /// </para>
    /// </remarks>
    IEntityBuilder IWorld.Spawn(string? name)
    {
        var builder = threadLocalBuilder.Value!;
        builder.Reset(name);
        return builder;
    }

    /// <summary>
    /// Begins building a new entity.
    /// </summary>
    /// <returns>A fluent builder for adding components.</returns>
    /// <remarks>
    /// <para>
    /// This method uses a thread-local <see cref="EntityBuilder"/> instance for both
    /// thread safety and allocation efficiency. Each thread gets its own builder that
    /// is reused across multiple <c>Spawn()</c> calls, avoiding per-call allocations.
    /// </para>
    /// </remarks>
    public EntityBuilder Spawn()
    {
        var builder = threadLocalBuilder.Value!;
        builder.Reset();
        return builder;
    }

    /// <summary>
    /// Begins building a new entity with an optional name.
    /// </summary>
    /// <param name="name">
    /// The optional name for the entity. If provided, must be unique within this world.
    /// Can be <c>null</c> for unnamed entities.
    /// </param>
    /// <returns>A fluent builder for adding components.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when a non-null name is already assigned to another entity in this world.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Named entities can be retrieved later using <see cref="GetEntityByName(string)"/>.
    /// This is useful for debugging, editor tooling, and scenarios where entities need
    /// human-readable identifiers.
    /// </para>
    /// <para>
    /// Names must be unique within a world. Attempting to create an entity with a name
    /// that is already in use will throw an <see cref="ArgumentException"/>.
    /// </para>
    /// <para>
    /// This method uses a thread-local <see cref="EntityBuilder"/> instance for both
    /// thread safety and allocation efficiency. Each thread gets its own builder that
    /// is reused across multiple <c>Spawn()</c> calls, avoiding per-call allocations.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var player = world.Spawn("Player")
    ///     .With(new Position { X = 0, Y = 0 })
    ///     .With(new Health { Current = 100, Max = 100 })
    ///     .Build();
    ///
    /// // Later, retrieve by name
    /// var foundPlayer = world.GetEntityByName("Player");
    /// </code>
    /// </example>
    /// <seealso cref="GetName(Entity)"/>
    /// <seealso cref="GetEntityByName(string)"/>
    public EntityBuilder Spawn(string? name)
    {
        var builder = threadLocalBuilder.Value!;
        builder.Reset(name);
        return builder;
    }

    /// <summary>
    /// Ensures the event dispatcher is set up for a component type.
    /// Uses lazy initialization - dispatcher is created once per type on first use.
    /// No reflection needed - setup function stored during component registration.
    /// </summary>
    private void EnsureEventDispatcher(ComponentInfo info)
    {
        if (info.FireAddedBoxed != null)
        {
            return; // Already set up
        }

        // Call the setup function stored during registration (no reflection)
        info.SetupDispatcher?.Invoke(info, eventManager.ComponentEvents);
    }

    /// <summary>
    /// Creates an entity directly with the specified components and optional name.
    /// </summary>
    internal Entity CreateEntity(List<(ComponentInfo Info, object Data)> components, string? name = null)
    {
        // Validate name uniqueness before creating the entity
        entityNamingManager.ValidateName(name);

        // Validate component constraints (dependencies and conflicts) before creating entity
        validationManager.ValidateBuild(components);

        // Acquire entity from pool
        var entity = entityPool.Acquire();

        // Add to archetype
        archetypeManager.AddEntity(entity, components);

        // Register the entity name if provided
        entityNamingManager.RegisterName(entity.Id, name);

        // Run custom validators after entity is created (they need access to entity)
        validationManager.ValidateBuildCustom(entity, components);

        // Fire component added events for each component
        foreach (var (info, data) in components)
        {
            // Ensure dispatcher is set up (lazy initialization, once per component type)
            EnsureEventDispatcher(info);

            // Call the type-specific dispatcher delegate (no reflection, minimal overhead)
            info.FireAddedBoxed?.Invoke(eventManager.ComponentEvents, entity, data);
        }

        // Fire entity created event after entity is fully set up
        eventManager.FireEntityCreated(entity, name);

        return entity;
    }

    /// <summary>
    /// Destroys an entity and all its components.
    /// </summary>
    /// <param name="entity">The entity to destroy.</param>
    /// <returns>True if the entity was destroyed, false if it didn't exist.</returns>
    /// <remarks>
    /// <para>
    /// When an entity with children is despawned, its children become orphaned (their parent
    /// is set to <see cref="Entity.Null"/>). If you want to destroy an entity and all its
    /// descendants, use <see cref="DespawnRecursive(Entity)"/> instead.
    /// </para>
    /// <para>
    /// When an entity with a parent is despawned, it is automatically removed from its
    /// parent's children collection.
    /// </para>
    /// </remarks>
    /// <seealso cref="DespawnRecursive(Entity)"/>
    public bool Despawn(Entity entity)
    {
        if (!IsAlive(entity))
        {
            return false;
        }

        // Fire entity destroyed event before cleanup (entity still accessible)
        eventManager.FireEntityDestroyed(entity);

        // Clean up hierarchy relationships
        hierarchyManager.CleanupEntity(entity);

        // Remove from change tracking
        changeTracker.RemoveEntity(entity.Id);

        // Remove from archetype
        archetypeManager.RemoveEntity(entity);

        // Release entity to pool (increments version)
        entityPool.Release(entity);

        // Clean up entity name mappings
        entityNamingManager.UnregisterName(entity.Id);

        // Clean up string tags
        tagManager.RemoveAllTags(entity.Id);

        return true;
    }

    /// <summary>
    /// Checks if an entity is still alive.
    /// </summary>
    public bool IsAlive(Entity entity)
    {
        return entityPool.IsValid(entity) && archetypeManager.IsTracked(entity);
    }

    /// <summary>
    /// Gets a component from an entity by reference, allowing direct modification.
    /// </summary>
    /// <typeparam name="T">The component type to retrieve.</typeparam>
    /// <param name="entity">The entity to get the component from.</param>
    /// <returns>A reference to the component data for zero-copy access.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the entity is not alive, the component type is not registered,
    /// or the entity does not have the specified component.
    /// </exception>
    /// <example>
    /// <code>
    /// ref var position = ref world.Get&lt;Position&gt;(entity);
    /// position.X += 10; // Modifies the component directly
    /// </code>
    /// </example>
    public ref T Get<T>(Entity entity) where T : struct, IComponent
    {
        if (!IsAlive(entity))
        {
            throw new InvalidOperationException($"Entity {entity} is not alive.");
        }

        var info = Components.Get<T>();
        if (info is null)
        {
            throw new InvalidOperationException(
                $"Component type {typeof(T).Name} is not registered in this world.");
        }

        if (!archetypeManager.Has<T>(entity))
        {
            throw new InvalidOperationException(
                $"Entity {entity} does not have component {typeof(T).Name}.");
        }

        return ref archetypeManager.Get<T>(entity);
    }

    /// <summary>
    /// Gets a readonly reference to a component from an entity.
    /// </summary>
    /// <typeparam name="T">The component type to retrieve.</typeparam>
    /// <param name="entity">The entity to get the component from.</param>
    /// <returns>A readonly reference to the component data.</returns>
    /// <remarks>
    /// <para>
    /// Use this method when you only need to read component data without modification.
    /// The readonly reference prevents accidental mutation and enables compiler optimizations.
    /// </para>
    /// <para>
    /// For modifying components, use <see cref="Get{T}(Entity)"/> instead.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the entity is not alive, the component type is not registered,
    /// or the entity does not have the specified component.
    /// </exception>
    /// <example>
    /// <code>
    /// ref readonly var position = ref world.GetReadonly&lt;Position&gt;(entity);
    /// float x = position.X; // Read-only access
    /// </code>
    /// </example>
    public ref readonly T GetReadonly<T>(Entity entity) where T : struct, IComponent
    {
        if (!IsAlive(entity))
        {
            throw new InvalidOperationException($"Entity {entity} is not alive.");
        }

        var info = Components.Get<T>();
        if (info is null)
        {
            throw new InvalidOperationException(
                $"Component type {typeof(T).Name} is not registered in this world.");
        }

        if (!archetypeManager.Has<T>(entity))
        {
            throw new InvalidOperationException(
                $"Entity {entity} does not have component {typeof(T).Name}.");
        }

        return ref archetypeManager.GetReadonly<T>(entity);
    }

    /// <summary>
    /// Checks if an entity has a specific component type.
    /// </summary>
    /// <typeparam name="T">The component type to check for.</typeparam>
    /// <param name="entity">The entity to check.</param>
    /// <returns>
    /// <c>true</c> if the entity is alive and has the specified component;
    /// <c>false</c> if the entity is not alive, the component type is not registered,
    /// or the entity does not have the component.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is safe to call with stale entity handles. A stale handle refers to
    /// an entity that has been destroyed (via <see cref="Despawn"/>). In such cases,
    /// this method returns <c>false</c> rather than throwing an exception.
    /// </para>
    /// <para>
    /// This operation is O(1) for the archetype-based storage implementation.
    /// </para>
    /// <para>
    /// Use this method to conditionally check for components before calling
    /// <see cref="Get{T}(Entity)"/> to avoid exceptions, or use the guard clause
    /// pattern to skip entities that lack required components.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Conditional component access
    /// if (world.Has&lt;Velocity&gt;(entity))
    /// {
    ///     ref var velocity = ref world.Get&lt;Velocity&gt;(entity);
    ///     // Process velocity...
    /// }
    ///
    /// // Guard clause pattern
    /// if (!world.Has&lt;Health&gt;(entity))
    /// {
    ///     return; // Skip entities without health
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="Get{T}(Entity)"/>
    /// <seealso cref="Add{T}(Entity, in T)"/>
    /// <seealso cref="Remove{T}(Entity)"/>
    public bool Has<T>(Entity entity) where T : struct, IComponent
    {
        // Check entity validity first - stale handles should return false
        if (!IsAlive(entity))
        {
            return false;
        }

        var info = Components.Get<T>();
        if (info is null)
        {
            return false;
        }

        return archetypeManager.Has<T>(entity);
    }

    /// <summary>
    /// Checks if an entity has a component of the specified runtime type.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <param name="componentType">The component type to check for.</param>
    /// <returns><c>true</c> if the entity has the component; <c>false</c> otherwise.</returns>
    /// <remarks>
    /// <para>
    /// This method is useful when the component type is only known at runtime,
    /// such as during validation or reflection-based operations.
    /// </para>
    /// <para>
    /// If the entity is no longer alive (stale handle) or the component type
    /// is not registered, this method returns <c>false</c>.
    /// </para>
    /// </remarks>
    public bool HasComponent(Entity entity, Type componentType)
    {
        ArgumentNullException.ThrowIfNull(componentType);

        if (!IsAlive(entity))
        {
            return false;
        }

        var info = Components.Get(componentType);
        if (info is null)
        {
            return false;
        }

        return archetypeManager.Has(entity, componentType);
    }

    /// <summary>
    /// Adds a component to an existing entity at runtime.
    /// </summary>
    /// <typeparam name="T">The component type to add.</typeparam>
    /// <param name="entity">The entity to add the component to.</param>
    /// <param name="component">The component value to add.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the entity is not alive, or when the entity already has a component
    /// of the specified type. Use <see cref="Set{T}(Entity, in T)"/> to update existing components.
    /// </exception>
    /// <remarks>
    /// <para>
    /// After adding a component, the entity will be matched by queries that require that component type.
    /// This operation migrates the entity to a new archetype, which is O(C) where C is the number
    /// of components on the entity.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create an entity with just Position
    /// var entity = world.Spawn().With(new Position { X = 0, Y = 0 }).Build();
    ///
    /// // Later, add Velocity at runtime
    /// world.Add(entity, new Velocity { X = 1, Y = 0 });
    ///
    /// // Now entity is matched by queries requiring both Position and Velocity
    /// </code>
    /// </example>
    public void Add<T>(Entity entity, in T component) where T : struct, IComponent
    {
        if (!IsAlive(entity))
        {
            throw new InvalidOperationException($"Entity {entity} is not alive.");
        }

        // Validate component constraints (dependencies and conflicts)
        validationManager.ValidateAdd(entity, in component);

        // Register the component type if not already registered
        Components.GetOrRegister<T>();

        archetypeManager.AddComponent(entity, in component);

        // Fire component added event after successful addition
        eventManager.FireComponentAdded(entity, in component);
    }

    /// <summary>
    /// Sets (replaces) a component value on an entity that already has this component.
    /// </summary>
    /// <typeparam name="T">The component type to update.</typeparam>
    /// <param name="entity">The entity to update the component on.</param>
    /// <param name="value">The new component value.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the entity is not alive, the component type is not registered,
    /// or the entity does not have the specified component. Use <see cref="EntityBuilder.With{T}(T)"/>
    /// to add new components during entity creation.
    /// </exception>
    /// <example>
    /// <code>
    /// world.Set(entity, new Position { X = 100, Y = 200 });
    /// </code>
    /// </example>
    public void Set<T>(Entity entity, in T value) where T : struct, IComponent
    {
        if (!IsAlive(entity))
        {
            throw new InvalidOperationException($"Entity {entity} is not alive.");
        }

        var info = Components.Get<T>();
        if (info is null)
        {
            throw new InvalidOperationException(
                $"Component type {typeof(T).Name} is not registered in this world.");
        }

        // Check if entity has the component before accessing it
        if (!archetypeManager.Has<T>(entity))
        {
            throw new InvalidOperationException(
                $"Entity {entity} does not have component {typeof(T).Name}. " +
                $"Use Add<T>() to add it first.");
        }

        // Capture old value before setting new value (for change event)
        var oldValue = archetypeManager.Get<T>(entity);

        archetypeManager.Set(entity, in value);

        // Fire component changed event after successful update
        eventManager.FireComponentChanged(entity, in oldValue, in value);

        // Auto-track dirty if enabled for this component type
        if (changeTracker.IsAutoTrackingEnabled<T>())
        {
            changeTracker.MarkDirtyInternal<T>(entity);
        }
    }

    /// <summary>
    /// Removes a component from an entity.
    /// </summary>
    /// <typeparam name="T">The component type to remove.</typeparam>
    /// <param name="entity">The entity to remove the component from.</param>
    /// <returns>
    /// <c>true</c> if the component was removed; <c>false</c> if the entity is not alive,
    /// the component type is not registered, or the entity does not have the component.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This operation is idempotent: calling it multiple times with the same arguments
    /// will return <c>false</c> after the first successful removal.
    /// </para>
    /// <para>
    /// After removing a component, the entity will no longer be matched by queries that
    /// require that component type. This operation migrates the entity to a new archetype.
    /// </para>
    /// <para>
    /// <strong>Warning:</strong> Removing components from entities during query iteration
    /// may cause unexpected behavior. Consider using a command buffer pattern where removals
    /// are queued and applied after iteration completes.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Remove a component from an entity
    /// bool removed = world.Remove&lt;Velocity&gt;(entity);
    /// if (removed)
    /// {
    ///     Console.WriteLine("Velocity component removed");
    /// }
    ///
    /// // Idempotent: second removal returns false
    /// bool removedAgain = world.Remove&lt;Velocity&gt;(entity);
    /// Debug.Assert(removedAgain == false);
    /// </code>
    /// </example>
    public bool Remove<T>(Entity entity) where T : struct, IComponent
    {
        // Check if entity is alive
        if (!IsAlive(entity))
        {
            return false;
        }

        // Check if component type is registered
        var info = Components.Get<T>();
        if (info is null)
        {
            return false;
        }

        // Check if entity has the component before attempting removal
        if (!archetypeManager.Has<T>(entity))
        {
            return false;
        }

        // Fire component removed event before removal
        eventManager.FireComponentRemoved<T>(entity);

        return archetypeManager.RemoveComponent<T>(entity);
    }

    /// <summary>
    /// Gets all entities currently alive in this world.
    /// </summary>
    public IEnumerable<Entity> GetAllEntities()
    {
        foreach (var archetype in archetypeManager.Archetypes)
        {
            foreach (var entity in archetype.Entities)
            {
                yield return entity;
            }
        }
    }

    /// <summary>
    /// Clears all entities, components, singletons, and hierarchy data from this world.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method resets the world to an empty state while keeping registered systems,
    /// plugins, and event subscriptions intact. It is primarily used for restoring from
    /// snapshots where the world state needs to be cleared before recreation.
    /// </para>
    /// <para>
    /// After clearing, entity IDs start fresh from 0. Any existing entity handles
    /// become stale and will return false for <see cref="IsAlive(Entity)"/>.
    /// </para>
    /// <para>
    /// Note that this method does NOT clear:
    /// <list type="bullet">
    /// <item><description>Registered systems</description></item>
    /// <item><description>Installed plugins</description></item>
    /// <item><description>Event subscriptions</description></item>
    /// <item><description>Extensions</description></item>
    /// <item><description>The component registry</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Clear world before restoring from snapshot
    /// world.Clear();
    ///
    /// // Recreate entities from saved data
    /// foreach (var savedEntity in snapshot.Entities)
    /// {
    ///     world.Spawn(savedEntity.Name)
    ///         .With(savedEntity.Position)
    ///         .Build();
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="KeenEyes.Serialization.SnapshotManager"/>
    public void Clear()
    {
        // Clear all entity-related state
        archetypeManager.Clear();
        entityPool.Clear();
        entityNamingManager.Clear();
        hierarchyManager.Clear();
        singletonManager.Clear();
        changeTracker.Clear();
        messageManager.Clear();

        // Invalidate query cache since all entities are gone
        queryManager.InvalidateCache();
    }

    /// <summary>
    /// Gets the name assigned to an entity, if any.
    /// </summary>
    /// <param name="entity">The entity to get the name for.</param>
    /// <returns>
    /// The name assigned to the entity, or <c>null</c> if the entity has no name
    /// or is not alive.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is safe to call with stale entity handles. A stale handle refers to
    /// an entity that has been destroyed (via <see cref="Despawn"/>). In such cases,
    /// this method returns <c>null</c> rather than throwing an exception.
    /// </para>
    /// <para>
    /// This operation is O(1) for the dictionary-based storage implementation.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var player = world.Spawn("Player")
    ///     .With(new Position { X = 0, Y = 0 })
    ///     .Build();
    ///
    /// var name = world.GetName(player); // Returns "Player"
    ///
    /// var unnamed = world.Spawn().Build();
    /// var noName = world.GetName(unnamed); // Returns null
    /// </code>
    /// </example>
    /// <seealso cref="Spawn(string?)"/>
    /// <seealso cref="GetEntityByName(string)"/>
    public string? GetName(Entity entity)
    {
        // Return null for stale/invalid entities (safe pattern like Has<T>)
        if (!IsAlive(entity))
        {
            return null;
        }

        return entityNamingManager.GetName(entity.Id);
    }

    /// <summary>
    /// Changes the name of an entity.
    /// </summary>
    /// <param name="entity">The entity to rename.</param>
    /// <param name="newName">The new name, or null to remove the name.</param>
    /// <exception cref="InvalidOperationException">Thrown when the entity is not alive.</exception>
    /// <exception cref="ArgumentException">Thrown when the new name is already in use.</exception>
    /// <remarks>
    /// This method is primarily used by the delta restoration system.
    /// </remarks>
    internal void SetName(Entity entity, string? newName)
    {
        if (!IsAlive(entity))
        {
            throw new InvalidOperationException($"Entity {entity} is not alive.");
        }

        entityNamingManager.SetName(entity.Id, newName);
    }

    /// <summary>
    /// Finds an entity by its assigned name.
    /// </summary>
    /// <param name="name">The name to search for.</param>
    /// <returns>
    /// The entity with the specified name, or <see cref="Entity.Null"/> if no entity
    /// with that name exists in this world.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This operation is O(1) for the dictionary-based storage implementation.
    /// </para>
    /// <para>
    /// Entity names are unique within a world. If you need multiple entities with
    /// similar identifiers, consider using a naming convention like "Enemy_01", "Enemy_02", etc.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create a named entity
    /// world.Spawn("Player")
    ///     .With(new Position { X = 0, Y = 0 })
    ///     .Build();
    ///
    /// // Later, find it by name
    /// var player = world.GetEntityByName("Player");
    /// if (player.IsValid)
    /// {
    ///     ref var pos = ref world.Get&lt;Position&gt;(player);
    ///     Console.WriteLine($"Player at ({pos.X}, {pos.Y})");
    /// }
    ///
    /// // Non-existent name returns Entity.Null
    /// var notFound = world.GetEntityByName("NonExistent");
    /// Debug.Assert(!notFound.IsValid);
    /// </code>
    /// </example>
    /// <seealso cref="Spawn(string?)"/>
    /// <seealso cref="GetName(Entity)"/>
    public Entity GetEntityByName(string name)
    {
        if (!entityNamingManager.TryGetEntityIdByName(name, out var entityId))
        {
            return Entity.Null;
        }

        // Get the current version from the pool
        var version = entityPool.GetVersion(entityId);
        if (version < 0)
        {
            return Entity.Null;
        }

        var entity = new Entity(entityId, version);

        // Verify the entity is still alive
        if (!IsAlive(entity))
        {
            return Entity.Null;
        }

        return entity;
    }

    /// <summary>
    /// Retrieves all components attached to the specified entity for debugging and introspection.
    /// </summary>
    /// <param name="entity">The entity to get components from.</param>
    /// <returns>
    /// An enumerable of tuples containing the component type and its boxed value.
    /// Returns an empty sequence if the entity is not alive or has no components.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <strong>Boxing overhead:</strong> Component values are returned as boxed objects, which
    /// incurs allocation costs. This method is intended for debugging, editor integration, and
    /// serialization scenarios where performance is not critical. For performance-sensitive code,
    /// use <see cref="Get{T}(Entity)"/> or <see cref="Has{T}(Entity)"/> instead.
    /// </para>
    /// <para>
    /// This method is safe to call with stale entity handles. A stale handle refers to
    /// an entity that has been destroyed (via <see cref="Despawn"/>). In such cases,
    /// this method returns an empty sequence rather than throwing an exception.
    /// </para>
    /// <para>
    /// The complexity is O(C) where C is the number of component types on the entity.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Debug: Print all components on an entity
    /// foreach (var (type, value) in world.GetComponents(entity))
    /// {
    ///     Console.WriteLine($"{type.Name}: {value}");
    /// }
    ///
    /// // Serialization: Create a snapshot of entity state
    /// var snapshot = world.GetComponents(entity)
    ///     .ToDictionary(c => c.Type.Name, c => c.Value);
    /// </code>
    /// </example>
    /// <seealso cref="Get{T}(Entity)"/>
    /// <seealso cref="Has{T}(Entity)"/>
    public IEnumerable<(Type Type, object Value)> GetComponents(Entity entity)
    {
        // Return empty for stale/invalid entities (safe pattern like Has<T>)
        if (!IsAlive(entity))
        {
            yield break;
        }

        foreach (var component in archetypeManager.GetComponents(entity))
        {
            yield return component;
        }
    }

    /// <summary>
    /// Gets all entities matching a query description.
    /// </summary>
    internal IEnumerable<Entity> GetMatchingEntities(QueryDescription description)
    {
        var matchingArchetypes = queryManager.GetMatchingArchetypes(description);

        foreach (var archetype in matchingArchetypes)
        {
            foreach (var entity in archetype.Entities)
            {
                yield return entity;
            }
        }
    }

    /// <summary>
    /// Gets the matching archetypes for a query description.
    /// Uses cached results when available.
    /// </summary>
    internal IReadOnlyList<Archetype> GetMatchingArchetypes(QueryDescription description)
    {
        return queryManager.GetMatchingArchetypes(description);
    }

    /// <summary>
    /// Removes a component from an entity by type.
    /// </summary>
    /// <param name="entity">The entity to remove the component from.</param>
    /// <param name="componentType">The type of component to remove.</param>
    /// <returns>True if the component was removed, false if the entity didn't have it.</returns>
    /// <remarks>
    /// This method is primarily used by the delta restoration system for AOT-compatible
    /// component removal. For typed component removal, prefer <see cref="Remove{T}"/>.
    /// </remarks>
    internal bool RemoveComponent(Entity entity, Type componentType)
    {
        if (!IsAlive(entity))
        {
            return false;
        }

        if (!archetypeManager.Has(entity, componentType))
        {
            return false;
        }

        return archetypeManager.RemoveComponent(entity, componentType);
    }

    /// <summary>
    /// Sets a component value on an entity using component info and a boxed value.
    /// </summary>
    /// <param name="entity">The entity to set the component on.</param>
    /// <param name="info">The component info.</param>
    /// <param name="value">The boxed component value.</param>
    /// <remarks>
    /// This method is primarily used by the delta restoration system for AOT-compatible
    /// component setting. For typed component setting, prefer <see cref="Set{T}"/>.
    /// When adding a new component, archetype migration occurs but the entity ID is preserved.
    /// </remarks>
    internal void SetComponent(Entity entity, ComponentInfo info, object value)
    {
        if (!IsAlive(entity))
        {
            throw new InvalidOperationException($"Entity {entity} is not alive.");
        }

        if (archetypeManager.Has(entity, info.Type))
        {
            // Update existing component
            archetypeManager.SetBoxed(entity, info.Type, value);
        }
        else
        {
            // Add new component - proper archetype migration preserves entity ID
            archetypeManager.AddComponentBoxed(entity, info.Type, value);
        }
    }

    /// <summary>
    /// Sets or adds a component value on an entity using a boxed value and type.
    /// </summary>
    /// <param name="entity">The entity to set the component on.</param>
    /// <param name="componentType">The type of the component.</param>
    /// <param name="value">The boxed component value.</param>
    /// <remarks>
    /// <para>
    /// This method adds the component if the entity doesn't have it, or updates
    /// the existing value if it does. The component type is registered automatically
    /// if not already registered.
    /// </para>
    /// <para>
    /// This is primarily used for network replication where components are deserialized
    /// as boxed values. For typed component operations, prefer <see cref="Add{T}"/> and
    /// <see cref="Set{T}"/>.
    /// </para>
    /// </remarks>
    public void SetComponent(Entity entity, Type componentType, object value)
    {
        if (!IsAlive(entity))
        {
            throw new InvalidOperationException($"Entity {entity} is not alive.");
        }

        // For network replication, component types should already be registered
        // via the generated serializer initialization
        if (Components.Get(componentType) is null)
        {
            throw new InvalidOperationException(
                $"Component type {componentType.Name} is not registered. " +
                "Ensure the component is registered before network replication.");
        }

        if (archetypeManager.Has(entity, componentType))
        {
            // Update existing component
            archetypeManager.SetBoxed(entity, componentType, value);
        }
        else
        {
            // Add new component - proper archetype migration preserves entity ID
            archetypeManager.AddComponentBoxed(entity, componentType, value);
        }
    }

    #endregion
}
