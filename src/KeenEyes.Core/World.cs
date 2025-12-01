using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace KeenEyes;

/// <summary>
/// The world is the container for all entities and their components.
/// Each world is completely isolated with its own component registry.
/// </summary>
public sealed class World : IDisposable
{
    private int nextEntityId;
    private readonly Dictionary<int, int> entityVersions = [];
    private readonly Dictionary<int, HashSet<Type>> entityComponentTypes = [];
    private readonly Dictionary<int, string?> entityNames = [];

    /// <summary>
    /// Per-type component storage. Each value is a Dictionary&lt;int, T&gt; for that component type.
    /// This enables typed access and ref returns via CollectionsMarshal.
    /// </summary>
    private readonly Dictionary<Type, object> componentStorage = [];

    private readonly EntityBuilder builder;
    private readonly List<ISystem> systems = [];

    /// <summary>
    /// Singleton storage. Each value is the singleton instance for that type.
    /// </summary>
    private readonly Dictionary<Type, object> singletons = [];

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
    /// Begins building a new entity with the specified name.
    /// </summary>
    /// <param name="name">The name to assign to the entity.</param>
    /// <returns>A fluent builder for adding components.</returns>
    public EntityBuilder Spawn(string name)
    {
        builder.Reset(name);
        return builder;
    }

    /// <summary>
    /// Creates an entity directly with the specified components.
    /// </summary>
    internal Entity CreateEntity(List<(ComponentInfo Info, object Data)> components, string? name = null)
    {
        var id = Interlocked.Increment(ref nextEntityId) - 1;
        var version = 1;

        entityVersions[id] = version;
        entityComponentTypes[id] = [];
        entityNames[id] = name;

        foreach (var (info, data) in components)
        {
            AddComponentInternal(id, info.Type, data);
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

        // Remove all components from typed storage
        if (entityComponentTypes.TryGetValue(entity.Id, out var types))
        {
            foreach (var type in types)
            {
                RemoveComponentInternal(entity.Id, type);
            }
        }

        entityVersions[entity.Id]++;
        entityComponentTypes.Remove(entity.Id);
        entityNames.Remove(entity.Id);
        return true;
    }

    /// <summary>
    /// Checks if an entity is still alive.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <returns>True if the entity exists and has the correct version.</returns>
    public bool IsAlive(Entity entity)
    {
        return entityVersions.TryGetValue(entity.Id, out var version) && version == entity.Version;
    }

    /// <summary>
    /// Gets the name of an entity, if one was assigned.
    /// </summary>
    /// <param name="entity">The entity to get the name for.</param>
    /// <returns>The entity's name, or null if not named.</returns>
    public string? GetName(Entity entity)
    {
        if (!IsAlive(entity))
        {
            return null;
        }

        return entityNames.TryGetValue(entity.Id, out var name) ? name : null;
    }

    /// <summary>
    /// Sets the name of an entity.
    /// </summary>
    /// <param name="entity">The entity to name.</param>
    /// <param name="name">The name to assign, or null to remove the name.</param>
    /// <returns>True if the entity was named, false if the entity doesn't exist.</returns>
    public bool SetName(Entity entity, string? name)
    {
        if (!IsAlive(entity))
        {
            return false;
        }

        entityNames[entity.Id] = name;
        return true;
    }

    /// <summary>
    /// Gets all entities currently alive in this world.
    /// </summary>
    public IEnumerable<Entity> GetAllEntities()
    {
        foreach (var (id, version) in entityVersions)
        {
            if (entityComponentTypes.ContainsKey(id))
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

    #region Component Operations

    /// <summary>
    /// Gets a component from an entity.
    /// </summary>
    /// <typeparam name="T">The component type to retrieve.</typeparam>
    /// <param name="entity">The entity to get the component from.</param>
    /// <returns>A reference to the component data.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the entity is not alive.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the entity does not have the component.</exception>
    public ref T Get<T>(Entity entity) where T : struct, IComponent
    {
        if (!IsAlive(entity))
        {
            throw new InvalidOperationException($"Entity {entity} is not alive.");
        }

        var storage = GetOrCreateStorage<T>();
        ref var component = ref CollectionsMarshal.GetValueRefOrNullRef(storage, entity.Id);

        if (Unsafe.IsNullRef(ref component))
        {
            throw new KeyNotFoundException($"Entity {entity} does not have component {typeof(T).Name}.");
        }

        return ref component;
    }

    /// <summary>
    /// Tries to get a component from an entity.
    /// </summary>
    /// <typeparam name="T">The component type to retrieve.</typeparam>
    /// <param name="entity">The entity to get the component from.</param>
    /// <param name="component">The component data if found.</param>
    /// <returns>True if the component was found, false otherwise.</returns>
    public bool TryGet<T>(Entity entity, out T component) where T : struct, IComponent
    {
        component = default;

        if (!IsAlive(entity))
        {
            return false;
        }

        if (!componentStorage.TryGetValue(typeof(T), out var storage))
        {
            return false;
        }

        var typedStorage = (Dictionary<int, T>)storage;
        return typedStorage.TryGetValue(entity.Id, out component);
    }

    /// <summary>
    /// Sets a component on an entity, replacing any existing value.
    /// </summary>
    /// <typeparam name="T">The component type to set.</typeparam>
    /// <param name="entity">The entity to set the component on.</param>
    /// <param name="component">The component data to set.</param>
    /// <exception cref="InvalidOperationException">Thrown when the entity is not alive.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the entity does not have the component.</exception>
    public void Set<T>(Entity entity, T component) where T : struct, IComponent
    {
        if (!IsAlive(entity))
        {
            throw new InvalidOperationException($"Entity {entity} is not alive.");
        }

        if (!Has<T>(entity))
        {
            throw new KeyNotFoundException($"Entity {entity} does not have component {typeof(T).Name}. Use Add<T>() to add a new component.");
        }

        var storage = GetOrCreateStorage<T>();
        storage[entity.Id] = component;
    }

    /// <summary>
    /// Adds a component to an entity.
    /// </summary>
    /// <typeparam name="T">The component type to add.</typeparam>
    /// <param name="entity">The entity to add the component to.</param>
    /// <param name="component">The component data to add.</param>
    /// <exception cref="InvalidOperationException">Thrown when the entity is not alive or already has the component.</exception>
    public void Add<T>(Entity entity, T component) where T : struct, IComponent
    {
        if (!IsAlive(entity))
        {
            throw new InvalidOperationException($"Entity {entity} is not alive.");
        }

        if (Has<T>(entity))
        {
            throw new InvalidOperationException($"Entity {entity} already has component {typeof(T).Name}. Use Set<T>() to update an existing component.");
        }

        var type = typeof(T);
        Components.GetOrRegister<T>();
        AddComponentInternal(entity.Id, type, component);
        entityComponentTypes[entity.Id].Add(type);
    }

    /// <summary>
    /// Adds a tag component to an entity.
    /// </summary>
    /// <typeparam name="T">The tag component type to add.</typeparam>
    /// <param name="entity">The entity to add the tag to.</param>
    /// <exception cref="InvalidOperationException">Thrown when the entity is not alive or already has the tag.</exception>
    public void AddTag<T>(Entity entity) where T : struct, ITagComponent
    {
        if (!IsAlive(entity))
        {
            throw new InvalidOperationException($"Entity {entity} is not alive.");
        }

        if (Has<T>(entity))
        {
            throw new InvalidOperationException($"Entity {entity} already has tag {typeof(T).Name}.");
        }

        var type = typeof(T);
        Components.GetOrRegister<T>(isTag: true);
        AddComponentInternal(entity.Id, type, default(T)!);
        entityComponentTypes[entity.Id].Add(type);
    }

    /// <summary>
    /// Removes a component from an entity.
    /// </summary>
    /// <typeparam name="T">The component type to remove.</typeparam>
    /// <param name="entity">The entity to remove the component from.</param>
    /// <returns>True if the component was removed, false if the entity didn't have it.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the entity is not alive.</exception>
    public bool Remove<T>(Entity entity) where T : struct, IComponent
    {
        if (!IsAlive(entity))
        {
            throw new InvalidOperationException($"Entity {entity} is not alive.");
        }

        var type = typeof(T);
        if (!entityComponentTypes.TryGetValue(entity.Id, out var types) || !types.Contains(type))
        {
            return false;
        }

        RemoveComponentInternal(entity.Id, type);
        types.Remove(type);
        return true;
    }

    /// <summary>
    /// Checks if an entity has a component.
    /// </summary>
    /// <typeparam name="T">The component type to check.</typeparam>
    /// <param name="entity">The entity to check.</param>
    /// <returns>True if the entity has the component, false otherwise.</returns>
    public bool Has<T>(Entity entity) where T : struct, IComponent
    {
        if (!entityComponentTypes.TryGetValue(entity.Id, out var types))
        {
            return false;
        }

        return types.Contains(typeof(T));
    }

    /// <summary>
    /// Gets all component types on an entity.
    /// </summary>
    /// <param name="entity">The entity to get components for.</param>
    /// <returns>A read-only collection of component types.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the entity is not alive.</exception>
    public IReadOnlyCollection<Type> GetComponents(Entity entity)
    {
        if (!IsAlive(entity))
        {
            throw new InvalidOperationException($"Entity {entity} is not alive.");
        }

        if (!entityComponentTypes.TryGetValue(entity.Id, out var types))
        {
            return Array.Empty<Type>();
        }

        return types;
    }

    /// <summary>
    /// Gets or creates the typed storage dictionary for a component type.
    /// </summary>
    private Dictionary<int, T> GetOrCreateStorage<T>() where T : struct
    {
        var type = typeof(T);
        if (!componentStorage.TryGetValue(type, out var storage))
        {
            storage = new Dictionary<int, T>();
            componentStorage[type] = storage;
        }
        return (Dictionary<int, T>)storage;
    }

    /// <summary>
    /// Adds a component to the typed storage.
    /// </summary>
    private void AddComponentInternal(int entityId, Type type, object data)
    {
        if (!componentStorage.TryGetValue(type, out var storage))
        {
            // Create a typed dictionary using reflection
            var dictType = typeof(Dictionary<,>).MakeGenericType(typeof(int), type);
            storage = Activator.CreateInstance(dictType)!;
            componentStorage[type] = storage;
        }

        // Use reflection to add the component (necessary for dynamic type)
        var method = storage.GetType().GetMethod("set_Item")!;
        method.Invoke(storage, [entityId, data]);
    }

    /// <summary>
    /// Removes a component from the typed storage.
    /// </summary>
    private void RemoveComponentInternal(int entityId, Type type)
    {
        if (!componentStorage.TryGetValue(type, out var storage))
        {
            return;
        }

        // Use reflection to remove the component (necessary for dynamic type)
        var method = storage.GetType().GetMethod("Remove", [typeof(int)])!;
        method.Invoke(storage, [entityId]);
    }

    #endregion

    #region Singletons

    /// <summary>
    /// Sets a singleton value for this world.
    /// Singletons are world-level data not tied to any entity.
    /// </summary>
    /// <typeparam name="T">The singleton type.</typeparam>
    /// <param name="value">The singleton value.</param>
    public void SetSingleton<T>(T value) where T : notnull
    {
        singletons[typeof(T)] = value;
    }

    /// <summary>
    /// Gets a singleton value from this world.
    /// </summary>
    /// <typeparam name="T">The singleton type.</typeparam>
    /// <returns>The singleton value.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the singleton doesn't exist.</exception>
    public T GetSingleton<T>() where T : notnull
    {
        if (!singletons.TryGetValue(typeof(T), out var value))
        {
            throw new KeyNotFoundException($"Singleton of type {typeof(T).Name} not found.");
        }
        return (T)value;
    }

    /// <summary>
    /// Tries to get a singleton value from this world.
    /// </summary>
    /// <typeparam name="T">The singleton type.</typeparam>
    /// <param name="value">The singleton value if found.</param>
    /// <returns>True if the singleton was found, false otherwise.</returns>
    public bool TryGetSingleton<T>(out T? value) where T : notnull
    {
        if (singletons.TryGetValue(typeof(T), out var obj))
        {
            value = (T)obj;
            return true;
        }
        value = default;
        return false;
    }

    /// <summary>
    /// Checks if this world has a singleton.
    /// </summary>
    /// <typeparam name="T">The singleton type.</typeparam>
    /// <returns>True if the singleton exists, false otherwise.</returns>
    public bool HasSingleton<T>() where T : notnull
    {
        return singletons.ContainsKey(typeof(T));
    }

    /// <summary>
    /// Removes a singleton from this world.
    /// </summary>
    /// <typeparam name="T">The singleton type.</typeparam>
    /// <returns>True if the singleton was removed, false if it didn't exist.</returns>
    public bool RemoveSingleton<T>() where T : notnull
    {
        return singletons.Remove(typeof(T));
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

    #region Command Buffers

    /// <summary>
    /// Creates a new command buffer for this world.
    /// </summary>
    /// <returns>A new command buffer.</returns>
    public CommandBuffer CreateCommandBuffer()
    {
        return new CommandBuffer(this);
    }

    /// <summary>
    /// Executes all commands in a command buffer and clears it.
    /// </summary>
    /// <param name="buffer">The command buffer to flush.</param>
    public void Flush(CommandBuffer buffer)
    {
        buffer.Execute();
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
        entityComponentTypes.Clear();
        entityNames.Clear();
        componentStorage.Clear();
        singletons.Clear();
    }
}
