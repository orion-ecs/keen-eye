namespace KeenEyes.Logging;

/// <summary>
/// Defines categories for ECS-specific logging.
/// </summary>
/// <remarks>
/// Each category can have its own minimum log level configured via
/// <see cref="EcsLogger.SetCategoryLevel(EcsLogCategory, LogLevel)"/>.
/// </remarks>
public enum EcsLogCategory
{
    /// <summary>
    /// Logs related to system execution, including timing and lifecycle.
    /// </summary>
    /// <remarks>
    /// Includes system registration, initialization, update execution timing,
    /// and system enable/disable events.
    /// </remarks>
    System,

    /// <summary>
    /// Logs related to entity lifecycle events.
    /// </summary>
    /// <remarks>
    /// Includes entity creation, destruction, naming, and hierarchy changes.
    /// </remarks>
    Entity,

    /// <summary>
    /// Logs related to component changes.
    /// </summary>
    /// <remarks>
    /// Includes component addition, removal, and value changes.
    /// Note: Component change logging can be high-volume in active scenes.
    /// </remarks>
    Component,

    /// <summary>
    /// Logs related to query execution and caching.
    /// </summary>
    /// <remarks>
    /// Includes query cache hits/misses, archetype matching, and query statistics.
    /// </remarks>
    Query
}
