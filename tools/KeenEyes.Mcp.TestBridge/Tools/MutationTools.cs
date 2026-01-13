using System.ComponentModel;
using System.Text.Json;
using KeenEyes.Mcp.TestBridge.Connection;
using KeenEyes.TestBridge.Mutation;
using ModelContextProtocol.Server;

namespace KeenEyes.Mcp.TestBridge.Tools;

/// <summary>
/// MCP tools for world mutation operations.
/// </summary>
/// <remarks>
/// <para>
/// These tools allow spawning, despawning, and modifying entities and their
/// components at runtime. Use with caution as mutations can affect game state.
/// </para>
/// </remarks>
[McpServerToolType]
public sealed class MutationTools(BridgeConnectionManager connection)
{
    #region Entity Management

    [McpServerTool(Name = "mutation_spawn")]
    [Description("Spawn a new entity with an optional name and components.")]
    public async Task<MutationEntityResult> Spawn(
        [Description("Optional name for the entity.")]
        string? name = null)
    {
        var bridge = connection.GetBridge();
        var result = await bridge.Mutation.SpawnAsync(name);
        return MutationEntityResult.FromResult(result);
    }

    [McpServerTool(Name = "mutation_spawn_with_components")]
    [Description("Spawn a new entity with specified components. Component data is JSON.")]
    public async Task<MutationEntityResult> SpawnWithComponents(
        [Description("Optional name for the entity.")]
        string? name = null,
        [Description("JSON array of components. Each component has 'type' (component type name) and optional 'data' (JSON object with field values).")]
        string? componentsJson = null)
    {
        var bridge = connection.GetBridge();

        IReadOnlyList<ComponentData>? components = null;
        if (!string.IsNullOrEmpty(componentsJson))
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(componentsJson);
                var componentList = new List<ComponentData>();

                foreach (var element in jsonDoc.RootElement.EnumerateArray())
                {
                    var type = element.GetProperty("type").GetString()
                        ?? throw new InvalidOperationException("Component must have a 'type' property");

                    JsonElement? data = null;
                    if (element.TryGetProperty("data", out var dataElement))
                    {
                        data = dataElement;
                    }

                    componentList.Add(new ComponentData { Type = type, Data = data });
                }

                components = componentList;
            }
            catch (JsonException ex)
            {
                return new MutationEntityResult
                {
                    Success = false,
                    Error = $"Invalid components JSON: {ex.Message}"
                };
            }
        }

        var result = await bridge.Mutation.SpawnAsync(name, components);
        return MutationEntityResult.FromResult(result);
    }

    [McpServerTool(Name = "mutation_despawn")]
    [Description("Despawn (destroy) an entity.")]
    public async Task<MutationBoolResult> Despawn(
        [Description("The ID of the entity to despawn.")]
        int entityId)
    {
        var bridge = connection.GetBridge();
        var success = await bridge.Mutation.DespawnAsync(entityId);
        return new MutationBoolResult
        {
            Success = success,
            Error = success ? null : $"Failed to despawn entity {entityId}. Entity may not exist."
        };
    }

    [McpServerTool(Name = "mutation_clone")]
    [Description("Clone an existing entity, duplicating all its components and tags.")]
    public async Task<MutationEntityResult> Clone(
        [Description("The ID of the entity to clone.")]
        int entityId,
        [Description("Optional name for the cloned entity.")]
        string? name = null)
    {
        var bridge = connection.GetBridge();
        var result = await bridge.Mutation.CloneAsync(entityId, name);
        return MutationEntityResult.FromResult(result);
    }

    [McpServerTool(Name = "mutation_set_name")]
    [Description("Set the name of an entity.")]
    public async Task<MutationBoolResult> SetName(
        [Description("The ID of the entity to rename.")]
        int entityId,
        [Description("The new name for the entity.")]
        string name)
    {
        var bridge = connection.GetBridge();
        var success = await bridge.Mutation.SetNameAsync(entityId, name);
        return new MutationBoolResult
        {
            Success = success,
            Error = success ? null : $"Failed to set name for entity {entityId}. Entity may not exist or name may be taken."
        };
    }

    [McpServerTool(Name = "mutation_clear_name")]
    [Description("Clear (remove) the name from an entity.")]
    public async Task<MutationBoolResult> ClearName(
        [Description("The ID of the entity to clear the name from.")]
        int entityId)
    {
        var bridge = connection.GetBridge();
        var success = await bridge.Mutation.ClearNameAsync(entityId);
        return new MutationBoolResult
        {
            Success = success,
            Error = success ? null : $"Failed to clear name for entity {entityId}. Entity may not exist."
        };
    }

    #endregion

    #region Hierarchy

    [McpServerTool(Name = "mutation_set_parent")]
    [Description("Set the parent of an entity, or make it a root entity.")]
    public async Task<MutationBoolResult> SetParent(
        [Description("The ID of the entity to reparent.")]
        int entityId,
        [Description("The parent entity ID, or null to make this a root entity.")]
        int? parentId = null)
    {
        var bridge = connection.GetBridge();
        var success = await bridge.Mutation.SetParentAsync(entityId, parentId);
        return new MutationBoolResult
        {
            Success = success,
            Error = success ? null : $"Failed to set parent for entity {entityId}."
        };
    }

    [McpServerTool(Name = "mutation_get_root_entities")]
    [Description("Get all root entities (entities without parents).")]
    public async Task<MutationRootEntitiesResult> GetRootEntities()
    {
        var bridge = connection.GetBridge();
        var roots = await bridge.Mutation.GetRootEntitiesAsync();
        return new MutationRootEntitiesResult
        {
            Success = true,
            EntityIds = roots.ToArray()
        };
    }

    #endregion

    #region Components

    [McpServerTool(Name = "mutation_add_component")]
    [Description("Add a component to an entity.")]
    public async Task<MutationBoolResult> AddComponent(
        [Description("The ID of the entity to add the component to.")]
        int entityId,
        [Description("The component type name (e.g., 'Position', 'Velocity').")]
        string componentType,
        [Description("Optional JSON object with field values for the component.")]
        string? dataJson = null)
    {
        var bridge = connection.GetBridge();

        JsonElement? data = null;
        if (!string.IsNullOrEmpty(dataJson))
        {
            try
            {
                data = JsonDocument.Parse(dataJson).RootElement;
            }
            catch (JsonException ex)
            {
                return new MutationBoolResult
                {
                    Success = false,
                    Error = $"Invalid data JSON: {ex.Message}"
                };
            }
        }

        var success = await bridge.Mutation.AddComponentAsync(entityId, componentType, data);
        return new MutationBoolResult
        {
            Success = success,
            Error = success ? null : $"Failed to add component '{componentType}' to entity {entityId}. Entity may not exist, component type may be unknown, or entity may already have this component."
        };
    }

    [McpServerTool(Name = "mutation_remove_component")]
    [Description("Remove a component from an entity.")]
    public async Task<MutationBoolResult> RemoveComponent(
        [Description("The ID of the entity to remove the component from.")]
        int entityId,
        [Description("The component type name (e.g., 'Position', 'Velocity').")]
        string componentType)
    {
        var bridge = connection.GetBridge();
        var success = await bridge.Mutation.RemoveComponentAsync(entityId, componentType);
        return new MutationBoolResult
        {
            Success = success,
            Error = success ? null : $"Failed to remove component '{componentType}' from entity {entityId}. Entity or component may not exist."
        };
    }

    [McpServerTool(Name = "mutation_set_component")]
    [Description("Set all fields of a component on an entity.")]
    public async Task<MutationBoolResult> SetComponent(
        [Description("The ID of the entity.")]
        int entityId,
        [Description("The component type name (e.g., 'Position', 'Velocity').")]
        string componentType,
        [Description("JSON object with field values for the component.")]
        string dataJson)
    {
        var bridge = connection.GetBridge();

        JsonElement data;
        try
        {
            data = JsonDocument.Parse(dataJson).RootElement;
        }
        catch (JsonException ex)
        {
            return new MutationBoolResult
            {
                Success = false,
                Error = $"Invalid data JSON: {ex.Message}"
            };
        }

        var success = await bridge.Mutation.SetComponentAsync(entityId, componentType, data);
        return new MutationBoolResult
        {
            Success = success,
            Error = success ? null : $"Failed to set component '{componentType}' on entity {entityId}. Entity or component may not exist."
        };
    }

    [McpServerTool(Name = "mutation_set_field")]
    [Description("Set a single field of a component on an entity.")]
    public async Task<MutationBoolResult> SetField(
        [Description("The ID of the entity.")]
        int entityId,
        [Description("The component type name (e.g., 'Position', 'Velocity').")]
        string componentType,
        [Description("The name of the field to set.")]
        string fieldName,
        [Description("JSON value for the field (can be a number, string, boolean, or object).")]
        string valueJson)
    {
        var bridge = connection.GetBridge();

        JsonElement value;
        try
        {
            value = JsonDocument.Parse(valueJson).RootElement;
        }
        catch (JsonException ex)
        {
            return new MutationBoolResult
            {
                Success = false,
                Error = $"Invalid value JSON: {ex.Message}"
            };
        }

        var success = await bridge.Mutation.SetFieldAsync(entityId, componentType, fieldName, value);
        return new MutationBoolResult
        {
            Success = success,
            Error = success ? null : $"Failed to set field '{fieldName}' on component '{componentType}' for entity {entityId}."
        };
    }

    #endregion

    #region Tags

    [McpServerTool(Name = "mutation_add_tag")]
    [Description("Add a string tag to an entity.")]
    public async Task<MutationBoolResult> AddTag(
        [Description("The ID of the entity to add the tag to.")]
        int entityId,
        [Description("The tag to add.")]
        string tag)
    {
        var bridge = connection.GetBridge();
        var success = await bridge.Mutation.AddTagAsync(entityId, tag);
        return new MutationBoolResult
        {
            Success = success,
            Error = success ? null : $"Failed to add tag '{tag}' to entity {entityId}. Entity may not exist or may already have this tag."
        };
    }

    [McpServerTool(Name = "mutation_remove_tag")]
    [Description("Remove a string tag from an entity.")]
    public async Task<MutationBoolResult> RemoveTag(
        [Description("The ID of the entity to remove the tag from.")]
        int entityId,
        [Description("The tag to remove.")]
        string tag)
    {
        var bridge = connection.GetBridge();
        var success = await bridge.Mutation.RemoveTagAsync(entityId, tag);
        return new MutationBoolResult
        {
            Success = success,
            Error = success ? null : $"Failed to remove tag '{tag}' from entity {entityId}. Entity may not exist or may not have this tag."
        };
    }

    [McpServerTool(Name = "mutation_get_all_tags")]
    [Description("Get all unique tags currently in use across all entities.")]
    public async Task<MutationTagsResult> GetAllTags()
    {
        var bridge = connection.GetBridge();
        var tags = await bridge.Mutation.GetAllTagsAsync();
        return new MutationTagsResult
        {
            Success = true,
            Tags = tags.ToArray()
        };
    }

    #endregion
}

#region Result Records

/// <summary>
/// Result of an entity creation or cloning operation.
/// </summary>
public sealed record MutationEntityResult
{
    /// <summary>
    /// Gets whether the operation was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the entity ID if the operation succeeded.
    /// </summary>
    public int? EntityId { get; init; }

    /// <summary>
    /// Gets the entity version if the operation succeeded.
    /// </summary>
    public int? EntityVersion { get; init; }

    /// <summary>
    /// Gets an error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Creates a result from an EntityResult.
    /// </summary>
    public static MutationEntityResult FromResult(EntityResult result)
    {
        return new MutationEntityResult
        {
            Success = result.Success,
            EntityId = result.EntityId,
            EntityVersion = result.EntityVersion,
            Error = result.Error
        };
    }
}

/// <summary>
/// Result of a boolean mutation operation.
/// </summary>
public sealed record MutationBoolResult
{
    /// <summary>
    /// Gets whether the operation was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets an error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>
/// Result of getting root entities.
/// </summary>
public sealed record MutationRootEntitiesResult
{
    /// <summary>
    /// Gets whether the operation was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the entity IDs of all root entities.
    /// </summary>
    public int[] EntityIds { get; init; } = [];

    /// <summary>
    /// Gets an error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>
/// Result of getting all tags.
/// </summary>
public sealed record MutationTagsResult
{
    /// <summary>
    /// Gets whether the operation was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets all unique tags in use.
    /// </summary>
    public string[] Tags { get; init; } = [];

    /// <summary>
    /// Gets an error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }
}

#endregion
