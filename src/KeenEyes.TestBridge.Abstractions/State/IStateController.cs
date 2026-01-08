namespace KeenEyes.TestBridge.State;

/// <summary>
/// Controller for querying world state.
/// </summary>
/// <remarks>
/// <para>
/// The state controller provides read-only access to the ECS world state.
/// It allows querying entities, components, systems, and performance metrics.
/// </para>
/// <para>
/// All state queries return snapshots - the actual world state may change
/// between queries. For consistent state inspection, pause the simulation.
/// </para>
/// </remarks>
public interface IStateController
{
    /// <summary>
    /// Gets the number of entities in the world.
    /// </summary>
    /// <returns>The total entity count.</returns>
    Task<int> GetEntityCountAsync();

    /// <summary>
    /// Gets all entities matching a query.
    /// </summary>
    /// <param name="query">The query parameters.</param>
    /// <returns>A list of entity snapshots.</returns>
    Task<IReadOnlyList<EntitySnapshot>> QueryEntitiesAsync(EntityQuery query);

    /// <summary>
    /// Gets detailed information about a specific entity.
    /// </summary>
    /// <param name="entityId">The entity ID.</param>
    /// <returns>The entity snapshot, or null if not found.</returns>
    Task<EntitySnapshot?> GetEntityAsync(int entityId);

    /// <summary>
    /// Gets an entity by name.
    /// </summary>
    /// <param name="name">The entity name.</param>
    /// <returns>The entity snapshot, or null if not found.</returns>
    /// <remarks>
    /// If multiple entities have the same name, the first one found is returned.
    /// </remarks>
    Task<EntitySnapshot?> GetEntityByNameAsync(string name);

    /// <summary>
    /// Gets a component value from an entity.
    /// </summary>
    /// <param name="entityId">The entity ID.</param>
    /// <param name="componentTypeName">The component type name.</param>
    /// <returns>The component data as a dictionary of field names to values.</returns>
    /// <remarks>
    /// For IPC mode, component data is serialized as a dictionary. For in-process mode,
    /// this may return the actual component object.
    /// </remarks>
    Task<IReadOnlyDictionary<string, object?>?> GetComponentAsync(int entityId, string componentTypeName);

    /// <summary>
    /// Gets world statistics.
    /// </summary>
    /// <returns>Statistics about the world state.</returns>
    Task<WorldStats> GetWorldStatsAsync();

    /// <summary>
    /// Gets all registered systems and their current state.
    /// </summary>
    /// <returns>A list of system information.</returns>
    Task<IReadOnlyList<SystemInfo>> GetSystemsAsync();

    /// <summary>
    /// Gets a world extension value by type name.
    /// </summary>
    /// <param name="typeName">The extension type name.</param>
    /// <returns>The extension data, or null if not found.</returns>
    /// <remarks>
    /// For IPC mode, only serializable extensions can be retrieved.
    /// </remarks>
    Task<object?> GetExtensionAsync(string typeName);

    /// <summary>
    /// Checks if an extension is registered.
    /// </summary>
    /// <param name="typeName">The extension type name.</param>
    /// <returns>True if the extension is registered.</returns>
    Task<bool> HasExtensionAsync(string typeName);

    /// <summary>
    /// Gets performance metrics for the last N frames.
    /// </summary>
    /// <param name="frameCount">The number of frames to analyze. Defaults to 60.</param>
    /// <returns>Performance metrics summary.</returns>
    Task<PerformanceMetrics> GetPerformanceMetricsAsync(int frameCount = 60);

    /// <summary>
    /// Gets all entities with the specified tag.
    /// </summary>
    /// <param name="tag">The tag name.</param>
    /// <returns>A list of entity IDs with the tag.</returns>
    Task<IReadOnlyList<int>> GetEntitiesWithTagAsync(string tag);

    /// <summary>
    /// Gets the children of an entity.
    /// </summary>
    /// <param name="parentId">The parent entity ID.</param>
    /// <returns>A list of child entity IDs.</returns>
    Task<IReadOnlyList<int>> GetChildrenAsync(int parentId);

    /// <summary>
    /// Gets the parent of an entity.
    /// </summary>
    /// <param name="entityId">The entity ID.</param>
    /// <returns>The parent entity ID, or null if no parent.</returns>
    Task<int?> GetParentAsync(int entityId);
}

/// <summary>
/// Query parameters for filtering entities.
/// </summary>
public sealed record EntityQuery
{
    /// <summary>
    /// Gets or sets component types that entities must have.
    /// </summary>
    public string[]? WithComponents { get; init; }

    /// <summary>
    /// Gets or sets component types that entities must not have.
    /// </summary>
    public string[]? WithoutComponents { get; init; }

    /// <summary>
    /// Gets or sets tags that entities must have.
    /// </summary>
    public string[]? WithTags { get; init; }

    /// <summary>
    /// Gets or sets a name pattern to match (supports * and ? wildcards).
    /// </summary>
    public string? NamePattern { get; init; }

    /// <summary>
    /// Gets or sets a parent entity ID to filter by.
    /// </summary>
    public int? ParentId { get; init; }

    /// <summary>
    /// Gets or sets the maximum number of results to return. Defaults to 1000.
    /// </summary>
    public int MaxResults { get; init; } = 1000;

    /// <summary>
    /// Gets or sets the number of results to skip (for pagination).
    /// </summary>
    public int Skip { get; init; } = 0;

    /// <summary>
    /// Gets or sets whether to include component data in entity snapshots.
    /// </summary>
    /// <remarks>
    /// When false (default), only component type names are returned for performance.
    /// When true, full component field values are included in the response.
    /// </remarks>
    public bool IncludeComponentData { get; init; } = false;
}

/// <summary>
/// Snapshot of an entity's state.
/// </summary>
public sealed record EntitySnapshot
{
    /// <summary>
    /// Gets the entity ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the entity version.
    /// </summary>
    public required int Version { get; init; }

    /// <summary>
    /// Gets the entity's name, or null if unnamed.
    /// </summary>
    public required string? Name { get; init; }

    /// <summary>
    /// Gets the component data for each component type.
    /// </summary>
    /// <remarks>
    /// Keys are component type names. Values are dictionaries of field names to values.
    /// </remarks>
    public required IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> Components { get; init; }

    /// <summary>
    /// Gets the component type names (without full data).
    /// </summary>
    public required string[] ComponentTypes { get; init; }

    /// <summary>
    /// Gets the parent entity ID, or null if no parent.
    /// </summary>
    public required int? ParentId { get; init; }

    /// <summary>
    /// Gets the child entity IDs.
    /// </summary>
    public required int[] ChildIds { get; init; }

    /// <summary>
    /// Gets the tags assigned to this entity.
    /// </summary>
    public required string[] Tags { get; init; }
}

/// <summary>
/// World statistics.
/// </summary>
public sealed record WorldStats
{
    /// <summary>
    /// Gets the total number of entities.
    /// </summary>
    public required int EntityCount { get; init; }

    /// <summary>
    /// Gets the number of archetypes.
    /// </summary>
    public required int ArchetypeCount { get; init; }

    /// <summary>
    /// Gets the number of registered systems.
    /// </summary>
    public required int SystemCount { get; init; }

    /// <summary>
    /// Gets the estimated memory usage in bytes.
    /// </summary>
    public required long MemoryBytes { get; init; }

    /// <summary>
    /// Gets the number of registered component types.
    /// </summary>
    public required int ComponentTypeCount { get; init; }

    /// <summary>
    /// Gets the number of registered plugins.
    /// </summary>
    public required int PluginCount { get; init; }

    /// <summary>
    /// Gets the current frame number.
    /// </summary>
    public required long FrameNumber { get; init; }

    /// <summary>
    /// Gets the total elapsed time.
    /// </summary>
    public required TimeSpan ElapsedTime { get; init; }
}

/// <summary>
/// Information about a registered system.
/// </summary>
public sealed record SystemInfo
{
    /// <summary>
    /// Gets the system's type name.
    /// </summary>
    public required string TypeName { get; init; }

    /// <summary>
    /// Gets the execution phase.
    /// </summary>
    public required string Phase { get; init; }

    /// <summary>
    /// Gets the execution order within the phase.
    /// </summary>
    public required int Order { get; init; }

    /// <summary>
    /// Gets whether the system is enabled.
    /// </summary>
    public required bool Enabled { get; init; }

    /// <summary>
    /// Gets the average execution time in milliseconds.
    /// </summary>
    public required double AverageExecutionMs { get; init; }

    /// <summary>
    /// Gets the system group name, if any.
    /// </summary>
    public string? GroupName { get; init; }
}

/// <summary>
/// Performance metrics summary.
/// </summary>
public sealed record PerformanceMetrics
{
    /// <summary>
    /// Gets the average frame time in milliseconds.
    /// </summary>
    public required double AverageFrameTimeMs { get; init; }

    /// <summary>
    /// Gets the minimum frame time in milliseconds.
    /// </summary>
    public required double MinFrameTimeMs { get; init; }

    /// <summary>
    /// Gets the maximum frame time in milliseconds.
    /// </summary>
    public required double MaxFrameTimeMs { get; init; }

    /// <summary>
    /// Gets the average frames per second.
    /// </summary>
    public required double AverageFps { get; init; }

    /// <summary>
    /// Gets the 99th percentile frame time in milliseconds.
    /// </summary>
    public required double P99FrameTimeMs { get; init; }

    /// <summary>
    /// Gets the number of frames analyzed.
    /// </summary>
    public required int SampleCount { get; init; }

    /// <summary>
    /// Gets the average execution time per system.
    /// </summary>
    public required IReadOnlyDictionary<string, double> SystemAverages { get; init; }
}
