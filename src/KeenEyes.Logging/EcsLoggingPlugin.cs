using KeenEyes.Capabilities;

namespace KeenEyes.Logging;

/// <summary>
/// A plugin that provides automatic ECS-specific logging for a world.
/// </summary>
/// <remarks>
/// <para>
/// EcsLoggingPlugin automatically hooks into world events to log:
/// </para>
/// <list type="bullet">
/// <item>System execution (start, complete, enable/disable)</item>
/// <item>Entity lifecycle (create, destroy)</item>
/// <item>Component changes (add, remove, change) - requires explicit registration</item>
/// </list>
/// <para>
/// The plugin uses the <see cref="EcsLogger"/> class internally, which provides
/// per-category verbosity configuration. Access the logger via <see cref="Logger"/>
/// to customize logging levels.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var logManager = new LogManager();
/// logManager.AddProvider(new ConsoleLogProvider());
///
/// var plugin = new EcsLoggingPlugin(logManager);
///
/// // Configure verbosity before installing
/// plugin.Logger.SetCategoryLevel(EcsLogCategory.System, LogLevel.Debug);
/// plugin.Logger.SetCategoryLevel(EcsLogCategory.Component, LogLevel.Warning);
///
/// var world = new WorldBuilder()
///     .WithPlugin(plugin)
///     .Build();
///
/// // Component logging requires explicit registration
/// plugin.EnableComponentLogging&lt;Position&gt;();
/// plugin.EnableComponentLogging&lt;Velocity&gt;();
/// </code>
/// </example>
public sealed class EcsLoggingPlugin : IWorldPlugin
{
    private readonly EcsLogger logger;
    private readonly List<EventSubscription> subscriptions = [];
    private readonly List<Action> componentUnsubscribers = [];
    private IWorld? currentWorld;

    /// <summary>
    /// Initializes a new instance of the <see cref="EcsLoggingPlugin"/> class.
    /// </summary>
    /// <param name="logManager">The log manager to write messages to.</param>
    /// <exception cref="ArgumentNullException">Thrown when logManager is null.</exception>
    public EcsLoggingPlugin(LogManager logManager)
    {
        ArgumentNullException.ThrowIfNull(logManager);
        logger = new EcsLogger(logManager);
    }

    /// <inheritdoc />
    public string Name => "EcsLogging";

    /// <summary>
    /// Gets the ECS logger for configuring verbosity levels.
    /// </summary>
    public EcsLogger Logger => logger;

    /// <inheritdoc />
    public void Install(IPluginContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        currentWorld = context.World;

        // Set up system hooks if available
        if (context.TryGetCapability<ISystemHookCapability>(out var hookCapability) && hookCapability != null)
        {
            var hookSubscription = hookCapability.AddSystemHook(
                beforeHook: OnBeforeSystem,
                afterHook: OnAfterSystem
            );
            subscriptions.Add(hookSubscription);
        }

        // Subscribe to entity lifecycle events
        subscriptions.Add(context.World.OnEntityCreated(OnEntityCreated));
        subscriptions.Add(context.World.OnEntityDestroyed(OnEntityDestroyed));

        // Expose the logger as an extension for easy access
        context.SetExtension(logger);
    }

    /// <inheritdoc />
    public void Uninstall(IPluginContext context)
    {
        // Dispose all subscriptions
        foreach (var subscription in subscriptions)
        {
            subscription.Dispose();
        }
        subscriptions.Clear();

        // Clean up component subscriptions
        foreach (var unsubscriber in componentUnsubscribers)
        {
            unsubscriber();
        }
        componentUnsubscribers.Clear();

        context.RemoveExtension<EcsLogger>();
        currentWorld = null;
    }

    /// <summary>
    /// Enables logging for component add/remove/change events of the specified type.
    /// </summary>
    /// <typeparam name="T">The component type to log.</typeparam>
    /// <remarks>
    /// <para>
    /// Component logging must be explicitly enabled per type because subscribing to
    /// events requires compile-time type information. This also prevents excessive
    /// logging overhead for components that don't need tracking.
    /// </para>
    /// <para>
    /// Call this method after the plugin is installed. The subscriptions will be
    /// automatically cleaned up when the plugin is uninstalled.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when the plugin is not installed.</exception>
    public void EnableComponentLogging<T>() where T : struct, IComponent
    {
        if (currentWorld == null)
        {
            throw new InvalidOperationException("Plugin must be installed before enabling component logging.");
        }

        var typeName = typeof(T).Name;

        var addedSub = currentWorld.OnComponentAdded<T>((entity, _) =>
            logger.LogComponentAdded(entity.Id, typeName));

        var removedSub = currentWorld.OnComponentRemoved<T>(entity =>
            logger.LogComponentRemoved(entity.Id, typeName));

        var changedSub = currentWorld.OnComponentChanged<T>((entity, _, _) =>
            logger.LogComponentChanged(entity.Id, typeName));

        subscriptions.Add(addedSub);
        subscriptions.Add(removedSub);
        subscriptions.Add(changedSub);

        // Track unsubscription for component-specific cleanup
        componentUnsubscribers.Add(() =>
        {
            addedSub.Dispose();
            removedSub.Dispose();
            changedSub.Dispose();
        });
    }

    /// <summary>
    /// Logs current query cache statistics.
    /// </summary>
    /// <param name="cachedQueries">The number of cached queries.</param>
    /// <param name="cacheHits">Total cache hits.</param>
    /// <param name="cacheMisses">Total cache misses.</param>
    /// <param name="hitRate">The cache hit rate percentage.</param>
    public void LogQueryStats(int cachedQueries, long cacheHits, long cacheMisses, double hitRate)
    {
        logger.LogQueryCacheStats(cachedQueries, cacheHits, cacheMisses, hitRate);
    }

    private void OnBeforeSystem(ISystem system, float deltaTime)
    {
        logger.LogSystemStarted(system.GetType().Name, deltaTime);
    }

    private void OnAfterSystem(ISystem system, float deltaTime)
    {
        logger.LogSystemCompleted(system.GetType().Name, deltaTime);
    }

    private void OnEntityCreated(Entity entity, string? name)
    {
        logger.LogEntityCreated(entity.Id, entity.Version, name);
    }

    private void OnEntityDestroyed(Entity entity)
    {
        logger.LogEntityDestroyed(entity.Id, entity.Version);
    }
}
