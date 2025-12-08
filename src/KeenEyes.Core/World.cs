using KeenEyes.Events;

namespace KeenEyes;

/// <summary>
/// The world is the container for all entities and their components.
/// Each world is completely isolated with its own component registry.
/// </summary>
/// <remarks>
/// <para>
/// World uses an archetype-based storage system for high-performance entity iteration.
/// Entities with the same component types are stored contiguously in memory,
/// enabling cache-friendly access patterns.
/// </para>
/// <para>
/// The world manages entity lifecycle, component storage, and system execution.
/// Use <see cref="Spawn()"/> to create entities and <see cref="Query{T1}"/> to
/// iterate over entities with specific components.
/// </para>
/// </remarks>
public sealed class World : IDisposable
{
    private readonly EntityPool entityPool;
    private readonly ArchetypeManager archetypeManager;
    private readonly QueryManager queryManager;
    private readonly HierarchyManager hierarchyManager;
    private readonly SystemManager systemManager;
    private readonly PluginManager pluginManager;
    private readonly SingletonManager singletonManager = new();
    private readonly EntityNamingManager entityNamingManager = new();
    private readonly EventManager eventManager = new();
    private readonly MessageManager messageManager = new();
    private readonly ChangeTracker changeTracker;
    private readonly ExtensionManager extensionManager = new();
    private readonly PrefabManager prefabManager;
    private readonly TagManager tagManager = new();
    private readonly ComponentValidationManager validationManager;
    private readonly EntityBuilder builder;

    /// <summary>
    /// The component registry for this world.
    /// Component IDs are unique per-world, not global.
    /// </summary>
    public ComponentRegistry Components { get; } = new();

    /// <summary>
    /// Gets the archetype manager for this world.
    /// Provides access to archetype storage, chunk pooling, and entity location tracking.
    /// </summary>
    public ArchetypeManager ArchetypeManager => archetypeManager;

    /// <summary>
    /// Gets the query manager for this world.
    /// </summary>
    internal QueryManager Queries => queryManager;

    /// <summary>
    /// Gets the entity pool for this world.
    /// </summary>
    internal EntityPool EntityPool => entityPool;

    /// <summary>
    /// Gets the event bus for publishing and subscribing to custom events.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The event bus provides a generic pub/sub mechanism for user-defined events.
    /// For built-in lifecycle events (entity creation/destruction, component changes),
    /// use the dedicated methods like <see cref="OnEntityCreated(Action{Entity, string?})"/>,
    /// <see cref="OnComponentAdded{T}(Action{Entity, T})"/>, etc.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Define a custom event
    /// public readonly record struct DamageEvent(Entity Target, int Amount);
    ///
    /// // Subscribe
    /// var sub = world.Events.Subscribe&lt;DamageEvent&gt;(e => Console.WriteLine($"Damage: {e.Amount}"));
    ///
    /// // Publish
    /// world.Events.Publish(new DamageEvent(entity, 50));
    /// </code>
    /// </example>
    public EventBus Events => eventManager.Bus;

    /// <summary>
    /// Creates a new ECS world.
    /// </summary>
    public World()
    {
        entityPool = new EntityPool();
        archetypeManager = new ArchetypeManager(Components);
        queryManager = new QueryManager(archetypeManager);
        hierarchyManager = new HierarchyManager(this);
        systemManager = new SystemManager(this);
        pluginManager = new PluginManager(this, systemManager);
        changeTracker = new ChangeTracker(entityPool);
        prefabManager = new PrefabManager(this);
        validationManager = new ComponentValidationManager(this);
        builder = new EntityBuilder(this);
    }

    #region Entity Management

    /// <summary>
    /// Begins building a new entity.
    /// </summary>
    /// <returns>A fluent builder for adding components.</returns>
    public EntityBuilder Spawn()
    {
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
        builder.Reset(name);
        return builder;
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

    #endregion

    #region String Tags

    /// <summary>
    /// Adds a string tag to an entity.
    /// </summary>
    /// <param name="entity">The entity to add the tag to.</param>
    /// <param name="tag">The tag to add. Cannot be null, empty, or whitespace.</param>
    /// <returns>
    /// <c>true</c> if the tag was added; <c>false</c> if the entity already had the tag.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when the entity is not alive.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tag"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tag"/> is empty or whitespace.</exception>
    /// <remarks>
    /// <para>
    /// String tags provide runtime-flexible tagging for scenarios like designer-driven
    /// content tagging, serialization, and editor tooling. Unlike type-safe tag components
    /// (<see cref="ITagComponent"/>), string tags can be assigned dynamically without
    /// compile-time type definitions.
    /// </para>
    /// <para>
    /// This operation is O(1).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var enemy = world.Spawn()
    ///     .With(new Position { X = 0, Y = 0 })
    ///     .Build();
    ///
    /// world.AddTag(enemy, "Enemy");
    /// world.AddTag(enemy, "Hostile");
    /// world.AddTag(enemy, "Boss");
    ///
    /// // Check if entity has tag
    /// if (world.HasTag(enemy, "Boss"))
    /// {
    ///     // Special boss handling
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="RemoveTag(Entity, string)"/>
    /// <seealso cref="HasTag(Entity, string)"/>
    /// <seealso cref="GetTags(Entity)"/>
    public bool AddTag(Entity entity, string tag)
    {
        if (!IsAlive(entity))
        {
            throw new InvalidOperationException($"Entity {entity} is not alive.");
        }

        return tagManager.AddTag(entity.Id, tag);
    }

    /// <summary>
    /// Removes a string tag from an entity.
    /// </summary>
    /// <param name="entity">The entity to remove the tag from.</param>
    /// <param name="tag">The tag to remove. Cannot be null, empty, or whitespace.</param>
    /// <returns>
    /// <c>true</c> if the tag was removed; <c>false</c> if the entity didn't have the tag.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when the entity is not alive.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tag"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tag"/> is empty or whitespace.</exception>
    /// <remarks>
    /// <para>
    /// This operation is O(1).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Remove a tag when enemy becomes friendly
    /// world.RemoveTag(entity, "Hostile");
    /// world.AddTag(entity, "Friendly");
    /// </code>
    /// </example>
    /// <seealso cref="AddTag(Entity, string)"/>
    /// <seealso cref="HasTag(Entity, string)"/>
    public bool RemoveTag(Entity entity, string tag)
    {
        if (!IsAlive(entity))
        {
            throw new InvalidOperationException($"Entity {entity} is not alive.");
        }

        return tagManager.RemoveTag(entity.Id, tag);
    }

    /// <summary>
    /// Checks if an entity has a specific string tag.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <param name="tag">The tag to check for. Cannot be null, empty, or whitespace.</param>
    /// <returns>
    /// <c>true</c> if the entity is alive and has the specified tag;
    /// <c>false</c> if the entity is not alive or doesn't have the tag.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tag"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tag"/> is empty or whitespace.</exception>
    /// <remarks>
    /// <para>
    /// This method is safe to call with stale entity handles. A stale handle refers to
    /// an entity that has been destroyed (via <see cref="Despawn"/>). In such cases,
    /// this method returns <c>false</c> rather than throwing an exception.
    /// </para>
    /// <para>
    /// This operation is O(1).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// foreach (var entity in world.Query&lt;Position&gt;())
    /// {
    ///     if (world.HasTag(entity, "Player"))
    ///     {
    ///         // Handle player entity
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="AddTag(Entity, string)"/>
    /// <seealso cref="GetTags(Entity)"/>
    public bool HasTag(Entity entity, string tag)
    {
        if (!IsAlive(entity))
        {
            return false;
        }

        return tagManager.HasTag(entity.Id, tag);
    }

    /// <summary>
    /// Gets all string tags on an entity.
    /// </summary>
    /// <param name="entity">The entity to get tags for.</param>
    /// <returns>
    /// A read-only collection of tags on the entity. Returns an empty collection
    /// if the entity is not alive or has no tags.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is safe to call with stale entity handles. A stale handle refers to
    /// an entity that has been destroyed (via <see cref="Despawn"/>). In such cases,
    /// this method returns an empty collection rather than throwing an exception.
    /// </para>
    /// <para>
    /// This operation is O(1) to obtain the collection.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var tags = world.GetTags(entity);
    /// Console.WriteLine($"Entity has {tags.Count} tags:");
    /// foreach (var tag in tags)
    /// {
    ///     Console.WriteLine($"  - {tag}");
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="AddTag(Entity, string)"/>
    /// <seealso cref="HasTag(Entity, string)"/>
    public IReadOnlyCollection<string> GetTags(Entity entity)
    {
        if (!IsAlive(entity))
        {
            return [];
        }

        return tagManager.GetTags(entity.Id);
    }

    /// <summary>
    /// Gets all entities that have a specific string tag.
    /// </summary>
    /// <param name="tag">The tag to query for. Cannot be null, empty, or whitespace.</param>
    /// <returns>
    /// An enumerable of entities that have the specified tag.
    /// Returns an empty sequence if no entities have the tag.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tag"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tag"/> is empty or whitespace.</exception>
    /// <remarks>
    /// <para>
    /// This method filters out any stale entity references, ensuring all returned
    /// entities are alive.
    /// </para>
    /// <para>
    /// This operation is O(N) where N is the number of entities with the tag.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Process all enemies
    /// foreach (var entity in world.QueryByTag("Enemy"))
    /// {
    ///     ref var pos = ref world.Get&lt;Position&gt;(entity);
    ///     // Update enemy position
    /// }
    ///
    /// // Count players
    /// var playerCount = world.QueryByTag("Player").Count();
    /// </code>
    /// </example>
    /// <seealso cref="AddTag(Entity, string)"/>
    /// <seealso cref="HasTag(Entity, string)"/>
    public IEnumerable<Entity> QueryByTag(string tag)
    {
        var entityIds = tagManager.GetEntitiesWithTag(tag);

        foreach (var entityId in entityIds)
        {
            var version = entityPool.GetVersion(entityId);
            if (version < 0)
            {
                continue;
            }

            var entity = new Entity(entityId, version);
            if (IsAlive(entity))
            {
                yield return entity;
            }
        }
    }

    #endregion

    #region Entity Hierarchy

    /// <summary>
    /// Sets the parent of an entity, establishing a parent-child relationship.
    /// </summary>
    /// <param name="child">The entity to become a child.</param>
    /// <param name="parent">
    /// The entity to become the parent. Pass <see cref="Entity.Null"/> to remove the parent
    /// (make the entity a root entity).
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the child entity is not alive, or when setting the parent would create
    /// a circular relationship (the parent is a descendant of the child).
    /// </exception>
    /// <remarks>
    /// <para>
    /// If the child already has a parent, it will be removed from the previous parent's
    /// children collection before being added to the new parent.
    /// </para>
    /// <para>
    /// This operation performs cycle detection to prevent circular hierarchies. A cycle
    /// occurs when setting an ancestor as a child's parent, which would create an infinite
    /// loop in the hierarchy.
    /// </para>
    /// <para>
    /// Parent lookup is O(1). Setting a parent is O(D) where D is the depth of the hierarchy
    /// due to cycle detection.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var parent = world.Spawn().Build();
    /// var child = world.Spawn().Build();
    ///
    /// // Establish parent-child relationship
    /// world.SetParent(child, parent);
    ///
    /// // Remove parent (make child a root entity)
    /// world.SetParent(child, Entity.Null);
    /// </code>
    /// </example>
    /// <seealso cref="GetParent(Entity)"/>
    /// <seealso cref="GetChildren(Entity)"/>
    /// <seealso cref="AddChild(Entity, Entity)"/>
    public void SetParent(Entity child, Entity parent)
        => hierarchyManager.SetParent(child, parent);

    /// <summary>
    /// Gets the parent of an entity.
    /// </summary>
    /// <param name="entity">The entity to get the parent for.</param>
    /// <returns>
    /// The parent entity, or <see cref="Entity.Null"/> if the entity has no parent
    /// (is a root entity) or is not alive.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is safe to call with stale entity handles. A stale handle refers to
    /// an entity that has been destroyed (via <see cref="Despawn"/>). In such cases,
    /// this method returns <see cref="Entity.Null"/> rather than throwing an exception.
    /// </para>
    /// <para>
    /// This operation is O(1) for the dictionary-based storage implementation.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var parent = world.Spawn().Build();
    /// var child = world.Spawn().Build();
    /// world.SetParent(child, parent);
    ///
    /// var foundParent = world.GetParent(child);
    /// Debug.Assert(foundParent == parent);
    ///
    /// var rootParent = world.GetParent(parent);
    /// Debug.Assert(!rootParent.IsValid); // Root entities have no parent
    /// </code>
    /// </example>
    /// <seealso cref="SetParent(Entity, Entity)"/>
    /// <seealso cref="GetChildren(Entity)"/>
    public Entity GetParent(Entity entity)
        => hierarchyManager.GetParent(entity);

    /// <summary>
    /// Gets all immediate children of an entity.
    /// </summary>
    /// <param name="entity">The entity to get children for.</param>
    /// <returns>
    /// An enumerable of child entities. Returns an empty sequence if the entity has no
    /// children or is not alive.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method only returns immediate children, not grandchildren or other descendants.
    /// Use <see cref="GetDescendants(Entity)"/> to get all descendants recursively.
    /// </para>
    /// <para>
    /// This method is safe to call with stale entity handles. A stale handle refers to
    /// an entity that has been destroyed (via <see cref="Despawn"/>). In such cases,
    /// this method returns an empty sequence rather than throwing an exception.
    /// </para>
    /// <para>
    /// This operation is O(C) where C is the number of children.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var parent = world.Spawn().Build();
    /// var child1 = world.Spawn().Build();
    /// var child2 = world.Spawn().Build();
    ///
    /// world.SetParent(child1, parent);
    /// world.SetParent(child2, parent);
    ///
    /// foreach (var child in world.GetChildren(parent))
    /// {
    ///     Console.WriteLine($"Child: {child}");
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="GetParent(Entity)"/>
    /// <seealso cref="GetDescendants(Entity)"/>
    public IEnumerable<Entity> GetChildren(Entity entity)
        => hierarchyManager.GetChildren(entity);

    /// <summary>
    /// Adds a child entity to a parent entity.
    /// </summary>
    /// <param name="parent">The parent entity.</param>
    /// <param name="child">The entity to add as a child.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when either entity is not alive, or when the relationship would create
    /// a circular hierarchy.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This is a convenience method equivalent to calling <c>SetParent(child, parent)</c>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var parent = world.Spawn().Build();
    /// var child = world.Spawn().Build();
    ///
    /// world.AddChild(parent, child);
    /// // Equivalent to: world.SetParent(child, parent);
    /// </code>
    /// </example>
    /// <seealso cref="SetParent(Entity, Entity)"/>
    /// <seealso cref="RemoveChild(Entity, Entity)"/>
    public void AddChild(Entity parent, Entity child)
        => hierarchyManager.AddChild(parent, child);

    /// <summary>
    /// Removes a specific child from a parent entity.
    /// </summary>
    /// <param name="parent">The parent entity.</param>
    /// <param name="child">The child entity to remove from the parent.</param>
    /// <returns>
    /// <c>true</c> if the child was removed from the parent; <c>false</c> if the parent
    /// is not alive, the child is not alive, or the child was not a child of the parent.
    /// </returns>
    /// <remarks>
    /// <para>
    /// After removal, the child becomes a root entity (has no parent).
    /// </para>
    /// <para>
    /// This operation is idempotent: calling it multiple times with the same arguments
    /// will return <c>false</c> after the first successful removal.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var parent = world.Spawn().Build();
    /// var child = world.Spawn().Build();
    ///
    /// world.SetParent(child, parent);
    /// bool removed = world.RemoveChild(parent, child);
    /// Debug.Assert(removed);
    /// Debug.Assert(!world.GetParent(child).IsValid); // Child is now a root
    /// </code>
    /// </example>
    /// <seealso cref="AddChild(Entity, Entity)"/>
    /// <seealso cref="SetParent(Entity, Entity)"/>
    public bool RemoveChild(Entity parent, Entity child)
        => hierarchyManager.RemoveChild(parent, child);

    /// <summary>
    /// Gets all descendants of an entity (children, grandchildren, etc.).
    /// </summary>
    /// <param name="entity">The entity to get descendants for.</param>
    /// <returns>
    /// An enumerable of all descendant entities in breadth-first order.
    /// Returns an empty sequence if the entity has no descendants or is not alive.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method performs a breadth-first traversal of the hierarchy starting from
    /// the given entity. The entity itself is not included in the results.
    /// </para>
    /// <para>
    /// This method is safe to call with stale entity handles. A stale handle refers to
    /// an entity that has been destroyed (via <see cref="Despawn"/>). In such cases,
    /// this method returns an empty sequence rather than throwing an exception.
    /// </para>
    /// <para>
    /// This operation is O(N) where N is the total number of descendants.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create a hierarchy: root -> child -> grandchild
    /// var root = world.Spawn().Build();
    /// var child = world.Spawn().Build();
    /// var grandchild = world.Spawn().Build();
    ///
    /// world.SetParent(child, root);
    /// world.SetParent(grandchild, child);
    ///
    /// // GetDescendants returns: child, grandchild
    /// foreach (var descendant in world.GetDescendants(root))
    /// {
    ///     Console.WriteLine($"Descendant: {descendant}");
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="GetChildren(Entity)"/>
    /// <seealso cref="GetAncestors(Entity)"/>
    public IEnumerable<Entity> GetDescendants(Entity entity)
        => hierarchyManager.GetDescendants(entity);

    /// <summary>
    /// Gets all ancestors of an entity (parent, grandparent, etc.).
    /// </summary>
    /// <param name="entity">The entity to get ancestors for.</param>
    /// <returns>
    /// An enumerable of all ancestor entities, starting with the immediate parent
    /// and ending with the root. Returns an empty sequence if the entity has no
    /// parent or is not alive.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method walks up the hierarchy from the given entity to the root.
    /// The entity itself is not included in the results.
    /// </para>
    /// <para>
    /// This method is safe to call with stale entity handles. A stale handle refers to
    /// an entity that has been destroyed (via <see cref="Despawn"/>). In such cases,
    /// this method returns an empty sequence rather than throwing an exception.
    /// </para>
    /// <para>
    /// This operation is O(D) where D is the depth of the hierarchy.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create a hierarchy: root -> child -> grandchild
    /// var root = world.Spawn().Build();
    /// var child = world.Spawn().Build();
    /// var grandchild = world.Spawn().Build();
    ///
    /// world.SetParent(child, root);
    /// world.SetParent(grandchild, child);
    ///
    /// // GetAncestors(grandchild) returns: child, root
    /// foreach (var ancestor in world.GetAncestors(grandchild))
    /// {
    ///     Console.WriteLine($"Ancestor: {ancestor}");
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="GetParent(Entity)"/>
    /// <seealso cref="GetRoot(Entity)"/>
    /// <seealso cref="GetDescendants(Entity)"/>
    public IEnumerable<Entity> GetAncestors(Entity entity)
        => hierarchyManager.GetAncestors(entity);

    /// <summary>
    /// Gets the root entity of the hierarchy containing the given entity.
    /// </summary>
    /// <param name="entity">The entity to find the root for.</param>
    /// <returns>
    /// The root entity (the topmost ancestor with no parent). If the entity itself
    /// has no parent, returns the entity itself. Returns <see cref="Entity.Null"/>
    /// if the entity is not alive.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method walks up the hierarchy until it finds an entity with no parent.
    /// If the given entity has no parent, it is itself the root and is returned.
    /// </para>
    /// <para>
    /// This method is safe to call with stale entity handles. A stale handle refers to
    /// an entity that has been destroyed (via <see cref="Despawn"/>). In such cases,
    /// this method returns <see cref="Entity.Null"/> rather than throwing an exception.
    /// </para>
    /// <para>
    /// This operation is O(D) where D is the depth of the hierarchy.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create a hierarchy: root -> child -> grandchild
    /// var root = world.Spawn().Build();
    /// var child = world.Spawn().Build();
    /// var grandchild = world.Spawn().Build();
    ///
    /// world.SetParent(child, root);
    /// world.SetParent(grandchild, child);
    ///
    /// var foundRoot = world.GetRoot(grandchild);
    /// Debug.Assert(foundRoot == root);
    ///
    /// // Root entity returns itself
    /// Debug.Assert(world.GetRoot(root) == root);
    /// </code>
    /// </example>
    /// <seealso cref="GetParent(Entity)"/>
    /// <seealso cref="GetAncestors(Entity)"/>
    public Entity GetRoot(Entity entity)
        => hierarchyManager.GetRoot(entity);

    /// <summary>
    /// Destroys an entity and all its descendants recursively.
    /// </summary>
    /// <param name="entity">The entity to destroy along with all its descendants.</param>
    /// <returns>
    /// The number of entities destroyed (including the root entity and all descendants).
    /// Returns 0 if the entity was not alive.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method performs a depth-first traversal to destroy all descendants before
    /// destroying the entity itself. This ensures proper cleanup of the hierarchy.
    /// </para>
    /// <para>
    /// Unlike <see cref="Despawn(Entity)"/> which orphans children, this method
    /// completely removes the entity and its entire subtree from the world.
    /// </para>
    /// <para>
    /// This method is safe to call with stale entity handles. A stale handle refers to
    /// an entity that has been destroyed (via <see cref="Despawn"/>). In such cases,
    /// this method returns 0 rather than throwing an exception.
    /// </para>
    /// <para>
    /// This operation is O(N) where N is the total number of entities in the subtree.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create a hierarchy: root -> child -> grandchild
    /// var root = world.Spawn().Build();
    /// var child = world.Spawn().Build();
    /// var grandchild = world.Spawn().Build();
    ///
    /// world.SetParent(child, root);
    /// world.SetParent(grandchild, child);
    ///
    /// // Destroy entire hierarchy
    /// int count = world.DespawnRecursive(root);
    /// Debug.Assert(count == 3); // root, child, grandchild
    /// Debug.Assert(!world.IsAlive(root));
    /// Debug.Assert(!world.IsAlive(child));
    /// Debug.Assert(!world.IsAlive(grandchild));
    /// </code>
    /// </example>
    /// <seealso cref="Despawn(Entity)"/>
    /// <seealso cref="GetDescendants(Entity)"/>
    public int DespawnRecursive(Entity entity)
        => hierarchyManager.DespawnRecursive(entity);

    #endregion

    #region Prefabs

    /// <summary>
    /// Registers a prefab with the given name for later instantiation.
    /// </summary>
    /// <param name="name">The unique name for the prefab.</param>
    /// <param name="prefab">The prefab definition to register.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name"/> or <paramref name="prefab"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when a prefab with the given name is already registered.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Prefabs are reusable entity templates that define a set of components. Once registered,
    /// entities can be created from the prefab using <see cref="SpawnFromPrefab(string)"/>.
    /// </para>
    /// <para>
    /// Prefabs support inheritance through <see cref="EntityPrefab.Extends(string)"/>. When
    /// spawning from a derived prefab, the inheritance chain is resolved and components are
    /// merged, with derived prefabs overriding base prefab components of the same type.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Define a base enemy prefab
    /// var enemyPrefab = new EntityPrefab()
    ///     .With(new Health { Current = 100, Max = 100 })
    ///     .With(new Position { X = 0, Y = 0 })
    ///     .WithTag&lt;EnemyTag&gt;();
    ///
    /// world.RegisterPrefab("Enemy", enemyPrefab);
    ///
    /// // Create entities from the prefab
    /// var enemy1 = world.SpawnFromPrefab("Enemy").Build();
    /// var enemy2 = world.SpawnFromPrefab("Enemy").Build();
    /// </code>
    /// </example>
    /// <seealso cref="SpawnFromPrefab(string)"/>
    /// <seealso cref="SpawnFromPrefab(string, string?)"/>
    /// <seealso cref="HasPrefab(string)"/>
    public void RegisterPrefab(string name, EntityPrefab prefab)
        => prefabManager.Register(name, prefab);

    /// <summary>
    /// Spawns an entity from a registered prefab.
    /// </summary>
    /// <param name="name">The name of the prefab to spawn from.</param>
    /// <returns>
    /// An entity builder pre-configured with the prefab's components.
    /// Call <see cref="EntityBuilder.Build"/> to create the entity.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no prefab with the given name is registered, or when the prefab has
    /// a circular inheritance chain.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The returned builder can be used to add additional components or apply overrides
    /// before creating the entity. This allows customizing individual instances while
    /// still using the prefab as a base template.
    /// </para>
    /// <para>
    /// If the prefab has a base prefab (via <see cref="EntityPrefab.Extends(string)"/>),
    /// the inheritance chain is resolved and all inherited components are included.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Spawn with default prefab values
    /// var enemy1 = world.SpawnFromPrefab("Enemy").Build();
    ///
    /// // Spawn with overridden position
    /// var enemy2 = world.SpawnFromPrefab("Enemy")
    ///     .With(new Position { X = 100, Y = 50 })
    ///     .Build();
    /// </code>
    /// </example>
    /// <seealso cref="RegisterPrefab(string, EntityPrefab)"/>
    /// <seealso cref="SpawnFromPrefab(string, string?)"/>
    public EntityBuilder SpawnFromPrefab(string name)
        => prefabManager.SpawnFromPrefab(name);

    /// <summary>
    /// Spawns a named entity from a registered prefab.
    /// </summary>
    /// <param name="prefabName">The name of the prefab to spawn from.</param>
    /// <param name="entityName">
    /// The name for the spawned entity, or <c>null</c> for an unnamed entity.
    /// Must be unique within this world if provided.
    /// </param>
    /// <returns>
    /// An entity builder pre-configured with the prefab's components.
    /// Call <see cref="EntityBuilder.Build"/> to create the entity.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="prefabName"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no prefab with the given name is registered, or when the prefab has
    /// a circular inheritance chain.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="entityName"/> is already assigned to another entity.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Named entities can be retrieved later using <see cref="GetEntityByName(string)"/>.
    /// This is useful for scenarios where entities need human-readable identifiers,
    /// such as debugging or editor tooling.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Spawn a named entity from a prefab
    /// var player = world.SpawnFromPrefab("Player", "MainPlayer").Build();
    ///
    /// // Later, retrieve by name
    /// var foundPlayer = world.GetEntityByName("MainPlayer");
    /// </code>
    /// </example>
    /// <seealso cref="RegisterPrefab(string, EntityPrefab)"/>
    /// <seealso cref="SpawnFromPrefab(string)"/>
    /// <seealso cref="GetEntityByName(string)"/>
    public EntityBuilder SpawnFromPrefab(string prefabName, string? entityName)
        => prefabManager.SpawnFromPrefab(prefabName, entityName);

    /// <summary>
    /// Checks if a prefab with the given name is registered.
    /// </summary>
    /// <param name="name">The name to check.</param>
    /// <returns><c>true</c> if a prefab with the name exists; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null.</exception>
    /// <example>
    /// <code>
    /// if (world.HasPrefab("Enemy"))
    /// {
    ///     var enemy = world.SpawnFromPrefab("Enemy").Build();
    /// }
    /// </code>
    /// </example>
    public bool HasPrefab(string name)
        => prefabManager.HasPrefab(name);

    /// <summary>
    /// Unregisters a prefab by name.
    /// </summary>
    /// <param name="name">The name of the prefab to remove.</param>
    /// <returns><c>true</c> if the prefab was removed; <c>false</c> if it wasn't registered.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null.</exception>
    /// <remarks>
    /// Unregistering a prefab does not affect entities that were already spawned from it.
    /// Those entities continue to exist with their components intact.
    /// </remarks>
    public bool UnregisterPrefab(string name)
        => prefabManager.Unregister(name);

    /// <summary>
    /// Gets all registered prefab names.
    /// </summary>
    /// <returns>An enumerable of all registered prefab names.</returns>
    /// <remarks>
    /// The returned names are in no particular order.
    /// </remarks>
    public IEnumerable<string> GetAllPrefabNames()
        => prefabManager.GetAllPrefabNames();

    #endregion

    #region Queries

    /// <summary>
    /// Creates a query for entities with the specified component.
    /// </summary>
    public QueryBuilder<T1> Query<T1>()
        where T1 : struct, IComponent
    {
        return new QueryBuilder<T1>(this);
    }

    /// <summary>
    /// Creates a query for entities with the specified components.
    /// </summary>
    public QueryBuilder<T1, T2> Query<T1, T2>()
        where T1 : struct, IComponent
        where T2 : struct, IComponent
    {
        return new QueryBuilder<T1, T2>(this);
    }

    /// <summary>
    /// Creates a query for entities with the specified components.
    /// </summary>
    public QueryBuilder<T1, T2, T3> Query<T1, T2, T3>()
        where T1 : struct, IComponent
        where T2 : struct, IComponent
        where T3 : struct, IComponent
    {
        return new QueryBuilder<T1, T2, T3>(this);
    }

    /// <summary>
    /// Creates a query for entities with the specified components.
    /// </summary>
    public QueryBuilder<T1, T2, T3, T4> Query<T1, T2, T3, T4>()
        where T1 : struct, IComponent
        where T2 : struct, IComponent
        where T3 : struct, IComponent
        where T4 : struct, IComponent
    {
        return new QueryBuilder<T1, T2, T3, T4>(this);
    }

    #endregion

    #region Systems

    /// <summary>
    /// Adds a system to this world with specified execution phase and order.
    /// </summary>
    /// <typeparam name="T">The system type to add.</typeparam>
    /// <param name="phase">The execution phase for this system. Defaults to <see cref="SystemPhase.Update"/>.</param>
    /// <param name="order">The execution order within the phase. Lower values execute first. Defaults to 0.</param>
    /// <returns>This world for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// Systems are automatically sorted by phase then by order when <see cref="Update"/> is called.
    /// Systems in earlier phases (e.g., <see cref="SystemPhase.EarlyUpdate"/>) execute before
    /// systems in later phases (e.g., <see cref="SystemPhase.LateUpdate"/>).
    /// </para>
    /// <para>
    /// Within the same phase, systems with lower order values execute first.
    /// Systems with the same phase and order maintain stable relative ordering.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// world.AddSystem&lt;InputSystem&gt;(SystemPhase.EarlyUpdate, order: -10)
    ///      .AddSystem&lt;PhysicsSystem&gt;(SystemPhase.FixedUpdate)
    ///      .AddSystem&lt;MovementSystem&gt;(SystemPhase.Update, order: 0)
    ///      .AddSystem&lt;RenderSystem&gt;(SystemPhase.Render);
    /// </code>
    /// </example>
    public World AddSystem<T>(SystemPhase phase = SystemPhase.Update, int order = 0) where T : ISystem, new()
    {
        systemManager.AddSystem<T>(phase, order, runsBefore: [], runsAfter: []);
        return this;
    }

    /// <summary>
    /// Adds a system to this world with specified execution phase, order, and dependency constraints.
    /// </summary>
    /// <typeparam name="T">The system type to add.</typeparam>
    /// <param name="phase">The execution phase for this system.</param>
    /// <param name="order">The execution order within the phase. Lower values execute first.</param>
    /// <param name="runsBefore">Types of systems that this system must run before.</param>
    /// <param name="runsAfter">Types of systems that this system must run after.</param>
    /// <returns>This world for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// The <paramref name="runsBefore"/> and <paramref name="runsAfter"/> constraints define
    /// explicit ordering dependencies between systems within the same phase. Systems are sorted
    /// using topological sorting to respect these constraints.
    /// </para>
    /// <para>
    /// If constraints create a cycle (e.g., A runs before B, B runs before A), an
    /// <see cref="InvalidOperationException"/> is thrown during the first <see cref="Update"/> call.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // MovementSystem must run after InputSystem and before RenderSystem
    /// world.AddSystem&lt;MovementSystem&gt;(
    ///     SystemPhase.Update,
    ///     order: 0,
    ///     runsBefore: [typeof(RenderSystem)],
    ///     runsAfter: [typeof(InputSystem)]);
    /// </code>
    /// </example>
    public World AddSystem<T>(
        SystemPhase phase,
        int order,
        Type[] runsBefore,
        Type[] runsAfter) where T : ISystem, new()
    {
        systemManager.AddSystem<T>(phase, order, runsBefore, runsAfter);
        return this;
    }

    /// <summary>
    /// Adds a system instance to this world with specified execution phase and order.
    /// </summary>
    /// <param name="system">The system instance to add.</param>
    /// <param name="phase">The execution phase for this system. Defaults to <see cref="SystemPhase.Update"/>.</param>
    /// <param name="order">The execution order within the phase. Lower values execute first. Defaults to 0.</param>
    /// <returns>This world for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// Use this overload when you need to pass a pre-configured system instance or a system
    /// that requires constructor parameters.
    /// </para>
    /// <para>
    /// Systems are automatically sorted by phase then by order when <see cref="Update"/> is called.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var customSystem = new CustomSystem("config");
    /// world.AddSystem(customSystem, SystemPhase.Update, order: 10);
    /// </code>
    /// </example>
    public World AddSystem(ISystem system, SystemPhase phase = SystemPhase.Update, int order = 0)
    {
        systemManager.AddSystem(system, phase, order, runsBefore: [], runsAfter: []);
        return this;
    }

    /// <summary>
    /// Adds a system instance to this world with specified execution phase, order, and dependency constraints.
    /// </summary>
    /// <param name="system">The system instance to add.</param>
    /// <param name="phase">The execution phase for this system.</param>
    /// <param name="order">The execution order within the phase. Lower values execute first.</param>
    /// <param name="runsBefore">Types of systems that this system must run before.</param>
    /// <param name="runsAfter">Types of systems that this system must run after.</param>
    /// <returns>This world for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// Use this overload when you need to pass a pre-configured system instance with explicit
    /// ordering constraints.
    /// </para>
    /// <para>
    /// The <paramref name="runsBefore"/> and <paramref name="runsAfter"/> constraints define
    /// explicit ordering dependencies between systems within the same phase. Systems are sorted
    /// using topological sorting to respect these constraints.
    /// </para>
    /// <para>
    /// If constraints create a cycle (e.g., A runs before B, B runs before A), an
    /// <see cref="InvalidOperationException"/> is thrown during the first <see cref="Update"/> call.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var customSystem = new CustomSystem("config");
    /// world.AddSystem(
    ///     customSystem,
    ///     SystemPhase.Update,
    ///     order: 0,
    ///     runsBefore: [typeof(RenderSystem)],
    ///     runsAfter: [typeof(InputSystem)]);
    /// </code>
    /// </example>
    public World AddSystem(
        ISystem system,
        SystemPhase phase,
        int order,
        Type[] runsBefore,
        Type[] runsAfter)
    {
        systemManager.AddSystem(system, phase, order, runsBefore, runsAfter);
        return this;
    }

    /// <summary>
    /// Updates all enabled systems with the given delta time.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update.</param>
    /// <remarks>
    /// <para>
    /// Systems are executed in order of their phase (earliest first), then by their order
    /// value within each phase (lowest first). Disabled systems are skipped.
    /// </para>
    /// <para>
    /// For systems derived from <see cref="SystemBase"/>, the following lifecycle methods
    /// are called in order: <c>OnBeforeUpdate</c>, <c>Update</c>, <c>OnAfterUpdate</c>.
    /// </para>
    /// </remarks>
    public void Update(float deltaTime)
        => systemManager.Update(deltaTime);

    /// <summary>
    /// Updates only systems in the <see cref="SystemPhase.FixedUpdate"/> phase.
    /// </summary>
    /// <param name="fixedDeltaTime">The fixed timestep interval.</param>
    /// <remarks>
    /// <para>
    /// This method is intended for fixed-timestep physics and simulation updates that
    /// should run at a consistent rate regardless of frame rate. Call this method from
    /// your game loop's fixed update tick.
    /// </para>
    /// <para>
    /// Only systems registered with <see cref="SystemPhase.FixedUpdate"/> will be executed.
    /// Systems in other phases are not affected by this method.
    /// </para>
    /// <para>
    /// For systems derived from <see cref="SystemBase"/>, the following lifecycle methods
    /// are called in order: <c>OnBeforeUpdate</c>, <c>Update</c>, <c>OnAfterUpdate</c>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Typical game loop with fixed timestep
    /// float accumulator = 0f;
    /// const float fixedDeltaTime = 1f / 60f;
    ///
    /// while (running)
    /// {
    ///     float frameTime = GetFrameTime();
    ///     accumulator += frameTime;
    ///
    ///     while (accumulator >= fixedDeltaTime)
    ///     {
    ///         world.FixedUpdate(fixedDeltaTime);
    ///         accumulator -= fixedDeltaTime;
    ///     }
    ///
    ///     world.Update(frameTime);
    /// }
    /// </code>
    /// </example>
    public void FixedUpdate(float fixedDeltaTime)
        => systemManager.FixedUpdate(fixedDeltaTime);

    /// <summary>
    /// Gets a system of the specified type from this world.
    /// </summary>
    /// <typeparam name="T">The type of system to retrieve.</typeparam>
    /// <returns>The system instance, or null if not found.</returns>
    /// <remarks>
    /// This method searches for systems by type, including systems nested within
    /// <see cref="SystemGroup"/> instances.
    /// </remarks>
    /// <example>
    /// <code>
    /// var physics = world.GetSystem&lt;PhysicsSystem&gt;();
    /// if (physics is not null)
    /// {
    ///     physics.Enabled = false; // Pause physics
    /// }
    /// </code>
    /// </example>
    public T? GetSystem<T>() where T : class, ISystem
        => systemManager.GetSystem<T>();

    /// <summary>
    /// Enables a system of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of system to enable.</typeparam>
    /// <returns>True if the system was found and enabled; false otherwise.</returns>
    /// <remarks>
    /// <para>
    /// If the system was already enabled, this method has no effect but still returns true.
    /// </para>
    /// <para>
    /// For systems derived from <see cref="SystemBase"/>, the <c>OnEnabled</c> callback
    /// is invoked when transitioning from disabled to enabled state.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Resume physics simulation
    /// world.EnableSystem&lt;PhysicsSystem&gt;();
    /// </code>
    /// </example>
    public bool EnableSystem<T>() where T : class, ISystem
        => systemManager.EnableSystem<T>();

    /// <summary>
    /// Disables a system of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of system to disable.</typeparam>
    /// <returns>True if the system was found and disabled; false otherwise.</returns>
    /// <remarks>
    /// <para>
    /// Disabled systems are skipped during <see cref="Update"/> calls.
    /// </para>
    /// <para>
    /// If the system was already disabled, this method has no effect but still returns true.
    /// </para>
    /// <para>
    /// For systems derived from <see cref="SystemBase"/>, the <c>OnDisabled</c> callback
    /// is invoked when transitioning from enabled to disabled state.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Pause physics simulation
    /// world.DisableSystem&lt;PhysicsSystem&gt;();
    /// </code>
    /// </example>
    public bool DisableSystem<T>() where T : class, ISystem
        => systemManager.DisableSystem<T>();

    #endregion

    #region Singletons

    /// <summary>
    /// Sets or updates a singleton value for this world.
    /// </summary>
    /// <typeparam name="T">The singleton type. Must be a value type.</typeparam>
    /// <param name="value">The singleton value to store.</param>
    /// <remarks>
    /// <para>
    /// Singletons are world-level data not tied to any entity. They are useful for
    /// storing global state like time, input, configuration, or other resources that
    /// systems need to access.
    /// </para>
    /// <para>
    /// If a singleton of type <typeparamref name="T"/> already exists, it will be replaced.
    /// Use <see cref="HasSingleton{T}"/> to check existence before calling this method
    /// if you want to avoid overwrites.
    /// </para>
    /// <para>
    /// This operation is O(1) for the dictionary-based storage implementation.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Store game time as a singleton
    /// world.SetSingleton(new GameTime { DeltaTime = 0.016f, TotalTime = 10.5f });
    ///
    /// // Update existing singleton
    /// world.SetSingleton(new GameTime { DeltaTime = 0.016f, TotalTime = 10.516f });
    /// </code>
    /// </example>
    /// <seealso cref="GetSingleton{T}"/>
    /// <seealso cref="TryGetSingleton{T}(out T)"/>
    /// <seealso cref="HasSingleton{T}"/>
    /// <seealso cref="RemoveSingleton{T}"/>
    public void SetSingleton<T>(in T value) where T : struct
        => singletonManager.SetSingleton(in value);

    /// <summary>
    /// Gets a singleton value by reference, allowing direct modification.
    /// </summary>
    /// <typeparam name="T">The singleton type to retrieve.</typeparam>
    /// <returns>A reference to the singleton data for zero-copy access.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no singleton of type <typeparamref name="T"/> exists in this world.
    /// Use <see cref="TryGetSingleton{T}(out T)"/> or <see cref="HasSingleton{T}"/> to check
    /// existence before calling this method if the singleton may not exist.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method returns a reference to the boxed singleton value, enabling zero-copy
    /// access and direct modification. Changes made through the returned reference
    /// are immediately reflected in the stored singleton.
    /// </para>
    /// <para>
    /// This operation is O(1) for the dictionary-based storage implementation.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Read singleton value
    /// ref readonly var time = ref world.GetSingleton&lt;GameTime&gt;();
    /// float delta = time.DeltaTime;
    ///
    /// // Modify singleton directly (zero-copy)
    /// ref var config = ref world.GetSingleton&lt;GameConfig&gt;();
    /// config.Difficulty = 2;
    /// </code>
    /// </example>
    /// <seealso cref="SetSingleton{T}(in T)"/>
    /// <seealso cref="TryGetSingleton{T}(out T)"/>
    /// <seealso cref="HasSingleton{T}"/>
    public ref T GetSingleton<T>() where T : struct
        => ref singletonManager.GetSingleton<T>();

    /// <summary>
    /// Attempts to get a singleton value.
    /// </summary>
    /// <typeparam name="T">The singleton type to retrieve.</typeparam>
    /// <param name="value">
    /// When this method returns <c>true</c>, contains the singleton value.
    /// When this method returns <c>false</c>, contains the default value of <typeparamref name="T"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the singleton exists; <c>false</c> otherwise.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method provides a safe way to retrieve singletons without throwing exceptions.
    /// Unlike <see cref="GetSingleton{T}"/>, this method returns a copy of the value rather
    /// than a reference, so modifications will not affect the stored singleton.
    /// </para>
    /// <para>
    /// Use <see cref="GetSingleton{T}"/> when you need zero-copy access or want to modify
    /// the singleton in place.
    /// </para>
    /// <para>
    /// This operation is O(1) for the dictionary-based storage implementation.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// if (world.TryGetSingleton&lt;GameTime&gt;(out var time))
    /// {
    ///     Console.WriteLine($"Delta: {time.DeltaTime}");
    /// }
    /// else
    /// {
    ///     Console.WriteLine("GameTime singleton not set");
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="GetSingleton{T}"/>
    /// <seealso cref="HasSingleton{T}"/>
    public bool TryGetSingleton<T>(out T value) where T : struct
        => singletonManager.TryGetSingleton(out value);

    /// <summary>
    /// Checks if a singleton of the specified type exists in this world.
    /// </summary>
    /// <typeparam name="T">The singleton type to check for.</typeparam>
    /// <returns>
    /// <c>true</c> if a singleton of type <typeparamref name="T"/> exists;
    /// <c>false</c> otherwise.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method provides a quick way to check singleton existence without
    /// retrieving the value or risking exceptions.
    /// </para>
    /// <para>
    /// This operation is O(1) for the dictionary-based storage implementation.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// if (world.HasSingleton&lt;GameTime&gt;())
    /// {
    ///     ref var time = ref world.GetSingleton&lt;GameTime&gt;();
    ///     // Use time...
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="GetSingleton{T}"/>
    /// <seealso cref="TryGetSingleton{T}(out T)"/>
    /// <seealso cref="SetSingleton{T}(in T)"/>
    public bool HasSingleton<T>() where T : struct
        => singletonManager.HasSingleton<T>();

    /// <summary>
    /// Removes a singleton from this world.
    /// </summary>
    /// <typeparam name="T">The singleton type to remove.</typeparam>
    /// <returns>
    /// <c>true</c> if the singleton was removed; <c>false</c> if no singleton
    /// of type <typeparamref name="T"/> existed.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This operation is idempotent: calling it multiple times with the same type
    /// will return <c>false</c> after the first successful removal.
    /// </para>
    /// <para>
    /// This operation is O(1) for the dictionary-based storage implementation.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Remove a singleton
    /// bool removed = world.RemoveSingleton&lt;GameTime&gt;();
    /// if (removed)
    /// {
    ///     Console.WriteLine("GameTime singleton removed");
    /// }
    ///
    /// // Idempotent: second removal returns false
    /// bool removedAgain = world.RemoveSingleton&lt;GameTime&gt;();
    /// Debug.Assert(removedAgain == false);
    /// </code>
    /// </example>
    /// <seealso cref="SetSingleton{T}(in T)"/>
    /// <seealso cref="HasSingleton{T}"/>
    public bool RemoveSingleton<T>() where T : struct
        => singletonManager.RemoveSingleton<T>();

    /// <summary>
    /// Gets all singletons stored in this world.
    /// </summary>
    /// <returns>
    /// An enumerable of tuples containing each singleton's type and boxed value.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is primarily intended for serialization and debugging scenarios.
    /// Values are boxed, so avoid using this in performance-critical paths.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Snapshot all singletons for serialization
    /// var singletonSnapshot = world.GetAllSingletons()
    ///     .ToDictionary(s => s.Type.FullName, s => s.Value);
    /// </code>
    /// </example>
    /// <seealso cref="SetSingleton{T}(in T)"/>
    /// <seealso cref="GetSingleton{T}"/>
    public IEnumerable<(Type Type, object Value)> GetAllSingletons()
        => singletonManager.GetAllSingletons();

    #endregion

    #region Validation

    /// <summary>
    /// Gets or sets the validation mode for component constraints.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Validation checks <see cref="RequiresComponentAttribute"/> and
    /// <see cref="ConflictsWithAttribute"/> constraints when components are added.
    /// </para>
    /// <para>
    /// Available modes:
    /// <list type="bullet">
    /// <item><description><see cref="ValidationMode.Enabled"/> - Always validate (default)</description></item>
    /// <item><description><see cref="ValidationMode.Disabled"/> - Skip all validation</description></item>
    /// <item><description><see cref="ValidationMode.DebugOnly"/> - Only validate in DEBUG builds</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Disable validation for maximum performance in production
    /// world.ValidationMode = ValidationMode.Disabled;
    ///
    /// // Enable validation only in debug builds
    /// world.ValidationMode = ValidationMode.DebugOnly;
    /// </code>
    /// </example>
    public ValidationMode ValidationMode
    {
        get => validationManager.Mode;
        set => validationManager.Mode = value;
    }

    /// <summary>
    /// Registers a custom validator for a component type.
    /// </summary>
    /// <typeparam name="T">The component type to validate.</typeparam>
    /// <param name="validator">
    /// A delegate that receives the world, entity, and component data,
    /// and returns <c>true</c> if validation passes.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="validator"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// Custom validators run in addition to attribute-based validation
    /// (<see cref="RequiresComponentAttribute"/> and <see cref="ConflictsWithAttribute"/>).
    /// </para>
    /// <para>
    /// The validator is called when the component is added via <see cref="Add{T}"/>
    /// or during entity creation via <see cref="EntityBuilder.Build"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Validate that Health.Current never exceeds Health.Max
    /// world.RegisterValidator&lt;Health&gt;((world, entity, health) =>
    ///     health.Current &gt;= 0 &amp;&amp; health.Current &lt;= health.Max &amp;&amp; health.Max &gt; 0);
    ///
    /// // This will throw ComponentValidationException:
    /// world.Add(entity, new Health { Current = 150, Max = 100 });
    /// </code>
    /// </example>
    public void RegisterValidator<T>(ComponentValidator<T> validator) where T : struct, IComponent
        => validationManager.RegisterValidator(validator);

    /// <summary>
    /// Removes a previously registered custom validator for a component type.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <returns><c>true</c> if a validator was removed; <c>false</c> if no validator was registered.</returns>
    public bool UnregisterValidator<T>() where T : struct, IComponent
        => validationManager.UnregisterValidator<T>();

    #endregion

    #region Extensions

    /// <summary>
    /// Sets or updates an extension value for this world.
    /// </summary>
    /// <typeparam name="T">The extension type. Must be a reference type.</typeparam>
    /// <param name="extension">The extension instance to store.</param>
    /// <remarks>
    /// <para>
    /// Extensions are typically set by plugins to expose custom APIs.
    /// For example, a physics plugin might expose a <c>PhysicsWorld</c> extension
    /// that provides raycast and collision query methods.
    /// </para>
    /// <para>
    /// If an extension of type <typeparamref name="T"/> already exists, it will be replaced.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In a plugin:
    /// world.SetExtension(new PhysicsWorld());
    ///
    /// // In application code:
    /// var physics = world.GetExtension&lt;PhysicsWorld&gt;();
    /// var hit = physics.Raycast(origin, direction);
    /// </code>
    /// </example>
    /// <seealso cref="GetExtension{T}"/>
    /// <seealso cref="TryGetExtension{T}"/>
    /// <seealso cref="HasExtension{T}"/>
    public void SetExtension<T>(T extension) where T : class
        => extensionManager.SetExtension(extension);

    /// <summary>
    /// Gets an extension by type.
    /// </summary>
    /// <typeparam name="T">The extension type to retrieve.</typeparam>
    /// <returns>The extension instance.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no extension of type <typeparamref name="T"/> exists in this world.
    /// Use <see cref="TryGetExtension{T}"/> or <see cref="HasExtension{T}"/> to check
    /// existence before calling this method if the extension may not exist.
    /// </exception>
    /// <example>
    /// <code>
    /// var physics = world.GetExtension&lt;PhysicsWorld&gt;();
    /// var hit = physics.Raycast(origin, direction);
    /// </code>
    /// </example>
    public T GetExtension<T>() where T : class
        => extensionManager.GetExtension<T>();

    /// <summary>
    /// Tries to get an extension by type.
    /// </summary>
    /// <typeparam name="T">The extension type to retrieve.</typeparam>
    /// <param name="extension">When this method returns, contains the extension if found; otherwise, null.</param>
    /// <returns>True if the extension was found; false otherwise.</returns>
    /// <example>
    /// <code>
    /// if (world.TryGetExtension&lt;PhysicsWorld&gt;(out var physics))
    /// {
    ///     var hit = physics.Raycast(origin, direction);
    /// }
    /// </code>
    /// </example>
    public bool TryGetExtension<T>([System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out T extension) where T : class
        => extensionManager.TryGetExtension(out extension);

    /// <summary>
    /// Checks if an extension of the specified type exists.
    /// </summary>
    /// <typeparam name="T">The extension type to check for.</typeparam>
    /// <returns>True if the extension exists; false otherwise.</returns>
    public bool HasExtension<T>() where T : class
        => extensionManager.HasExtension<T>();

    /// <summary>
    /// Removes an extension from this world.
    /// </summary>
    /// <typeparam name="T">The extension type to remove.</typeparam>
    /// <returns>True if the extension was found and removed; false otherwise.</returns>
    public bool RemoveExtension<T>() where T : class
        => extensionManager.RemoveExtension<T>();

    #endregion

    #region Plugins

    /// <summary>
    /// Installs a plugin into this world.
    /// </summary>
    /// <typeparam name="T">The plugin type to install.</typeparam>
    /// <returns>This world for method chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a plugin with the same name is already installed.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The plugin's <see cref="IWorldPlugin.Install"/> method is called during installation.
    /// Systems registered by the plugin are tracked and will be automatically removed
    /// when the plugin is uninstalled.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// world.InstallPlugin&lt;PhysicsPlugin&gt;()
    ///      .InstallPlugin&lt;RenderingPlugin&gt;();
    /// </code>
    /// </example>
    public World InstallPlugin<T>() where T : IWorldPlugin, new()
    {
        pluginManager.InstallPlugin<T>();
        return this;
    }

    /// <summary>
    /// Installs a plugin instance into this world.
    /// </summary>
    /// <param name="plugin">The plugin instance to install.</param>
    /// <returns>This world for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when plugin is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a plugin with the same name is already installed.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Use this overload when you need to pass a pre-configured plugin instance.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var physics = new PhysicsPlugin(gravity: -9.81f);
    /// world.InstallPlugin(physics);
    /// </code>
    /// </example>
    public World InstallPlugin(IWorldPlugin plugin)
    {
        pluginManager.InstallPlugin(plugin);
        return this;
    }

    /// <summary>
    /// Uninstalls a plugin from this world.
    /// </summary>
    /// <typeparam name="T">The plugin type to uninstall.</typeparam>
    /// <returns>True if the plugin was found and uninstalled; false otherwise.</returns>
    /// <remarks>
    /// <para>
    /// The plugin's <see cref="IWorldPlugin.Uninstall"/> method is called during uninstallation.
    /// All systems registered by the plugin are automatically removed and disposed.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// world.UninstallPlugin&lt;PhysicsPlugin&gt;();
    /// </code>
    /// </example>
    public bool UninstallPlugin<T>() where T : IWorldPlugin
        => pluginManager.UninstallPlugin<T>();

    /// <summary>
    /// Uninstalls a plugin by name from this world.
    /// </summary>
    /// <param name="name">The name of the plugin to uninstall.</param>
    /// <returns>True if the plugin was found and uninstalled; false otherwise.</returns>
    public bool UninstallPlugin(string name)
        => pluginManager.UninstallPlugin(name);

    /// <summary>
    /// Gets a plugin of the specified type.
    /// </summary>
    /// <typeparam name="T">The plugin type to retrieve.</typeparam>
    /// <returns>The plugin instance, or null if not found.</returns>
    /// <example>
    /// <code>
    /// var physics = world.GetPlugin&lt;PhysicsPlugin&gt;();
    /// if (physics is not null)
    /// {
    ///     Console.WriteLine($"Physics plugin: {physics.Name}");
    /// }
    /// </code>
    /// </example>
    public T? GetPlugin<T>() where T : class, IWorldPlugin
        => pluginManager.GetPlugin<T>();

    /// <summary>
    /// Gets a plugin by name.
    /// </summary>
    /// <param name="name">The name of the plugin to retrieve.</param>
    /// <returns>The plugin instance, or null if not found.</returns>
    public IWorldPlugin? GetPlugin(string name)
        => pluginManager.GetPlugin(name);

    /// <summary>
    /// Checks if a plugin of the specified type is installed.
    /// </summary>
    /// <typeparam name="T">The plugin type to check for.</typeparam>
    /// <returns>True if the plugin is installed; false otherwise.</returns>
    public bool HasPlugin<T>() where T : IWorldPlugin
        => pluginManager.HasPlugin<T>();

    /// <summary>
    /// Checks if a plugin with the specified name is installed.
    /// </summary>
    /// <param name="name">The name of the plugin to check for.</param>
    /// <returns>True if the plugin is installed; false otherwise.</returns>
    public bool HasPlugin(string name)
        => pluginManager.HasPlugin(name);

    /// <summary>
    /// Gets all installed plugins.
    /// </summary>
    /// <returns>An enumerable of all installed plugins.</returns>
    public IEnumerable<IWorldPlugin> GetPlugins()
        => pluginManager.GetPlugins();

    #endregion

    #region Memory Statistics

    /// <summary>
    /// Gets memory usage statistics for this world.
    /// </summary>
    /// <returns>A snapshot of current memory statistics.</returns>
    /// <remarks>
    /// <para>
    /// Statistics are computed on-demand and represent a snapshot in time.
    /// They include entity allocations, component storage, and pooling metrics.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var stats = world.GetMemoryStats();
    /// Console.WriteLine($"Entities: {stats.EntitiesActive} active");
    /// Console.WriteLine($"Archetypes: {stats.ArchetypeCount}");
    /// Console.WriteLine($"Cache hit rate: {stats.QueryCacheHitRate:F1}%");
    /// </code>
    /// </example>
    public MemoryStats GetMemoryStats()
    {
        // Calculate estimated component bytes
        long estimatedBytes = 0;
        foreach (var archetype in archetypeManager.Archetypes)
        {
            foreach (var componentType in archetype.ComponentTypes)
            {
                var info = Components.Get(componentType);
                if (info is not null)
                {
                    estimatedBytes += (long)info.Size * archetype.Count;
                }
            }
        }

        return new MemoryStats
        {
            EntitiesAllocated = entityPool.TotalAllocated,
            EntitiesActive = entityPool.ActiveCount,
            EntitiesRecycled = entityPool.AvailableCount,
            EntityRecycleCount = entityPool.RecycleCount,
            ArchetypeCount = archetypeManager.ArchetypeCount,
            ComponentTypeCount = Components.Count,
            SystemCount = systemManager.Count,
            CachedQueryCount = queryManager.CachedQueryCount,
            QueryCacheHits = queryManager.CacheHits,
            QueryCacheMisses = queryManager.CacheMisses,
            EstimatedComponentBytes = estimatedBytes
        };
    }

    #endregion

    #region Events

    /// <summary>
    /// Registers a handler to be called when a component of type <typeparamref name="T"/>
    /// is added to an entity.
    /// </summary>
    /// <typeparam name="T">The component type to watch for additions.</typeparam>
    /// <param name="handler">
    /// The handler to invoke when the component is added. Receives the entity
    /// and the component value that was added.
    /// </param>
    /// <returns>
    /// An <see cref="EventSubscription"/> that can be disposed to unsubscribe the handler.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This event fires after the component has been successfully added to the entity.
    /// The entity is guaranteed to have the component when the handler is invoked.
    /// </para>
    /// <para>
    /// Component additions occur when calling <see cref="Add{T}(Entity, in T)"/> at runtime
    /// or when building an entity with <see cref="EntityBuilder.With{T}(T)"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var subscription = world.OnComponentAdded&lt;Health&gt;((entity, health) =>
    /// {
    ///     Console.WriteLine($"Entity {entity} now has {health.Current}/{health.Max} health");
    /// });
    ///
    /// // Later, unsubscribe
    /// subscription.Dispose();
    /// </code>
    /// </example>
    /// <seealso cref="OnComponentRemoved{T}(Action{Entity})"/>
    /// <seealso cref="OnComponentChanged{T}(Action{Entity, T, T})"/>
    public EventSubscription OnComponentAdded<T>(Action<Entity, T> handler) where T : struct, IComponent
        => eventManager.OnComponentAdded(handler);

    /// <summary>
    /// Registers a handler to be called when a component of type <typeparamref name="T"/>
    /// is removed from an entity.
    /// </summary>
    /// <typeparam name="T">The component type to watch for removals.</typeparam>
    /// <param name="handler">The handler to invoke when the component is removed.</param>
    /// <returns>
    /// An <see cref="EventSubscription"/> that can be disposed to unsubscribe the handler.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This event fires before the component is fully removed. The handler receives only
    /// the entity, not the component value, because the component data may be in the
    /// process of being overwritten.
    /// </para>
    /// <para>
    /// Component removals occur when calling <see cref="Remove{T}(Entity)"/> or when
    /// despawning an entity.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var subscription = world.OnComponentRemoved&lt;Health&gt;(entity =>
    /// {
    ///     Console.WriteLine($"Entity {entity} lost its Health component");
    /// });
    /// </code>
    /// </example>
    /// <seealso cref="OnComponentAdded{T}(Action{Entity, T})"/>
    public EventSubscription OnComponentRemoved<T>(Action<Entity> handler) where T : struct, IComponent
        => eventManager.OnComponentRemoved<T>(handler);

    /// <summary>
    /// Registers a handler to be called when a component of type <typeparamref name="T"/>
    /// is changed on an entity via <see cref="Set{T}(Entity, in T)"/>.
    /// </summary>
    /// <typeparam name="T">The component type to watch for changes.</typeparam>
    /// <param name="handler">
    /// The handler to invoke when the component is changed. Receives the entity,
    /// the old component value, and the new component value.
    /// </param>
    /// <returns>
    /// An <see cref="EventSubscription"/> that can be disposed to unsubscribe the handler.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This event is only fired when using <see cref="Set{T}(Entity, in T)"/>.
    /// Direct modifications via <see cref="Get{T}(Entity)"/> references do not
    /// trigger this event since there is no way to detect when a reference is modified.
    /// </para>
    /// <para>
    /// This is useful for implementing reactive patterns where systems need to respond
    /// to specific component value changes, such as health dropping to zero.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var subscription = world.OnComponentChanged&lt;Health&gt;((entity, oldHealth, newHealth) =>
    /// {
    ///     if (newHealth.Current &lt;= 0 &amp;&amp; oldHealth.Current &gt; 0)
    ///     {
    ///         Console.WriteLine($"Entity {entity} just died!");
    ///     }
    /// });
    /// </code>
    /// </example>
    /// <seealso cref="OnComponentAdded{T}(Action{Entity, T})"/>
    /// <seealso cref="OnComponentRemoved{T}(Action{Entity})"/>
    public EventSubscription OnComponentChanged<T>(Action<Entity, T, T> handler) where T : struct, IComponent
        => eventManager.OnComponentChanged(handler);

    /// <summary>
    /// Registers a handler to be called when an entity is created.
    /// </summary>
    /// <param name="handler">
    /// The handler to invoke when an entity is created. Receives the entity
    /// and its optional name (null if unnamed).
    /// </param>
    /// <returns>
    /// An <see cref="EventSubscription"/> that can be disposed to unsubscribe the handler.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This event fires after the entity has been fully created and added to the world,
    /// including all initial components from the entity builder.
    /// </para>
    /// <para>
    /// Entity creation events occur when <see cref="EntityBuilder.Build"/> is called.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var subscription = world.OnEntityCreated((entity, name) =>
    /// {
    ///     if (name is not null)
    ///     {
    ///         Console.WriteLine($"Named entity created: {name}");
    ///     }
    ///     else
    ///     {
    ///         Console.WriteLine($"Anonymous entity created: {entity}");
    ///     }
    /// });
    /// </code>
    /// </example>
    /// <seealso cref="OnEntityDestroyed(Action{Entity})"/>
    public EventSubscription OnEntityCreated(Action<Entity, string?> handler)
        => eventManager.OnEntityCreated(handler);

    /// <summary>
    /// Registers a handler to be called when an entity is destroyed.
    /// </summary>
    /// <param name="handler">The handler to invoke when an entity is destroyed.</param>
    /// <returns>
    /// An <see cref="EventSubscription"/> that can be disposed to unsubscribe the handler.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This event fires at the start of the despawn process, before the entity is removed
    /// from the world. The entity handle is still valid during the callback and can be
    /// used to query components.
    /// </para>
    /// <para>
    /// Entity destruction events occur when <see cref="Despawn(Entity)"/> or
    /// <see cref="DespawnRecursive(Entity)"/> is called.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var subscription = world.OnEntityDestroyed(entity =>
    /// {
    ///     var name = world.GetName(entity);
    ///     Console.WriteLine($"Entity destroyed: {name ?? entity.ToString()}");
    /// });
    /// </code>
    /// </example>
    /// <seealso cref="OnEntityCreated(Action{Entity, string?})"/>
    public EventSubscription OnEntityDestroyed(Action<Entity> handler)
        => eventManager.OnEntityDestroyed(handler);

    #endregion

    #region Messaging

    /// <summary>
    /// Sends a message immediately to all subscribed handlers.
    /// </summary>
    /// <typeparam name="T">The message type to send.</typeparam>
    /// <param name="message">The message data to send.</param>
    /// <remarks>
    /// <para>
    /// Messages are dispatched synchronously to all handlers in registration order.
    /// If no handlers are subscribed for the message type, this method returns
    /// immediately with minimal overhead (a dictionary lookup that returns false).
    /// </para>
    /// <para>
    /// Use struct types for messages to minimize allocations. If a handler throws
    /// an exception, it will propagate to the caller and subsequent handlers will
    /// not be invoked.
    /// </para>
    /// <para>
    /// For deferred message delivery, use <see cref="QueueMessage{T}(in T)"/> instead.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Define a message type
    /// public readonly record struct DamageMessage(Entity Target, int Amount, Entity Source);
    ///
    /// // Subscribe to the message in a system
    /// var subscription = world.Subscribe&lt;DamageMessage&gt;(msg =>
    /// {
    ///     ref var health = ref world.Get&lt;Health&gt;(msg.Target);
    ///     health.Current -= msg.Amount;
    /// });
    ///
    /// // Send a message from another system
    /// world.Send(new DamageMessage(target, 25, attacker));
    /// </code>
    /// </example>
    /// <seealso cref="Subscribe{T}(Action{T})"/>
    /// <seealso cref="QueueMessage{T}(in T)"/>
    /// <seealso cref="HasMessageSubscribers{T}()"/>
    public void Send<T>(in T message)
        => messageManager.Send(in message);

    /// <summary>
    /// Subscribes a handler to messages of the specified type.
    /// </summary>
    /// <typeparam name="T">The message type to subscribe to.</typeparam>
    /// <param name="handler">The handler to invoke when messages of type <typeparamref name="T"/> are sent.</param>
    /// <returns>
    /// An <see cref="EventSubscription"/> that can be disposed to unsubscribe the handler.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="handler"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// Handlers are invoked synchronously in registration order when messages are sent.
    /// The same handler instance can be registered multiple times, in which case it will
    /// be invoked multiple times per message. Each registration returns a separate subscription.
    /// </para>
    /// <para>
    /// To unsubscribe, call <see cref="EventSubscription.Dispose"/> on the returned subscription.
    /// Subscriptions are idempotent: disposing the same subscription multiple times has no
    /// additional effect.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Subscribe to damage messages
    /// var subscription = world.Subscribe&lt;DamageMessage&gt;(msg =>
    /// {
    ///     Console.WriteLine($"Entity {msg.Target} took {msg.Amount} damage");
    /// });
    ///
    /// // Later, unsubscribe
    /// subscription.Dispose();
    /// </code>
    /// </example>
    /// <seealso cref="Send{T}(in T)"/>
    /// <seealso cref="HasMessageSubscribers{T}()"/>
    public EventSubscription Subscribe<T>(Action<T> handler)
        => messageManager.Subscribe(handler);

    /// <summary>
    /// Checks if there are any handlers subscribed to a specific message type.
    /// </summary>
    /// <typeparam name="T">The message type to check.</typeparam>
    /// <returns><c>true</c> if at least one handler is subscribed; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <para>
    /// This can be used to skip expensive message creation when no handlers are listening.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Skip creating expensive message data if no one is listening
    /// if (world.HasMessageSubscribers&lt;ExpensiveMessage&gt;())
    /// {
    ///     var messageData = CreateExpensiveMessageData();
    ///     world.Send(messageData);
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="Subscribe{T}(Action{T})"/>
    /// <seealso cref="GetMessageSubscriberCount{T}()"/>
    public bool HasMessageSubscribers<T>()
        => messageManager.HasSubscribers<T>();

    /// <summary>
    /// Gets the number of handlers subscribed to a specific message type.
    /// </summary>
    /// <typeparam name="T">The message type to check.</typeparam>
    /// <returns>The number of subscribed handlers for the message type.</returns>
    /// <remarks>
    /// This method is primarily useful for testing and debugging.
    /// </remarks>
    public int GetMessageSubscriberCount<T>()
        => messageManager.GetSubscriberCount<T>();

    /// <summary>
    /// Queues a message for deferred delivery.
    /// </summary>
    /// <typeparam name="T">The message type to queue.</typeparam>
    /// <param name="message">The message data to queue.</param>
    /// <remarks>
    /// <para>
    /// Queued messages are stored and delivered when <see cref="ProcessQueuedMessages"/> is called.
    /// This is useful when you want to collect messages during a system's update and process
    /// them all at once, or when you want to ensure messages are processed at a specific
    /// point in the update cycle.
    /// </para>
    /// <para>
    /// Messages are processed in FIFO order (first-in, first-out) within each message type.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Queue messages during system updates
    /// foreach (var entity in world.Query&lt;Collision&gt;())
    /// {
    ///     world.QueueMessage(new CollisionMessage(entity, other));
    /// }
    ///
    /// // Process all queued messages at a specific point
    /// world.ProcessQueuedMessages();
    /// </code>
    /// </example>
    /// <seealso cref="ProcessQueuedMessages"/>
    /// <seealso cref="ProcessQueuedMessages{T}"/>
    /// <seealso cref="GetQueuedMessageCount{T}()"/>
    public void QueueMessage<T>(in T message)
        => messageManager.Queue(in message);

    /// <summary>
    /// Processes all queued messages, delivering them to subscribed handlers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Messages are processed in FIFO order within each message type. The order in which
    /// different message types are processed is not guaranteed.
    /// </para>
    /// <para>
    /// After processing, all message queues are cleared. If a handler throws an exception,
    /// the exception propagates and remaining messages may not be processed.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In your game loop, process queued messages at a specific point
    /// world.Update(deltaTime);
    /// world.ProcessQueuedMessages(); // Process all messages queued during Update
    /// </code>
    /// </example>
    /// <seealso cref="QueueMessage{T}(in T)"/>
    /// <seealso cref="ProcessQueuedMessages{T}"/>
    public void ProcessQueuedMessages()
        => messageManager.ProcessQueuedMessages();

    /// <summary>
    /// Processes all queued messages of a specific type.
    /// </summary>
    /// <typeparam name="T">The message type to process.</typeparam>
    /// <remarks>
    /// <para>
    /// Only processes messages of the specified type. Other queued messages remain queued.
    /// This is useful when you need fine-grained control over when specific message types
    /// are processed.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Process only damage messages at this point
    /// world.ProcessQueuedMessages&lt;DamageMessage&gt;();
    ///
    /// // Physics messages are processed later
    /// world.ProcessQueuedMessages&lt;CollisionMessage&gt;();
    /// </code>
    /// </example>
    /// <seealso cref="QueueMessage{T}(in T)"/>
    /// <seealso cref="ProcessQueuedMessages"/>
    public void ProcessQueuedMessages<T>()
        => messageManager.ProcessQueuedMessages<T>();

    /// <summary>
    /// Gets the count of queued messages for a specific type.
    /// </summary>
    /// <typeparam name="T">The message type to check.</typeparam>
    /// <returns>The number of queued messages of the specified type.</returns>
    /// <remarks>
    /// This method is primarily useful for testing and debugging.
    /// </remarks>
    public int GetQueuedMessageCount<T>()
        => messageManager.GetQueuedMessageCount<T>();

    /// <summary>
    /// Gets the total count of all queued messages across all types.
    /// </summary>
    /// <returns>The total number of queued messages.</returns>
    /// <remarks>
    /// This method is primarily useful for testing and debugging.
    /// </remarks>
    public int GetTotalQueuedMessageCount()
        => messageManager.GetTotalQueuedMessageCount();

    /// <summary>
    /// Clears all queued messages without processing them.
    /// </summary>
    /// <remarks>
    /// Use this to discard pending messages when they are no longer relevant,
    /// such as when resetting game state or changing levels.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Clear all pending messages when resetting level
    /// world.ClearQueuedMessages();
    /// </code>
    /// </example>
    /// <seealso cref="ClearQueuedMessages{T}"/>
    public void ClearQueuedMessages()
        => messageManager.ClearQueuedMessages();

    /// <summary>
    /// Clears queued messages of a specific type without processing them.
    /// </summary>
    /// <typeparam name="T">The message type to clear.</typeparam>
    /// <example>
    /// <code>
    /// // Discard all pending collision messages
    /// world.ClearQueuedMessages&lt;CollisionMessage&gt;();
    /// </code>
    /// </example>
    /// <seealso cref="ClearQueuedMessages"/>
    public void ClearQueuedMessages<T>()
        => messageManager.ClearQueuedMessages<T>();

    #endregion

    #region Change Tracking

    /// <summary>
    /// Manually marks an entity as dirty (modified) for a specific component type.
    /// </summary>
    /// <typeparam name="T">The component type to mark as dirty.</typeparam>
    /// <param name="entity">The entity to mark as dirty.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the entity is not alive.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Use this method to flag entities for change-based processing when modifying
    /// components via <see cref="Get{T}(Entity)"/> references. Modifications through
    /// <see cref="Set{T}(Entity, in T)"/> can be automatically tracked if
    /// <see cref="EnableAutoTracking{T}()"/> has been called.
    /// </para>
    /// <para>
    /// This method is idempotent: marking an already-dirty entity has no additional effect.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Modify a component via Get and manually mark dirty
    /// ref var position = ref world.Get&lt;Position&gt;(entity);
    /// position.X += 10;
    /// world.MarkDirty&lt;Position&gt;(entity);
    ///
    /// // Later, process dirty entities
    /// foreach (var dirtyEntity in world.GetDirtyEntities&lt;Position&gt;())
    /// {
    ///     SyncToNetwork(dirtyEntity);
    /// }
    /// world.ClearDirtyFlags&lt;Position&gt;();
    /// </code>
    /// </example>
    /// <seealso cref="GetDirtyEntities{T}()"/>
    /// <seealso cref="ClearDirtyFlags{T}()"/>
    /// <seealso cref="EnableAutoTracking{T}()"/>
    public void MarkDirty<T>(Entity entity) where T : struct, IComponent
        => changeTracker.MarkDirty<T>(entity, IsAlive);

    /// <summary>
    /// Gets all entities that have been marked dirty for a specific component type.
    /// </summary>
    /// <typeparam name="T">The component type to query.</typeparam>
    /// <returns>
    /// An enumerable of entities marked dirty for the component type.
    /// Returns an empty sequence if no entities are dirty.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The returned entities are verified to be alive. Any entities that were marked dirty
    /// but have since been despawned are not included.
    /// </para>
    /// <para>
    /// This method iterates through all dirty entity IDs and reconstructs full entity handles.
    /// For large numbers of dirty entities, consider caching the results if you need to
    /// iterate multiple times.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Process all entities with modified Position components
    /// foreach (var entity in world.GetDirtyEntities&lt;Position&gt;())
    /// {
    ///     ref readonly var pos = ref world.Get&lt;Position&gt;(entity);
    ///     SyncPositionToNetwork(entity, pos);
    /// }
    ///
    /// // Clear after processing
    /// world.ClearDirtyFlags&lt;Position&gt;();
    /// </code>
    /// </example>
    /// <seealso cref="MarkDirty{T}(Entity)"/>
    /// <seealso cref="ClearDirtyFlags{T}()"/>
    public IEnumerable<Entity> GetDirtyEntities<T>() where T : struct, IComponent
        => changeTracker.GetDirtyEntities<T>(IsAlive);

    /// <summary>
    /// Clears all dirty flags for a specific component type.
    /// </summary>
    /// <typeparam name="T">The component type to clear flags for.</typeparam>
    /// <remarks>
    /// <para>
    /// Call this method after processing dirty entities to reset the tracking state.
    /// Typically done at the end of a frame or after synchronization completes.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Process and clear in one pass
    /// foreach (var entity in world.GetDirtyEntities&lt;Position&gt;())
    /// {
    ///     ProcessEntity(entity);
    /// }
    /// world.ClearDirtyFlags&lt;Position&gt;();
    /// </code>
    /// </example>
    /// <seealso cref="MarkDirty{T}(Entity)"/>
    /// <seealso cref="GetDirtyEntities{T}()"/>
    public void ClearDirtyFlags<T>() where T : struct, IComponent
    {
        changeTracker.ClearDirtyFlags<T>();
    }

    /// <summary>
    /// Checks if an entity is marked dirty for a specific component type.
    /// </summary>
    /// <typeparam name="T">The component type to check.</typeparam>
    /// <param name="entity">The entity to check.</param>
    /// <returns>
    /// <c>true</c> if the entity is marked dirty for the component type;
    /// <c>false</c> if not dirty or the entity is not alive.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is safe to call with stale entity handles. A stale handle refers to
    /// an entity that has been destroyed (via <see cref="Despawn"/>). In such cases,
    /// this method returns <c>false</c> rather than throwing an exception.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// if (world.IsDirty&lt;Position&gt;(entity))
    /// {
    ///     // Entity's position has been modified since last clear
    ///     SyncPosition(entity);
    /// }
    /// </code>
    /// </example>
    public bool IsDirty<T>(Entity entity) where T : struct, IComponent
        => changeTracker.IsDirty<T>(entity, IsAlive);

    /// <summary>
    /// Enables automatic dirty tracking for a component type.
    /// </summary>
    /// <typeparam name="T">The component type to enable auto-tracking for.</typeparam>
    /// <remarks>
    /// <para>
    /// When auto-tracking is enabled, entities are automatically marked dirty when
    /// the component is modified via <see cref="Set{T}(Entity, in T)"/>. This saves
    /// the need to manually call <see cref="MarkDirty{T}(Entity)"/> after each Set.
    /// </para>
    /// <para>
    /// <strong>Important:</strong> Direct modifications via <see cref="Get{T}(Entity)"/>
    /// references are not automatically tracked, as there is no way to detect when
    /// a reference is modified. Use <see cref="MarkDirty{T}(Entity)"/> manually in those cases.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Enable auto-tracking for Position
    /// world.EnableAutoTracking&lt;Position&gt;();
    ///
    /// // Now Set() calls automatically mark entities dirty
    /// world.Set(entity, new Position { X = 100, Y = 200 });
    ///
    /// // The entity is now dirty without manual marking
    /// Debug.Assert(world.IsDirty&lt;Position&gt;(entity));
    /// </code>
    /// </example>
    /// <seealso cref="DisableAutoTracking{T}()"/>
    /// <seealso cref="IsAutoTrackingEnabled{T}()"/>
    public void EnableAutoTracking<T>() where T : struct, IComponent
    {
        changeTracker.EnableAutoTracking<T>();
    }

    /// <summary>
    /// Disables automatic dirty tracking for a component type.
    /// </summary>
    /// <typeparam name="T">The component type to disable auto-tracking for.</typeparam>
    /// <seealso cref="EnableAutoTracking{T}()"/>
    public void DisableAutoTracking<T>() where T : struct, IComponent
    {
        changeTracker.DisableAutoTracking<T>();
    }

    /// <summary>
    /// Checks if automatic dirty tracking is enabled for a component type.
    /// </summary>
    /// <typeparam name="T">The component type to check.</typeparam>
    /// <returns>
    /// <c>true</c> if auto-tracking is enabled; <c>false</c> otherwise.
    /// </returns>
    public bool IsAutoTrackingEnabled<T>() where T : struct, IComponent
    {
        return changeTracker.IsAutoTrackingEnabled<T>();
    }

    /// <summary>
    /// Gets the count of dirty entities for a specific component type.
    /// </summary>
    /// <typeparam name="T">The component type to count.</typeparam>
    /// <returns>The number of entities marked dirty for this component type.</returns>
    /// <remarks>
    /// <para>
    /// This count may include entities that have been despawned since being marked dirty.
    /// Use <see cref="GetDirtyEntities{T}()"/> to iterate only live entities.
    /// </para>
    /// </remarks>
    public int GetDirtyCount<T>() where T : struct, IComponent
    {
        return changeTracker.GetDirtyCount<T>();
    }

    /// <summary>
    /// Clears all dirty flags for all component types.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this to reset all tracking state at once, typically at the end of a frame
    /// after all change-dependent systems have run.
    /// </para>
    /// </remarks>
    public void ClearAllDirtyFlags()
    {
        changeTracker.ClearAll();
    }

    #endregion

    /// <inheritdoc />
    public void Dispose()
    {
        // Uninstall all plugins first (this disposes plugin-registered systems)
        pluginManager.UninstallAll();

        // Dispose remaining systems (not registered by plugins)
        systemManager.DisposeAll();
        archetypeManager.Dispose();
        entityPool.Clear();
        entityNamingManager.Clear();
        hierarchyManager.Clear();
        singletonManager.Clear();
        extensionManager.Clear();
        eventManager.Clear();
        messageManager.Clear();
        changeTracker.Clear();
        tagManager.Clear();
    }

}
