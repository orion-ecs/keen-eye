namespace KeenEyes;

/// <summary>
/// Interface for ECS world operations used by systems and plugins.
/// </summary>
/// <remarks>
/// <para>
/// This interface defines the core operations that systems need to interact with
/// the ECS world. It enables plugin authors to write systems that depend only on
/// the abstractions package rather than the full Core implementation.
/// </para>
/// <para>
/// For advanced operations not covered by this interface, systems can cast to
/// the concrete <c>World</c> type when necessary.
/// </para>
/// </remarks>
public interface IWorld : IDisposable
{
    #region World Identification

    /// <summary>
    /// Unique identifier for this world instance.
    /// </summary>
    /// <remarks>
    /// This identifier is useful for distinguishing between multiple worlds in the same process,
    /// such as in client-server scenarios or multi-scene games.
    /// </remarks>
    Guid Id { get; }

    /// <summary>
    /// Optional name for this world, useful for debugging and logging.
    /// </summary>
    /// <remarks>
    /// When working with multiple worlds, setting meaningful names like "Client", "Server",
    /// or "MainMenu" helps with debugging and tracing issues.
    /// </remarks>
    string? Name { get; set; }

    #endregion

    #region Entity Operations

    /// <summary>
    /// Gets the total number of entities in the world.
    /// </summary>
    int EntityCount { get; }

    /// <summary>
    /// Checks if an entity is alive (not despawned).
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <returns>True if the entity exists and is alive; false otherwise.</returns>
    bool IsAlive(Entity entity);

    /// <summary>
    /// Despawns an entity, removing it and all its components from the world.
    /// </summary>
    /// <param name="entity">The entity to despawn.</param>
    /// <returns>True if the entity was despawned; false if it wasn't alive.</returns>
    bool Despawn(Entity entity);

    /// <summary>
    /// Creates an entity builder for constructing a new entity.
    /// </summary>
    /// <returns>An entity builder for fluent entity construction.</returns>
    IEntityBuilder Spawn();

    /// <summary>
    /// Creates an entity builder for constructing a new named entity.
    /// </summary>
    /// <param name="name">The optional name for the entity. If provided, must be unique within the world.</param>
    /// <returns>An entity builder for fluent entity construction.</returns>
    IEntityBuilder Spawn(string? name);

    /// <summary>
    /// Adds a component to an entity.
    /// </summary>
    /// <typeparam name="T">The component type to add.</typeparam>
    /// <param name="entity">The entity to add the component to.</param>
    /// <param name="component">The component value to add.</param>
    void Add<T>(Entity entity, in T component) where T : struct, IComponent;

    /// <summary>
    /// Sets or replaces a component on an entity.
    /// </summary>
    /// <typeparam name="T">The component type to set.</typeparam>
    /// <param name="entity">The entity to set the component on.</param>
    /// <param name="component">The component value to set.</param>
    /// <remarks>
    /// <para>
    /// If the entity already has the component, it is replaced and <see cref="OnComponentChanged{T}"/>
    /// events are fired. If the entity doesn't have the component, it is added and
    /// <see cref="OnComponentAdded{T}"/> events are fired.
    /// </para>
    /// </remarks>
    void Set<T>(Entity entity, in T component) where T : struct, IComponent;

    #endregion

    #region Component Operations

    /// <summary>
    /// Gets a reference to a component on an entity.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="entity">The entity to get the component from.</param>
    /// <returns>A reference to the component data.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the entity doesn't have the component.</exception>
    ref T Get<T>(Entity entity) where T : struct, IComponent;

    /// <summary>
    /// Checks if an entity has a specific component.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="entity">The entity to check.</param>
    /// <returns>True if the entity has the component; false otherwise.</returns>
    bool Has<T>(Entity entity) where T : struct, IComponent;

    /// <summary>
    /// Removes a component from an entity.
    /// </summary>
    /// <typeparam name="T">The component type to remove.</typeparam>
    /// <param name="entity">The entity to remove the component from.</param>
    /// <returns>True if the component was removed; false if the entity didn't have it.</returns>
    bool Remove<T>(Entity entity) where T : struct, IComponent;

    #endregion

    #region Query Operations

    /// <summary>
    /// Creates a query for entities with a single component type.
    /// </summary>
    /// <typeparam name="T1">The required component type.</typeparam>
    /// <returns>A query builder for filtering and enumerating entities.</returns>
    IQueryBuilder<T1> Query<T1>() where T1 : struct, IComponent;

    /// <summary>
    /// Creates a query for entities with two component types.
    /// </summary>
    /// <typeparam name="T1">The first required component type.</typeparam>
    /// <typeparam name="T2">The second required component type.</typeparam>
    /// <returns>A query builder for filtering and enumerating entities.</returns>
    IQueryBuilder<T1, T2> Query<T1, T2>()
        where T1 : struct, IComponent
        where T2 : struct, IComponent;

    /// <summary>
    /// Creates a query for entities with three component types.
    /// </summary>
    /// <typeparam name="T1">The first required component type.</typeparam>
    /// <typeparam name="T2">The second required component type.</typeparam>
    /// <typeparam name="T3">The third required component type.</typeparam>
    /// <returns>A query builder for filtering and enumerating entities.</returns>
    IQueryBuilder<T1, T2, T3> Query<T1, T2, T3>()
        where T1 : struct, IComponent
        where T2 : struct, IComponent
        where T3 : struct, IComponent;

    /// <summary>
    /// Creates a query for entities with four component types.
    /// </summary>
    /// <typeparam name="T1">The first required component type.</typeparam>
    /// <typeparam name="T2">The second required component type.</typeparam>
    /// <typeparam name="T3">The third required component type.</typeparam>
    /// <typeparam name="T4">The fourth required component type.</typeparam>
    /// <returns>A query builder for filtering and enumerating entities.</returns>
    IQueryBuilder<T1, T2, T3, T4> Query<T1, T2, T3, T4>()
        where T1 : struct, IComponent
        where T2 : struct, IComponent
        where T3 : struct, IComponent
        where T4 : struct, IComponent;

    #endregion

    #region Change Tracking Operations

    /// <summary>
    /// Enables automatic dirty flagtracking for a component type.
    /// </summary>
    /// <typeparam name="T">The component type to track.</typeparam>
    /// <remarks>
    /// <para>
    /// When enabled, any modifications to components of this type will automatically
    /// mark the entity as dirty. Use <see cref="GetDirtyEntities{T}"/> to retrieve
    /// entities with modified components and <see cref="ClearDirtyFlags{T}"/> to
    /// reset the dirty flags after processing.
    /// </para>
    /// </remarks>
    void EnableAutoTracking<T>() where T : struct, IComponent;

    /// <summary>
    /// Disables automatic dirty flag tracking for a component type.
    /// </summary>
    /// <typeparam name="T">The component type to stop tracking.</typeparam>
    void DisableAutoTracking<T>() where T : struct, IComponent;

    /// <summary>
    /// Gets all entities with dirty (modified) components of the specified type.
    /// </summary>
    /// <typeparam name="T">The component type to check for modifications.</typeparam>
    /// <returns>An enumerable of entities with modified components.</returns>
    /// <remarks>
    /// <para>
    /// Entities are marked dirty when their components are modified after
    /// <see cref="EnableAutoTracking{T}"/> is called for that component type.
    /// Use <see cref="ClearDirtyFlags{T}"/> after processing to reset the dirty state.
    /// </para>
    /// </remarks>
    IEnumerable<Entity> GetDirtyEntities<T>() where T : struct, IComponent;

    /// <summary>
    /// Clears dirty flags for all entities with the specified component type.
    /// </summary>
    /// <typeparam name="T">The component type to clear dirty flags for.</typeparam>
    /// <remarks>
    /// <para>
    /// Call this after processing dirty entities with <see cref="GetDirtyEntities{T}"/>
    /// to reset the dirty state for the next frame.
    /// </para>
    /// </remarks>
    void ClearDirtyFlags<T>() where T : struct, IComponent;

    #endregion

    #region Event Operations

    /// <summary>
    /// Subscribes to component added events.
    /// </summary>
    /// <typeparam name="T">The component type to monitor.</typeparam>
    /// <param name="handler">The callback to invoke when a component is added.</param>
    /// <returns>A subscription that can be disposed to unsubscribe.</returns>
    /// <remarks>
    /// <para>
    /// The handler is invoked immediately when a component is added to an entity.
    /// The callback receives the entity and the component value.
    /// </para>
    /// </remarks>
    EventSubscription OnComponentAdded<T>(Action<Entity, T> handler) where T : struct, IComponent;

    /// <summary>
    /// Subscribes to component removed events.
    /// </summary>
    /// <typeparam name="T">The component type to monitor.</typeparam>
    /// <param name="handler">The callback to invoke when a component is removed.</param>
    /// <returns>A subscription that can be disposed to unsubscribe.</returns>
    /// <remarks>
    /// <para>
    /// The handler is invoked immediately after a component is removed from an entity.
    /// The component data is no longer accessible at this point.
    /// </para>
    /// </remarks>
    EventSubscription OnComponentRemoved<T>(Action<Entity> handler) where T : struct, IComponent;

    /// <summary>
    /// Subscribes to component changed events.
    /// </summary>
    /// <typeparam name="T">The component type to monitor.</typeparam>
    /// <param name="handler">The callback to invoke when a component is changed via Set().</param>
    /// <returns>A subscription that can be disposed to unsubscribe.</returns>
    /// <remarks>
    /// <para>
    /// The handler is invoked when a component is changed using <see cref="IWorld.Set{T}"/>.
    /// Direct modifications via <see cref="Get{T}"/> do not trigger this event since
    /// there is no way to detect reference-based mutations.
    /// </para>
    /// <para>
    /// The callback receives the entity, the old component value, and the new component value.
    /// </para>
    /// </remarks>
    EventSubscription OnComponentChanged<T>(Action<Entity, T, T> handler) where T : struct, IComponent;

    #endregion

    #region Entity Lifecycle Events

    /// <summary>
    /// Registers a handler to be called when an entity is created.
    /// </summary>
    /// <param name="handler">
    /// The handler to invoke when an entity is created. Receives the entity and its optional name
    /// (null if unnamed).
    /// </param>
    /// <returns>A subscription that can be disposed to unsubscribe.</returns>
    /// <remarks>
    /// <para>
    /// The handler is invoked after the entity is fully constructed with all its initial components.
    /// </para>
    /// </remarks>
    EventSubscription OnEntityCreated(Action<Entity, string?> handler);

    /// <summary>
    /// Registers a handler to be called when an entity is destroyed.
    /// </summary>
    /// <param name="handler">The handler to invoke when an entity is destroyed.</param>
    /// <returns>A subscription that can be disposed to unsubscribe.</returns>
    /// <remarks>
    /// <para>
    /// The handler is invoked before the entity is removed from the world. The entity and its
    /// components are still accessible at this point for cleanup operations.
    /// </para>
    /// </remarks>
    EventSubscription OnEntityDestroyed(Action<Entity> handler);

    #endregion

    #region Extension Operations

    /// <summary>
    /// Gets an extension registered with the world.
    /// </summary>
    /// <typeparam name="T">The extension type.</typeparam>
    /// <returns>The extension instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the extension is not registered.</exception>
    T GetExtension<T>() where T : class;

    /// <summary>
    /// Tries to get an extension registered with the world.
    /// </summary>
    /// <typeparam name="T">The extension type.</typeparam>
    /// <param name="extension">When this method returns, contains the extension if found.</param>
    /// <returns>True if the extension is registered; false otherwise.</returns>
    bool TryGetExtension<T>(out T? extension) where T : class;

    /// <summary>
    /// Checks if an extension of the specified type is registered.
    /// </summary>
    /// <typeparam name="T">The extension type.</typeparam>
    /// <returns>True if the extension is registered; false otherwise.</returns>
    bool HasExtension<T>() where T : class;

    #endregion

    #region Messaging Operations

    /// <summary>
    /// Sends a message to all subscribers immediately.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="message">The message to send.</param>
    void Send<T>(T message);

    /// <summary>
    /// Subscribes to messages of the specified type.
    /// </summary>
    /// <typeparam name="T">The message type to subscribe to.</typeparam>
    /// <param name="handler">The callback to invoke when a message is received.</param>
    /// <returns>A subscription that can be disposed to unsubscribe.</returns>
    EventSubscription Subscribe<T>(Action<T> handler);

    /// <summary>
    /// Checks if there are any subscribers for the specified message type.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <returns>True if there are subscribers; false otherwise.</returns>
    bool HasMessageSubscribers<T>();

    #endregion

    #region Additional Entity Operations

    /// <summary>
    /// Gets all entities currently alive in this world.
    /// </summary>
    /// <returns>An enumerable of all alive entities.</returns>
    IEnumerable<Entity> GetAllEntities();

    #endregion

    #region Random Number Generation

    /// <summary>
    /// Gets a random integer between 0 (inclusive) and maxValue (exclusive).
    /// </summary>
    /// <param name="maxValue">The exclusive upper bound of the random number to be generated.</param>
    /// <returns>A 32-bit signed integer greater than or equal to 0, and less than maxValue.</returns>
    /// <exception cref="ArgumentOutOfRangeException">maxValue is less than 0.</exception>
    /// <remarks>
    /// <para>
    /// Uses the world's deterministic random number generator. If the world was created with a seed,
    /// this method will produce the same sequence of values across runs.
    /// </para>
    /// </remarks>
    int NextInt(int maxValue);

    /// <summary>
    /// Gets a random integer between minValue (inclusive) and maxValue (exclusive).
    /// </summary>
    /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
    /// <param name="maxValue">The exclusive upper bound of the random number returned.</param>
    /// <returns>
    /// A 32-bit signed integer greater than or equal to minValue and less than maxValue.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// minValue is greater than maxValue.
    /// </exception>
    int NextInt(int minValue, int maxValue);

    /// <summary>
    /// Gets a random floating-point number between 0.0 (inclusive) and 1.0 (exclusive).
    /// </summary>
    /// <returns>A single-precision floating point number greater than or equal to 0.0, and less than 1.0.</returns>
    float NextFloat();

    /// <summary>
    /// Gets a random double-precision floating-point number between 0.0 (inclusive) and 1.0 (exclusive).
    /// </summary>
    /// <returns>A double-precision floating point number greater than or equal to 0.0, and less than 1.0.</returns>
    double NextDouble();

    /// <summary>
    /// Gets a random boolean value.
    /// </summary>
    /// <returns>True or false with equal probability.</returns>
    bool NextBool();

    /// <summary>
    /// Returns a random boolean with the specified probability of being true.
    /// </summary>
    /// <param name="probability">The probability (0.0 to 1.0) that the result will be true.</param>
    /// <returns>True with the specified probability, false otherwise.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// probability is less than 0.0 or greater than 1.0.
    /// </exception>
    bool NextBool(float probability);

    #endregion
}
