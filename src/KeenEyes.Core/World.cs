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
    /// Checks if an entity has a component.
    /// </summary>
    public bool Has<T>(Entity entity) where T : struct, IComponent
    {
        if (!entityComponents.TryGetValue(entity.Id, out var components))
        {
            return false;
        }

        var info = Components.Get<T>();
        return info is not null && components.ContainsKey(info.Id);
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
