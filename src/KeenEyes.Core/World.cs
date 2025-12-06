using System.Runtime.CompilerServices;

namespace KeenEyes;

/// <summary>
/// The world is the container for all entities and their components.
/// Each world is completely isolated with its own component registry.
/// </summary>
public sealed class World : IDisposable
{
    private int nextEntityId;
    private readonly Dictionary<int, int> entityVersions = [];
    private readonly Dictionary<int, Dictionary<ComponentId, object>> entityComponents = [];
    private readonly Dictionary<int, HashSet<Type>> entityComponentTypes = [];
    private readonly Dictionary<Type, object> singletons = [];
    private readonly EntityBuilder builder;
    private readonly List<ISystem> systems = [];

    // Entity naming support - bidirectional mapping for O(1) lookups
    private readonly Dictionary<int, string> entityNames = [];
    private readonly Dictionary<string, int> namesToEntityIds = [];

    /// <summary>
    /// The component registry for this world.
    /// Component IDs are unique per-world, not global.
    /// </summary>
    public ComponentRegistry Components { get; } = new();

    /// <summary>
    /// Creates a new ECS world.
    /// </summary>
    public World()
    {
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
        if (name is not null && namesToEntityIds.ContainsKey(name))
        {
            throw new ArgumentException(
                $"An entity with the name '{name}' already exists in this world.", nameof(name));
        }

        var id = Interlocked.Increment(ref nextEntityId) - 1;
        var version = 1;

        entityVersions[id] = version;
        entityComponents[id] = [];
        entityComponentTypes[id] = [];

        foreach (var (info, data) in components)
        {
            entityComponents[id][info.Id] = data;
            entityComponentTypes[id].Add(info.Type);
        }

        // Register the entity name if provided
        if (name is not null)
        {
            entityNames[id] = name;
            namesToEntityIds[name] = id;
        }

        return new Entity(id, version);
    }

    /// <summary>
    /// Destroys an entity and all its components.
    /// </summary>
    /// <param name="entity">The entity to destroy.</param>
    /// <returns>True if the entity was destroyed, false if it didn't exist.</returns>
    public bool Despawn(Entity entity)
    {
        if (!IsAlive(entity))
        {
            return false;
        }

        entityVersions[entity.Id]++;
        entityComponents.Remove(entity.Id);
        entityComponentTypes.Remove(entity.Id);

        // Clean up entity name mappings if the entity had a name
        if (entityNames.TryGetValue(entity.Id, out var name))
        {
            entityNames.Remove(entity.Id);
            namesToEntityIds.Remove(name);
        }

        return true;
    }

    /// <summary>
    /// Checks if an entity is still alive.
    /// </summary>
    public bool IsAlive(Entity entity)
    {
        return entityVersions.TryGetValue(entity.Id, out var version) && version == entity.Version;
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

        if (!entityComponents.TryGetValue(entity.Id, out var components) ||
            !components.TryGetValue(info.Id, out var boxed))
        {
            throw new InvalidOperationException(
                $"Entity {entity} does not have component {typeof(T).Name}.");
        }

        return ref Unsafe.Unbox<T>(boxed);
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
    /// This operation is O(1) for the dictionary-based storage implementation.
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

        return entityComponents.TryGetValue(entity.Id, out var components) &&
               components.ContainsKey(info.Id);
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
    /// After adding a component, the entity will be matched by queries that require that component type.
    /// This operation is O(1) for the dictionary-based storage in Phase 1.
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

        var info = Components.GetOrRegister<T>();

        if (!entityComponents.TryGetValue(entity.Id, out var components))
        {
            throw new InvalidOperationException($"Entity {entity} is not alive.");
        }

        if (components.ContainsKey(info.Id))
        {
            throw new InvalidOperationException(
                $"Entity {entity} already has component {typeof(T).Name}. Use Set<T>() to update existing components.");
        }

        components[info.Id] = component;
        entityComponentTypes[entity.Id].Add(typeof(T));
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

        if (!entityComponents.TryGetValue(entity.Id, out var components) ||
            !components.ContainsKey(info.Id))
        {
            throw new InvalidOperationException(
                $"Entity {entity} does not have component {typeof(T).Name}. Use Add<T>() to add new components.");
        }

        components[info.Id] = value;
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
    /// require that component type. This operation is O(1) for the dictionary-based storage.
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

        // Check if entity has the component and remove it
        if (!entityComponents.TryGetValue(entity.Id, out var components) ||
            !components.Remove(info.Id))
        {
            return false;
        }

        // Remove from type tracking for query matching
        entityComponentTypes[entity.Id].Remove(typeof(T));

        return true;
    }

    /// <summary>
    /// Gets all entities currently alive in this world.
    /// </summary>
    public IEnumerable<Entity> GetAllEntities()
    {
        foreach (var (id, version) in entityVersions)
        {
            if (entityComponents.ContainsKey(id))
            {
                yield return new Entity(id, version);
            }
        }
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

        return entityNames.TryGetValue(entity.Id, out var name) ? name : null;
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
        if (!namesToEntityIds.TryGetValue(name, out var entityId))
        {
            return Entity.Null;
        }

        // Verify the entity is still alive and return with correct version
        if (!entityVersions.TryGetValue(entityId, out var version) ||
            !entityComponents.ContainsKey(entityId))
        {
            return Entity.Null;
        }

        return new Entity(entityId, version);
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
    /// The complexity is O(C) where C is the number of component types registered in the world,
    /// as it iterates through the entity's component dictionary.
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

        if (!entityComponents.TryGetValue(entity.Id, out var components))
        {
            yield break;
        }

        // Iterate through all components on this entity
        foreach (var (componentId, boxedValue) in components)
        {
            // Get the type information from the component registry
            var info = Components.GetById(componentId);
            if (info is not null)
            {
                yield return (info.Type, boxedValue);
            }
        }
    }

    /// <summary>
    /// Gets all entities matching a query description.
    /// </summary>
    internal IEnumerable<Entity> GetMatchingEntities(QueryDescription description)
    {
        foreach (var entity in GetAllEntities())
        {
            if (entityComponentTypes.TryGetValue(entity.Id, out var types) &&
                description.Matches(types))
            {
                yield return entity;
            }
        }
    }

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
    /// Adds a system to this world.
    /// </summary>
    public World AddSystem<T>() where T : ISystem, new()
    {
        var system = new T();
        system.Initialize(this);
        systems.Add(system);
        return this;
    }

    /// <summary>
    /// Adds a system instance to this world.
    /// </summary>
    public World AddSystem(ISystem system)
    {
        system.Initialize(this);
        systems.Add(system);
        return this;
    }

    /// <summary>
    /// Updates all systems with the given delta time.
    /// </summary>
    public void Update(float deltaTime)
    {
        foreach (var system in systems)
        {
            system.Update(deltaTime);
        }
    }

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
    {
        singletons[typeof(T)] = value;
    }

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
    {
        if (!singletons.TryGetValue(typeof(T), out var boxed))
        {
            throw new InvalidOperationException(
                $"Singleton of type {typeof(T).Name} does not exist in this world. " +
                $"Use SetSingleton<{typeof(T).Name}>() to add it first.");
        }

        return ref Unsafe.Unbox<T>(boxed);
    }

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
    {
        if (singletons.TryGetValue(typeof(T), out var boxed))
        {
            value = (T)boxed;
            return true;
        }

        value = default;
        return false;
    }

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
    {
        return singletons.ContainsKey(typeof(T));
    }

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
    {
        return singletons.Remove(typeof(T));
    }

    #endregion

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var system in systems)
        {
            system.Dispose();
        }
        systems.Clear();
        entityVersions.Clear();
        entityComponents.Clear();
        entityComponentTypes.Clear();
        entityNames.Clear();
        namesToEntityIds.Clear();
        singletons.Clear();
    }
}
