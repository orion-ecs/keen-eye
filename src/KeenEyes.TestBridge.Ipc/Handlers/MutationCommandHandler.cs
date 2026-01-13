using System.Text.Json;
using KeenEyes.TestBridge.Ipc.Protocol;
using KeenEyes.TestBridge.Mutation;

namespace KeenEyes.TestBridge.Ipc.Handlers;

/// <summary>
/// Handles world mutation commands for entity and component manipulation.
/// </summary>
internal sealed class MutationCommandHandler(IMutationController mutationController) : ICommandHandler
{
    public string Prefix => "mutation";

    public async ValueTask<object?> HandleAsync(string command, JsonElement? args, CancellationToken cancellationToken)
    {
        return command switch
        {
            // Entity management
            "spawn" => await HandleSpawnAsync(args, cancellationToken),
            "despawn" => await HandleDespawnAsync(args, cancellationToken),
            "clone" => await HandleCloneAsync(args, cancellationToken),
            "setName" => await HandleSetNameAsync(args, cancellationToken),
            "clearName" => await HandleClearNameAsync(args, cancellationToken),

            // Hierarchy
            "setParent" => await HandleSetParentAsync(args, cancellationToken),
            "getRootEntities" => await mutationController.GetRootEntitiesAsync(cancellationToken),

            // Components
            "addComponent" => await HandleAddComponentAsync(args, cancellationToken),
            "removeComponent" => await HandleRemoveComponentAsync(args, cancellationToken),
            "setComponent" => await HandleSetComponentAsync(args, cancellationToken),
            "setField" => await HandleSetFieldAsync(args, cancellationToken),

            // Tags
            "addTag" => await HandleAddTagAsync(args, cancellationToken),
            "removeTag" => await HandleRemoveTagAsync(args, cancellationToken),
            "getAllTags" => await mutationController.GetAllTagsAsync(cancellationToken),

            _ => throw new InvalidOperationException($"Unknown mutation command: {command}")
        };
    }

    #region Entity Management

    private async Task<EntityResult> HandleSpawnAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var name = GetOptionalString(args, "name");
        var components = GetOptionalComponentDataList(args, "components");

        return await mutationController.SpawnAsync(name, components, cancellationToken);
    }

    private async Task<bool> HandleDespawnAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        return await mutationController.DespawnAsync(entityId, cancellationToken);
    }

    private async Task<EntityResult> HandleCloneAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        var name = GetOptionalString(args, "name");

        return await mutationController.CloneAsync(entityId, name, cancellationToken);
    }

    private async Task<bool> HandleSetNameAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        var name = GetRequiredString(args, "name");

        return await mutationController.SetNameAsync(entityId, name, cancellationToken);
    }

    private async Task<bool> HandleClearNameAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        return await mutationController.ClearNameAsync(entityId, cancellationToken);
    }

    #endregion

    #region Hierarchy

    private async Task<bool> HandleSetParentAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        var parentId = GetOptionalInt(args, "parentId");

        return await mutationController.SetParentAsync(entityId, parentId, cancellationToken);
    }

    #endregion

    #region Components

    private async Task<bool> HandleAddComponentAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        var componentType = GetRequiredString(args, "componentType");
        var data = GetOptionalJsonElement(args, "data");

        return await mutationController.AddComponentAsync(entityId, componentType, data, cancellationToken);
    }

    private async Task<bool> HandleRemoveComponentAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        var componentType = GetRequiredString(args, "componentType");

        return await mutationController.RemoveComponentAsync(entityId, componentType, cancellationToken);
    }

    private async Task<bool> HandleSetComponentAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        var componentType = GetRequiredString(args, "componentType");
        var data = GetRequiredJsonElement(args, "data");

        return await mutationController.SetComponentAsync(entityId, componentType, data, cancellationToken);
    }

    private async Task<bool> HandleSetFieldAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        var componentType = GetRequiredString(args, "componentType");
        var fieldName = GetRequiredString(args, "fieldName");
        var value = GetRequiredJsonElement(args, "value");

        return await mutationController.SetFieldAsync(entityId, componentType, fieldName, value, cancellationToken);
    }

    #endregion

    #region Tags

    private async Task<bool> HandleAddTagAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        var tag = GetRequiredString(args, "tag");

        return await mutationController.AddTagAsync(entityId, tag, cancellationToken);
    }

    private async Task<bool> HandleRemoveTagAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        var tag = GetRequiredString(args, "tag");

        return await mutationController.RemoveTagAsync(entityId, tag, cancellationToken);
    }

    #endregion

    #region Typed Argument Helpers (AOT-compatible)

    private static string GetRequiredString(JsonElement? args, string name)
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            throw new ArgumentException($"Missing required argument: {name}");
        }

        return prop.GetString() ?? throw new ArgumentException($"Invalid value for argument: {name}");
    }

    private static string? GetOptionalString(JsonElement? args, string name)
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            return null;
        }

        return prop.ValueKind == JsonValueKind.Null ? null : prop.GetString();
    }

    private static int GetRequiredInt(JsonElement? args, string name)
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            throw new ArgumentException($"Missing required argument: {name}");
        }

        return prop.GetInt32();
    }

    private static int? GetOptionalInt(JsonElement? args, string name)
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            return null;
        }

        return prop.ValueKind == JsonValueKind.Null ? null : prop.GetInt32();
    }

    private static JsonElement GetRequiredJsonElement(JsonElement? args, string name)
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            throw new ArgumentException($"Missing required argument: {name}");
        }

        return prop;
    }

    private static JsonElement? GetOptionalJsonElement(JsonElement? args, string name)
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            return null;
        }

        return prop.ValueKind == JsonValueKind.Null ? null : prop;
    }

    private static IReadOnlyList<ComponentData>? GetOptionalComponentDataList(JsonElement? args, string name)
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            return null;
        }

        if (prop.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return prop.Deserialize(IpcJsonContext.Default.IReadOnlyListComponentData);
    }

    #endregion
}
