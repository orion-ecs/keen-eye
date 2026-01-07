using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace KeenEyes.Logging;

/// <summary>
/// Provides ECS-specific logging with configurable verbosity per category.
/// </summary>
/// <remarks>
/// <para>
/// EcsLogger wraps a <see cref="LogManager"/> and provides specialized logging
/// methods for ECS operations (systems, entities, components, queries). Each
/// category can have its own minimum log level, enabling fine-grained control
/// over logging verbosity.
/// </para>
/// <para>
/// Performance optimization: When a category is disabled or below the minimum level,
/// logging methods return immediately with minimal overhead (level check only).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var logManager = new LogManager();
/// logManager.AddProvider(new ConsoleLogProvider());
///
/// var ecsLogger = new EcsLogger(logManager);
///
/// // Configure per-category verbosity
/// ecsLogger.SetCategoryLevel(EcsLogCategory.System, LogLevel.Debug);
/// ecsLogger.SetCategoryLevel(EcsLogCategory.Component, LogLevel.Warning); // Less verbose
///
/// // Log ECS events
/// ecsLogger.LogSystemStarted("MovementSystem", 0.016f);
/// ecsLogger.LogEntityCreated(entity, "Player");
/// </code>
/// </example>
public sealed class EcsLogger
{
    private const string SystemCategory = "ECS.System";
    private const string EntityCategory = "ECS.Entity";
    private const string ComponentCategory = "ECS.Component";
    private const string QueryCategory = "ECS.Query";

    private readonly LogManager logManager;
    private readonly LogLevel[] categoryLevels;
    private readonly Stopwatch stopwatch = new();
    private long lastSystemStartTicks;

    /// <summary>
    /// Initializes a new instance of the <see cref="EcsLogger"/> class.
    /// </summary>
    /// <param name="logManager">The log manager to write messages to.</param>
    /// <exception cref="ArgumentNullException">Thrown when logManager is null.</exception>
    public EcsLogger(LogManager logManager)
    {
        ArgumentNullException.ThrowIfNull(logManager);
        this.logManager = logManager;

        // Initialize all categories to Trace (most verbose) by default
        categoryLevels = new LogLevel[4];
        for (int i = 0; i < categoryLevels.Length; i++)
        {
            categoryLevels[i] = LogLevel.Trace;
        }

        stopwatch.Start();
    }

    /// <summary>
    /// Gets the underlying log manager.
    /// </summary>
    public LogManager LogManager => logManager;

    /// <summary>
    /// Gets or sets whether logging is globally enabled.
    /// </summary>
    /// <remarks>
    /// When false, all logging methods return immediately without any processing.
    /// This provides a master switch to disable all ECS logging.
    /// </remarks>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Sets the minimum log level for a specific ECS category.
    /// </summary>
    /// <param name="category">The category to configure.</param>
    /// <param name="level">The minimum level for this category.</param>
    /// <remarks>
    /// Messages below this level for the specified category will not be logged.
    /// </remarks>
    public void SetCategoryLevel(EcsLogCategory category, LogLevel level)
    {
        categoryLevels[(int)category] = level;
    }

    /// <summary>
    /// Gets the minimum log level for a specific ECS category.
    /// </summary>
    /// <param name="category">The category to query.</param>
    /// <returns>The minimum log level for the category.</returns>
    public LogLevel GetCategoryLevel(EcsLogCategory category)
    {
        return categoryLevels[(int)category];
    }

    /// <summary>
    /// Checks if logging is enabled for the specified category and level.
    /// </summary>
    /// <param name="category">The ECS category.</param>
    /// <param name="level">The log level to check.</param>
    /// <returns>True if a message at this level would be logged.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsLevelEnabled(EcsLogCategory category, LogLevel level)
    {
        return IsEnabled &&
               level >= categoryLevels[(int)category] &&
               logManager.IsLevelEnabled(level);
    }

    #region System Logging

    /// <summary>
    /// Logs that a system has been registered.
    /// </summary>
    /// <param name="systemType">The type name of the system.</param>
    /// <param name="phase">The execution phase.</param>
    /// <param name="order">The execution order within the phase.</param>
    public void LogSystemRegistered(string systemType, string phase, int order)
    {
        if (!IsLevelEnabled(EcsLogCategory.System, LogLevel.Info))
        {
            return;
        }

        logManager.Info(SystemCategory, $"System registered: {systemType} (Phase={phase}, Order={order})",
            new Dictionary<string, object?>
            {
                ["SystemType"] = systemType,
                ["Phase"] = phase,
                ["Order"] = order
            });
    }

    /// <summary>
    /// Logs that a system has started executing.
    /// </summary>
    /// <param name="systemType">The type name of the system.</param>
    /// <param name="deltaTime">The delta time passed to the system.</param>
    public void LogSystemStarted(string systemType, float deltaTime)
    {
        lastSystemStartTicks = stopwatch.ElapsedTicks;

        if (!IsLevelEnabled(EcsLogCategory.System, LogLevel.Trace))
        {
            return;
        }

        logManager.Trace(SystemCategory, $"System started: {systemType} (dt={deltaTime:F4})",
            new Dictionary<string, object?>
            {
                ["SystemType"] = systemType,
                ["DeltaTime"] = deltaTime
            });
    }

    /// <summary>
    /// Logs that a system has finished executing.
    /// </summary>
    /// <param name="systemType">The type name of the system.</param>
    /// <param name="deltaTime">The delta time passed to the system.</param>
    public void LogSystemCompleted(string systemType, float deltaTime)
    {
        var elapsedTicks = stopwatch.ElapsedTicks - lastSystemStartTicks;
        var elapsedMs = (double)elapsedTicks / Stopwatch.Frequency * 1000.0;

        if (!IsLevelEnabled(EcsLogCategory.System, LogLevel.Debug))
        {
            return;
        }

        logManager.Debug(SystemCategory, $"System completed: {systemType} ({elapsedMs:F3}ms)",
            new Dictionary<string, object?>
            {
                ["SystemType"] = systemType,
                ["DeltaTime"] = deltaTime,
                ["ElapsedMs"] = elapsedMs
            });
    }

    /// <summary>
    /// Logs that a system's enabled state has changed.
    /// </summary>
    /// <param name="systemType">The type name of the system.</param>
    /// <param name="enabled">The new enabled state.</param>
    public void LogSystemEnabledChanged(string systemType, bool enabled)
    {
        if (!IsLevelEnabled(EcsLogCategory.System, LogLevel.Info))
        {
            return;
        }

        var message = enabled ? $"System enabled: {systemType}" : $"System disabled: {systemType}";
        logManager.Info(SystemCategory, message,
            new Dictionary<string, object?>
            {
                ["SystemType"] = systemType,
                ["Enabled"] = enabled
            });
    }

    #endregion

    #region Entity Logging

    /// <summary>
    /// Logs that an entity has been created.
    /// </summary>
    /// <param name="entityId">The entity's ID.</param>
    /// <param name="entityVersion">The entity's version.</param>
    /// <param name="name">The entity's name, if any.</param>
    public void LogEntityCreated(int entityId, int entityVersion, string? name)
    {
        if (!IsLevelEnabled(EcsLogCategory.Entity, LogLevel.Debug))
        {
            return;
        }

        var nameInfo = name != null ? $" '{name}'" : "";
        logManager.Debug(EntityCategory, $"Entity created: {entityId}v{entityVersion}{nameInfo}",
            new Dictionary<string, object?>
            {
                ["EntityId"] = entityId,
                ["EntityVersion"] = entityVersion,
                ["EntityName"] = name
            });
    }

    /// <summary>
    /// Logs that an entity has been destroyed.
    /// </summary>
    /// <param name="entityId">The entity's ID.</param>
    /// <param name="entityVersion">The entity's version.</param>
    public void LogEntityDestroyed(int entityId, int entityVersion)
    {
        if (!IsLevelEnabled(EcsLogCategory.Entity, LogLevel.Debug))
        {
            return;
        }

        logManager.Debug(EntityCategory, $"Entity destroyed: {entityId}v{entityVersion}",
            new Dictionary<string, object?>
            {
                ["EntityId"] = entityId,
                ["EntityVersion"] = entityVersion
            });
    }

    /// <summary>
    /// Logs a hierarchy change for an entity.
    /// </summary>
    /// <param name="childId">The child entity's ID.</param>
    /// <param name="parentId">The parent entity's ID, or null if unparented.</param>
    public void LogEntityParentChanged(int childId, int? parentId)
    {
        if (!IsLevelEnabled(EcsLogCategory.Entity, LogLevel.Trace))
        {
            return;
        }

        var message = parentId.HasValue
            ? $"Entity {childId} parented to {parentId.Value}"
            : $"Entity {childId} unparented";

        logManager.Trace(EntityCategory, message,
            new Dictionary<string, object?>
            {
                ["ChildId"] = childId,
                ["ParentId"] = parentId
            });
    }

    #endregion

    #region Component Logging

    /// <summary>
    /// Logs that a component has been added to an entity.
    /// </summary>
    /// <param name="entityId">The entity's ID.</param>
    /// <param name="componentType">The type name of the component.</param>
    public void LogComponentAdded(int entityId, string componentType)
    {
        if (!IsLevelEnabled(EcsLogCategory.Component, LogLevel.Trace))
        {
            return;
        }

        logManager.Trace(ComponentCategory, $"Component added: {componentType} to entity {entityId}",
            new Dictionary<string, object?>
            {
                ["EntityId"] = entityId,
                ["ComponentType"] = componentType
            });
    }

    /// <summary>
    /// Logs that a component has been removed from an entity.
    /// </summary>
    /// <param name="entityId">The entity's ID.</param>
    /// <param name="componentType">The type name of the component.</param>
    public void LogComponentRemoved(int entityId, string componentType)
    {
        if (!IsLevelEnabled(EcsLogCategory.Component, LogLevel.Trace))
        {
            return;
        }

        logManager.Trace(ComponentCategory, $"Component removed: {componentType} from entity {entityId}",
            new Dictionary<string, object?>
            {
                ["EntityId"] = entityId,
                ["ComponentType"] = componentType
            });
    }

    /// <summary>
    /// Logs that a component's value has changed.
    /// </summary>
    /// <param name="entityId">The entity's ID.</param>
    /// <param name="componentType">The type name of the component.</param>
    public void LogComponentChanged(int entityId, string componentType)
    {
        if (!IsLevelEnabled(EcsLogCategory.Component, LogLevel.Trace))
        {
            return;
        }

        logManager.Trace(ComponentCategory, $"Component changed: {componentType} on entity {entityId}",
            new Dictionary<string, object?>
            {
                ["EntityId"] = entityId,
                ["ComponentType"] = componentType
            });
    }

    #endregion

    #region Query Logging

    /// <summary>
    /// Logs a query execution with cache statistics.
    /// </summary>
    /// <param name="componentTypes">The component types in the query.</param>
    /// <param name="matchingArchetypes">The number of matching archetypes.</param>
    /// <param name="cacheHit">Whether the query was served from cache.</param>
    public void LogQueryExecuted(string[] componentTypes, int matchingArchetypes, bool cacheHit)
    {
        if (!IsLevelEnabled(EcsLogCategory.Query, LogLevel.Trace))
        {
            return;
        }

        var components = string.Join(", ", componentTypes);
        var cacheStatus = cacheHit ? "cache hit" : "cache miss";

        logManager.Trace(QueryCategory, $"Query executed: <{components}> ({matchingArchetypes} archetypes, {cacheStatus})",
            new Dictionary<string, object?>
            {
                ["ComponentTypes"] = componentTypes,
                ["MatchingArchetypes"] = matchingArchetypes,
                ["CacheHit"] = cacheHit
            });
    }

    /// <summary>
    /// Logs query cache statistics.
    /// </summary>
    /// <param name="cachedQueries">The number of cached queries.</param>
    /// <param name="cacheHits">Total cache hits.</param>
    /// <param name="cacheMisses">Total cache misses.</param>
    /// <param name="hitRate">The cache hit rate percentage.</param>
    public void LogQueryCacheStats(int cachedQueries, long cacheHits, long cacheMisses, double hitRate)
    {
        if (!IsLevelEnabled(EcsLogCategory.Query, LogLevel.Info))
        {
            return;
        }

        logManager.Info(QueryCategory, $"Query cache stats: {cachedQueries} queries, {hitRate:F1}% hit rate ({cacheHits} hits, {cacheMisses} misses)",
            new Dictionary<string, object?>
            {
                ["CachedQueries"] = cachedQueries,
                ["CacheHits"] = cacheHits,
                ["CacheMisses"] = cacheMisses,
                ["HitRate"] = hitRate
            });
    }

    /// <summary>
    /// Logs that the query cache has been invalidated.
    /// </summary>
    /// <param name="reason">The reason for invalidation.</param>
    public void LogQueryCacheInvalidated(string reason)
    {
        if (!IsLevelEnabled(EcsLogCategory.Query, LogLevel.Debug))
        {
            return;
        }

        logManager.Debug(QueryCategory, $"Query cache invalidated: {reason}",
            new Dictionary<string, object?>
            {
                ["Reason"] = reason
            });
    }

    #endregion
}
