using System.ComponentModel;
using System.Text.Json;
using KeenEyes.Mcp.TestBridge.Connection;
using KeenEyes.TestBridge.State;
using ModelContextProtocol.Server;

namespace KeenEyes.Mcp.TestBridge.Tools;

/// <summary>
/// MCP tools for querying game state (entities, components, systems, performance).
/// </summary>
/// <param name="connection">The connection manager used to reach the active test bridge.</param>
[McpServerToolType]
public sealed class StateTools(BridgeConnectionManager connection)
{
    #region Entity Queries

    /// <summary>
    /// Gets the total number of entities currently in the world.
    /// </summary>
    /// <returns>The entity count.</returns>
    [McpServerTool(Name = "state_get_entity_count")]
    [Description("Get the total number of entities in the world.")]
    public async Task<EntityCountResult> StateGetEntityCount()
    {
        var bridge = connection.GetBridge();
        var count = await bridge.State.GetEntityCountAsync();

        return new EntityCountResult { Count = count };
    }

    /// <summary>
    /// Queries entities matching the given filters and returns their snapshots.
    /// </summary>
    /// <param name="withComponents">Component types entities must have (e.g., ['Position', 'Velocity']).</param>
    /// <param name="withoutComponents">Component types entities must NOT have.</param>
    /// <param name="withTags">Tags entities must have.</param>
    /// <param name="namePattern">Name pattern to match (supports * and ? wildcards).</param>
    /// <param name="parentId">Parent entity ID to filter by.</param>
    /// <param name="maxResults">The maximum number of results to return.</param>
    /// <param name="includeComponentData">Whether to include full component data in the results.</param>
    /// <returns>The entity snapshots matching the criteria.</returns>
    [McpServerTool(Name = "state_query_entities")]
    [Description("Query entities with filters. Returns entity snapshots matching the criteria.")]
    public async Task<IReadOnlyList<EntitySnapshot>> StateQueryEntities(
        [Description("Component types entities must have (e.g., ['Position', 'Velocity'])")]
        string[]? withComponents = null,
        [Description("Component types entities must NOT have")]
        string[]? withoutComponents = null,
        [Description("Tags entities must have")]
        string[]? withTags = null,
        [Description("Name pattern to match (supports * and ? wildcards)")]
        string? namePattern = null,
        [Description("Parent entity ID to filter by")]
        int? parentId = null,
        [Description("Maximum results to return (default: 100)")]
        int maxResults = 100,
        [Description("Include full component data in results (default: false for performance)")]
        bool includeComponentData = false)
    {
        var bridge = connection.GetBridge();

        var query = new EntityQuery
        {
            WithComponents = withComponents,
            WithoutComponents = withoutComponents,
            WithTags = withTags,
            NamePattern = namePattern,
            ParentId = parentId,
            MaxResults = maxResults,
            IncludeComponentData = includeComponentData
        };

        return await bridge.State.QueryEntitiesAsync(query);
    }

    /// <summary>
    /// Gets detailed information about an entity by its ID.
    /// </summary>
    /// <param name="entityId">The entity ID.</param>
    /// <returns>The entity snapshot, or <see langword="null"/> if no such entity exists.</returns>
    [McpServerTool(Name = "state_get_entity")]
    [Description("Get detailed information about an entity by its ID.")]
    public async Task<EntitySnapshot?> StateGetEntity(
        [Description("The entity ID")]
        int entityId)
    {
        var bridge = connection.GetBridge();
        return await bridge.State.GetEntityAsync(entityId);
    }

    /// <summary>
    /// Finds the first entity with the given name.
    /// </summary>
    /// <param name="name">The entity name to search for.</param>
    /// <returns>The matching entity snapshot, or <see langword="null"/> if none is found.</returns>
    [McpServerTool(Name = "state_get_entity_by_name")]
    [Description("Find an entity by its name. Returns the first entity with the given name.")]
    public async Task<EntitySnapshot?> StateGetEntityByName(
        [Description("The entity name to search for")]
        string name)
    {
        var bridge = connection.GetBridge();
        return await bridge.State.GetEntityByNameAsync(name);
    }

    #endregion

    #region Components

    /// <summary>
    /// Gets component data from an entity as field names and values.
    /// </summary>
    /// <param name="entityId">The entity ID.</param>
    /// <param name="componentType">The component type name (e.g., 'Position', 'Health').</param>
    /// <returns>The component data lookup result.</returns>
    [McpServerTool(Name = "state_get_component")]
    [Description("Get component data from an entity. Returns field names and values.")]
    public async Task<ComponentDataResult> StateGetComponent(
        [Description("The entity ID")]
        int entityId,
        [Description("The component type name (e.g., 'Position', 'Health')")]
        string componentType)
    {
        var bridge = connection.GetBridge();
        var componentData = await bridge.State.GetComponentAsync(entityId, componentType);

        if (componentData == null)
        {
            return new ComponentDataResult
            {
                EntityId = entityId,
                ComponentType = componentType,
                Found = false,
                Data = null,
                Error = $"Component '{componentType}' not found on entity {entityId}"
            };
        }

        // Convert JsonElement to Dictionary<string, JsonElement>
        var data = new Dictionary<string, JsonElement>();
        if (componentData.Value.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in componentData.Value.EnumerateObject())
            {
                data[property.Name] = property.Value.Clone();
            }
        }

        return new ComponentDataResult
        {
            EntityId = entityId,
            ComponentType = componentType,
            Found = true,
            Data = data,
            Error = null
        };
    }

    #endregion

    #region Hierarchy

    /// <summary>
    /// Gets the child entity IDs of an entity.
    /// </summary>
    /// <param name="entityId">The parent entity ID.</param>
    /// <returns>The child entity IDs.</returns>
    [McpServerTool(Name = "state_get_children")]
    [Description("Get the child entity IDs of an entity.")]
    public async Task<ChildrenResult> StateGetChildren(
        [Description("The parent entity ID")]
        int entityId)
    {
        var bridge = connection.GetBridge();
        var children = await bridge.State.GetChildrenAsync(entityId);

        return new ChildrenResult
        {
            ParentId = entityId,
            ChildIds = children.ToArray(),
            Count = children.Count
        };
    }

    /// <summary>
    /// Gets the parent entity ID of an entity.
    /// </summary>
    /// <param name="entityId">The entity ID.</param>
    /// <returns>The parent lookup result; <see cref="ParentResult.HasParent"/> is <see langword="false"/> if the entity has no parent.</returns>
    [McpServerTool(Name = "state_get_parent")]
    [Description("Get the parent entity ID of an entity. Returns null if entity has no parent.")]
    public async Task<ParentResult> StateGetParent(
        [Description("The entity ID")]
        int entityId)
    {
        var bridge = connection.GetBridge();
        var parentId = await bridge.State.GetParentAsync(entityId);

        return new ParentResult
        {
            EntityId = entityId,
            ParentId = parentId,
            HasParent = parentId.HasValue
        };
    }

    /// <summary>
    /// Finds all entity IDs that have a specific tag.
    /// </summary>
    /// <param name="tag">The tag name to search for.</param>
    /// <returns>The matching entity IDs.</returns>
    [McpServerTool(Name = "state_get_entities_with_tag")]
    [Description("Find all entity IDs that have a specific tag.")]
    public async Task<TagQueryResult> StateGetEntitiesWithTag(
        [Description("The tag name to search for")]
        string tag)
    {
        var bridge = connection.GetBridge();
        var entities = await bridge.State.GetEntitiesWithTagAsync(tag);

        return new TagQueryResult
        {
            Tag = tag,
            EntityIds = entities.ToArray(),
            Count = entities.Count
        };
    }

    #endregion

    #region World Info

    /// <summary>
    /// Gets statistics about the world, including entity count, archetype count, and memory usage.
    /// </summary>
    /// <returns>The world statistics.</returns>
    [McpServerTool(Name = "state_get_world_stats")]
    [Description("Get statistics about the world: entity count, archetype count, memory usage, etc.")]
    public async Task<WorldStats> StateGetWorldStats()
    {
        var bridge = connection.GetBridge();
        return await bridge.State.GetWorldStatsAsync();
    }

    /// <summary>
    /// Lists all registered systems with their phase, order, and execution stats.
    /// </summary>
    /// <returns>The registered systems' information.</returns>
    [McpServerTool(Name = "state_get_systems")]
    [Description("List all registered systems with their phase, order, and execution stats.")]
    public async Task<IReadOnlyList<SystemInfo>> StateGetSystems()
    {
        var bridge = connection.GetBridge();
        return await bridge.State.GetSystemsAsync();
    }

    /// <summary>
    /// Gets performance metrics such as FPS, frame times, and per-system timing.
    /// </summary>
    /// <param name="frameCount">The number of frames to analyze.</param>
    /// <returns>The performance metrics.</returns>
    [McpServerTool(Name = "state_get_performance")]
    [Description("Get performance metrics: FPS, frame times, per-system timing.")]
    public async Task<PerformanceMetrics> StateGetPerformance(
        [Description("Number of frames to analyze (default: 60)")]
        int frameCount = 60)
    {
        var bridge = connection.GetBridge();
        return await bridge.State.GetPerformanceMetricsAsync(frameCount);
    }

    #endregion
}

#region Result Records

/// <summary>
/// Result containing entity count.
/// </summary>
public sealed record EntityCountResult
{
    /// <summary>
    /// Gets the total number of entities in the world.
    /// </summary>
    public required int Count { get; init; }
}

/// <summary>
/// Result of a children query.
/// </summary>
public sealed record ChildrenResult
{
    /// <summary>
    /// Gets the ID of the entity whose children were queried.
    /// </summary>
    public required int ParentId { get; init; }

    /// <summary>
    /// Gets the child entity IDs.
    /// </summary>
    public required int[] ChildIds { get; init; }

    /// <summary>
    /// Gets the number of child entities.
    /// </summary>
    public required int Count { get; init; }
}

/// <summary>
/// Result of a parent query.
/// </summary>
public sealed record ParentResult
{
    /// <summary>
    /// Gets the ID of the entity whose parent was queried.
    /// </summary>
    public required int EntityId { get; init; }

    /// <summary>
    /// Gets the parent entity ID, or <see langword="null"/> if the entity has no parent.
    /// </summary>
    public int? ParentId { get; init; }

    /// <summary>
    /// Gets whether the entity has a parent.
    /// </summary>
    public required bool HasParent { get; init; }
}

/// <summary>
/// Result of a tag query.
/// </summary>
public sealed record TagQueryResult
{
    /// <summary>
    /// Gets the tag name that was queried.
    /// </summary>
    public required string Tag { get; init; }

    /// <summary>
    /// Gets the IDs of entities that have the tag.
    /// </summary>
    public required int[] EntityIds { get; init; }

    /// <summary>
    /// Gets the number of matching entities.
    /// </summary>
    public required int Count { get; init; }
}

/// <summary>
/// Result of a component data query.
/// </summary>
public sealed record ComponentDataResult
{
    /// <summary>
    /// Gets the entity ID that was queried.
    /// </summary>
    public required int EntityId { get; init; }

    /// <summary>
    /// Gets the component type name that was queried.
    /// </summary>
    public required string ComponentType { get; init; }

    /// <summary>
    /// Gets whether the component was found on the entity.
    /// </summary>
    public required bool Found { get; init; }

    /// <summary>
    /// Gets the component field data as key-value pairs.
    /// Null when Found is false.
    /// </summary>
    public IReadOnlyDictionary<string, JsonElement>? Data { get; init; }

    /// <summary>
    /// Gets an error message if the query failed.
    /// </summary>
    public string? Error { get; init; }
}

#endregion
