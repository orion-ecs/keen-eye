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
public sealed partial class World : IWorld
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
