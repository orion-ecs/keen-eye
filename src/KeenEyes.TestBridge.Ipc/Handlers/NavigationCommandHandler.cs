using System.Text.Json;
using KeenEyes.TestBridge.Navigation;

namespace KeenEyes.TestBridge.Ipc.Handlers;

/// <summary>
/// Handles navigation debugging commands for agents, paths, and navmesh queries.
/// </summary>
internal sealed class NavigationCommandHandler(INavigationController navigationController) : ICommandHandler
{
    public string Prefix => "navigation";

    public async ValueTask<object?> HandleAsync(string command, JsonElement? args, CancellationToken cancellationToken)
    {
        return command switch
        {
            // Statistics
            "getStatistics" => await navigationController.GetStatisticsAsync(cancellationToken),
            "isReady" => await navigationController.IsReadyAsync(cancellationToken),

            // Agent operations
            "getNavigationEntities" => await navigationController.GetNavigationEntitiesAsync(cancellationToken),
            "getAgentState" => await HandleGetAgentStateAsync(args, cancellationToken),
            "getPath" => await HandleGetPathAsync(args, cancellationToken),
            "setDestination" => await HandleSetDestinationAsync(args, cancellationToken),
            "stopAgent" => await HandleStopAgentAsync(args, cancellationToken),
            "resumeAgent" => await HandleResumeAgentAsync(args, cancellationToken),
            "warpAgent" => await HandleWarpAgentAsync(args, cancellationToken),

            // Path queries
            "findPath" => await HandleFindPathAsync(args, cancellationToken),
            "isNavigable" => await HandleIsNavigableAsync(args, cancellationToken),
            "findNearestPoint" => await HandleFindNearestPointAsync(args, cancellationToken),

            _ => throw new InvalidOperationException($"Unknown navigation command: {command}")
        };
    }

    #region Agent Handlers

    private async Task<NavAgentSnapshot?> HandleGetAgentStateAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        return await navigationController.GetAgentStateAsync(entityId, cancellationToken);
    }

    private async Task<NavPathSnapshot?> HandleGetPathAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        return await navigationController.GetPathAsync(entityId, cancellationToken);
    }

    private async Task<bool> HandleSetDestinationAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        var x = GetRequiredFloat(args, "x");
        var y = GetRequiredFloat(args, "y");
        var z = GetRequiredFloat(args, "z");
        return await navigationController.SetDestinationAsync(entityId, x, y, z, cancellationToken);
    }

    private async Task<bool> HandleStopAgentAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        return await navigationController.StopAgentAsync(entityId, cancellationToken);
    }

    private async Task<bool> HandleResumeAgentAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        return await navigationController.ResumeAgentAsync(entityId, cancellationToken);
    }

    private async Task<bool> HandleWarpAgentAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        var x = GetRequiredFloat(args, "x");
        var y = GetRequiredFloat(args, "y");
        var z = GetRequiredFloat(args, "z");
        return await navigationController.WarpAgentAsync(entityId, x, y, z, cancellationToken);
    }

    #endregion

    #region Path Query Handlers

    private async Task<NavPathSnapshot?> HandleFindPathAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var startX = GetRequiredFloat(args, "startX");
        var startY = GetRequiredFloat(args, "startY");
        var startZ = GetRequiredFloat(args, "startZ");
        var endX = GetRequiredFloat(args, "endX");
        var endY = GetRequiredFloat(args, "endY");
        var endZ = GetRequiredFloat(args, "endZ");
        return await navigationController.FindPathAsync(startX, startY, startZ, endX, endY, endZ, cancellationToken);
    }

    private async Task<bool> HandleIsNavigableAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var x = GetRequiredFloat(args, "x");
        var y = GetRequiredFloat(args, "y");
        var z = GetRequiredFloat(args, "z");
        return await navigationController.IsNavigableAsync(x, y, z, cancellationToken);
    }

    private async Task<NavPointSnapshot?> HandleFindNearestPointAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var x = GetRequiredFloat(args, "x");
        var y = GetRequiredFloat(args, "y");
        var z = GetRequiredFloat(args, "z");
        var searchRadius = GetRequiredFloat(args, "searchRadius");
        return await navigationController.FindNearestPointAsync(x, y, z, searchRadius, cancellationToken);
    }

    #endregion

    #region Typed Argument Helpers (AOT-compatible)

    private static int GetRequiredInt(JsonElement? args, string name)
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            throw new ArgumentException($"Missing required argument: {name}");
        }

        return prop.GetInt32();
    }

    private static float GetRequiredFloat(JsonElement? args, string name)
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            throw new ArgumentException($"Missing required argument: {name}");
        }

        return (float)prop.GetDouble();
    }

    #endregion
}
