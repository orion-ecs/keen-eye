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
    private readonly EntityBuilder builder;
    private readonly List<ISystem> systems = [];

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
    /// Creates an entity directly with the specified components.
    /// </summary>
    internal Entity CreateEntity(List<(ComponentInfo Info, object Data)> components)
    {
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
    }
}
