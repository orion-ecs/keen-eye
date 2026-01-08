using System.ComponentModel;
using KeenEyes.Mcp.TestBridge.Connection;
using KeenEyes.TestBridge.State;
using ModelContextProtocol.Server;

namespace KeenEyes.Mcp.TestBridge.Tools;

/// <summary>
/// MCP tools for querying game state (entities, components, systems, performance).
/// </summary>
[McpServerToolType]
public sealed class StateTools(BridgeConnectionManager connection)
{
    #region Entity Queries

    [McpServerTool(Name = "state_get_entity_count")]
    [Description("Get the total number of entities in the world.")]
    public async Task<EntityCountResult> StateGetEntityCount()
    {
        var bridge = connection.GetBridge();
        var count = await bridge.State.GetEntityCountAsync();

        return new EntityCountResult { Count = count };
    }

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

    [McpServerTool(Name = "state_get_entity")]
    [Description("Get detailed information about an entity by its ID.")]
    public async Task<EntitySnapshot?> StateGetEntity(
        [Description("The entity ID")]
        int entityId)
    {
        var bridge = connection.GetBridge();
        return await bridge.State.GetEntityAsync(entityId);
    }

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

    // TODO: Issue #853 - state_get_component fails with MCP serialization error
    // The MCP framework cannot serialize JsonElement? return types.
    // Use state_get_entity instead which returns full component data.
    // [McpServerTool(Name = "state_get_component")]
    // [Description("Get component data from an entity. Returns field names and values as a dictionary.")]
    // public async Task<JsonElement?> StateGetComponent(
    //     [Description("The entity ID")]
    //     int entityId,
    //     [Description("The component type name (e.g., 'Position', 'Health')")]
    //     string componentType)
    // {
    //     var bridge = connection.GetBridge();
    //     return await bridge.State.GetComponentAsync(entityId, componentType);
    // }

    #endregion

    #region Hierarchy

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

    [McpServerTool(Name = "state_get_world_stats")]
    [Description("Get statistics about the world: entity count, archetype count, memory usage, etc.")]
    public async Task<WorldStats> StateGetWorldStats()
    {
        var bridge = connection.GetBridge();
        return await bridge.State.GetWorldStatsAsync();
    }

    [McpServerTool(Name = "state_get_systems")]
    [Description("List all registered systems with their phase, order, and execution stats.")]
    public async Task<IReadOnlyList<SystemInfo>> StateGetSystems()
    {
        var bridge = connection.GetBridge();
        return await bridge.State.GetSystemsAsync();
    }

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
    public required int Count { get; init; }
}

/// <summary>
/// Result of a children query.
/// </summary>
public sealed record ChildrenResult
{
    public required int ParentId { get; init; }
    public required int[] ChildIds { get; init; }
    public required int Count { get; init; }
}

/// <summary>
/// Result of a parent query.
/// </summary>
public sealed record ParentResult
{
    public required int EntityId { get; init; }
    public int? ParentId { get; init; }
    public required bool HasParent { get; init; }
}

/// <summary>
/// Result of a tag query.
/// </summary>
public sealed record TagQueryResult
{
    public required string Tag { get; init; }
    public required int[] EntityIds { get; init; }
    public required int Count { get; init; }
}

#endregion
