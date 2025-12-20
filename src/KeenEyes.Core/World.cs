using KeenEyes.Capabilities;
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
public sealed partial class World : IWorld,
    ISystemHookCapability,
    IPersistenceCapability,
    IHierarchyCapability,
    IValidationCapability,
    ITagCapability,
    IPrefabCapability,
    IStatisticsCapability,
    IInspectionCapability,
    ISerializationCapability
{
    private readonly EntityPool entityPool;
    private readonly ArchetypeManager archetypeManager;
    private readonly QueryManager queryManager;
    private readonly HierarchyManager hierarchyManager;
    private readonly SystemManager systemManager;
    private readonly SystemHookManager systemHookManager = new();
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
    private readonly ComponentArrayPoolManager arrayPoolManager = new();
    private readonly StatisticsManager statisticsManager;
    private readonly EntityBuilder builder;
    private SaveManager? saveManager;

    /// <summary>
    /// Unique identifier for this world instance.
    /// </summary>
    /// <remarks>
    /// This identifier is useful for distinguishing between multiple worlds in the same process,
    /// such as in client-server scenarios or multi-scene games.
    /// </remarks>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Optional name for this world, useful for debugging and logging.
    /// </summary>
    /// <remarks>
    /// When working with multiple worlds, setting meaningful names like "Client", "Server",
    /// or "MainMenu" helps with debugging and tracing issues.
    /// </remarks>
    public string? Name { get; set; }

    /// <summary>
    /// The component registry for this world.
    /// Component IDs are unique per-world, not global.
    /// </summary>
    public ComponentRegistry Components { get; } = new();

    /// <summary>
    /// Gets the component registry as an interface for capability-based access.
    /// </summary>
    IComponentRegistry ISerializationCapability.Components => Components;

    /// <summary>
    /// Gets the total number of entities in the world.
    /// </summary>
    public int EntityCount => archetypeManager.EntityCount;

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
    /// Gets the array pool manager for this world.
    /// Provides pooled arrays for component storage with per-world statistics tracking.
    /// </summary>
    public ComponentArrayPoolManager ArrayPools => arrayPoolManager;

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
    /// <param name="seed">
    /// Optional seed for the world's random number generator.
    /// If null, uses a time-based seed. If specified, enables deterministic behavior
    /// for replays and testing.
    /// </param>
    /// <remarks>
    /// <para>
    /// Each world has its own isolated random number generator state. Providing a seed
    /// ensures that all random operations (via <see cref="NextInt(int)"/>, <see cref="NextFloat"/>, etc.)
    /// will produce the same sequence of values across runs, enabling deterministic simulations.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Non-deterministic world (different results each run)
    /// var world1 = new World();
    ///
    /// // Deterministic world (same results with same seed)
    /// var world2 = new World(seed: 12345);
    /// var world3 = new World(seed: 12345); // Same sequence as world2
    /// </code>
    /// </example>
    public World(int? seed = null)
    {
        random = seed.HasValue ? new Random(seed.Value) : new Random();
        entityPool = new EntityPool();
        archetypeManager = new ArchetypeManager(Components);
        queryManager = new QueryManager(archetypeManager);
        hierarchyManager = new HierarchyManager(this);
        systemManager = new SystemManager(this, systemHookManager);
        pluginManager = new PluginManager(this, systemManager);
        changeTracker = new ChangeTracker(entityPool);
        prefabManager = new PrefabManager(this);
        validationManager = new ComponentValidationManager(this);
        statisticsManager = new StatisticsManager(entityPool, archetypeManager, Components, systemManager, queryManager);
        builder = new EntityBuilder(this);

        // Pre-allocate archetypes for known bundles (generated by BundleGenerator)
        PreallocateBundleArchetypes();
    }

    /// <summary>
    /// Pre-allocates archetypes for all known bundles.
    /// Called automatically during World initialization.
    /// </summary>
    /// <remarks>
    /// This method is implemented as a partial method that is generated by the BundleGenerator.
    /// If no bundles are defined, the generator provides an empty implementation.
    /// </remarks>
    partial void PreallocateBundleArchetypes();

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
        arrayPoolManager.Clear();
        systemHookManager.Clear();
    }
}
