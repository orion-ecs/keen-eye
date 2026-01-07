using System.Text.Json;
using KeenEyes.TestBridge.Ipc.Protocol;
using KeenEyes.TestBridge.State;

namespace KeenEyes.TestBridge.Ipc.Handlers;

/// <summary>
/// Handles state query commands.
/// </summary>
internal sealed class StateCommandHandler(IStateController stateController) : ICommandHandler
{
    public string Prefix => "state";

    public async ValueTask<object?> HandleAsync(string command, JsonElement? args, CancellationToken cancellationToken)
    {
        return command switch
        {
            "getEntityCount" => await stateController.GetEntityCountAsync(),
            "queryEntities" => await HandleQueryEntitiesAsync(args),
            "getEntity" => await HandleGetEntityAsync(args),
            "getEntityByName" => await HandleGetEntityByNameAsync(args),
            "getComponent" => await HandleGetComponentAsync(args),
            "getWorldStats" => await stateController.GetWorldStatsAsync(),
            "getSystems" => await stateController.GetSystemsAsync(),
            "getPerformanceMetrics" => await HandleGetPerformanceMetricsAsync(args),
            "getEntitiesWithTag" => await HandleGetEntitiesWithTagAsync(args),
            "getChildren" => await HandleGetChildrenAsync(args),
            "getParent" => await HandleGetParentAsync(args),
            "getExtension" => await HandleGetExtensionAsync(args),
            "hasExtension" => await HandleHasExtensionAsync(args),
            _ => throw new InvalidOperationException($"Unknown state command: {command}")
        };
    }

    private async Task<object?> HandleQueryEntitiesAsync(JsonElement? args)
    {
        var query = args.HasValue
            ? args.Value.Deserialize(IpcJsonContext.Default.EntityQuery) ?? new EntityQuery()
            : new EntityQuery();
        return await stateController.QueryEntitiesAsync(query);
    }

    private async Task<object?> HandleGetEntityAsync(JsonElement? args)
    {
        var entityId = GetRequiredInt(args, "entityId");
        return await stateController.GetEntityAsync(entityId);
    }

    private async Task<object?> HandleGetEntityByNameAsync(JsonElement? args)
    {
        var name = GetRequiredString(args, "name");
        return await stateController.GetEntityByNameAsync(name);
    }

    private async Task<object?> HandleGetComponentAsync(JsonElement? args)
    {
        var entityId = GetRequiredInt(args, "entityId");
        var componentTypeName = GetRequiredString(args, "componentTypeName");
        return await stateController.GetComponentAsync(entityId, componentTypeName);
    }

    private async Task<object?> HandleGetPerformanceMetricsAsync(JsonElement? args)
    {
        var frameCount = GetOptionalInt(args, "frameCount") ?? 60;
        return await stateController.GetPerformanceMetricsAsync(frameCount);
    }

    private async Task<object?> HandleGetEntitiesWithTagAsync(JsonElement? args)
    {
        var tag = GetRequiredString(args, "tag");
        return await stateController.GetEntitiesWithTagAsync(tag);
    }

    private async Task<object?> HandleGetChildrenAsync(JsonElement? args)
    {
        var parentId = GetRequiredInt(args, "parentId");
        return await stateController.GetChildrenAsync(parentId);
    }

    private async Task<object?> HandleGetParentAsync(JsonElement? args)
    {
        var entityId = GetRequiredInt(args, "entityId");
        return await stateController.GetParentAsync(entityId);
    }

    private async Task<object?> HandleGetExtensionAsync(JsonElement? args)
    {
        var typeName = GetRequiredString(args, "typeName");
        return await stateController.GetExtensionAsync(typeName);
    }

    private async Task<object?> HandleHasExtensionAsync(JsonElement? args)
    {
        var typeName = GetRequiredString(args, "typeName");
        return await stateController.HasExtensionAsync(typeName);
    }

    #region Typed Argument Helpers (AOT-compatible)

    private static string GetRequiredString(JsonElement? args, string name)
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            throw new ArgumentException($"Missing required argument: {name}");
        }

        return prop.GetString() ?? throw new ArgumentException($"Invalid value for argument: {name}");
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

    #endregion
}
