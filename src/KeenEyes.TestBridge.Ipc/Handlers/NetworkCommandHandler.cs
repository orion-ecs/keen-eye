using System.Text.Json;
using KeenEyes.TestBridge.Network;

namespace KeenEyes.TestBridge.Ipc.Handlers;

/// <summary>
/// Handles network debugging commands for connection state, replication, and statistics.
/// </summary>
internal sealed class NetworkCommandHandler(INetworkController networkController) : ICommandHandler
{
    public string Prefix => "network";

    public async ValueTask<object?> HandleAsync(string command, JsonElement? args, CancellationToken cancellationToken)
    {
        return command switch
        {
            // Statistics
            "getStatistics" => await networkController.GetStatisticsAsync(cancellationToken),

            // Connection State
            "isConnected" => await networkController.IsConnectedAsync(cancellationToken),
            "isServer" => await networkController.IsServerAsync(cancellationToken),
            "getCurrentTick" => await networkController.GetCurrentTickAsync(cancellationToken),
            "getLatency" => await networkController.GetLatencyAsync(cancellationToken),
            "getConnectionStats" => await networkController.GetConnectionStatsAsync(cancellationToken),

            // Server Operations
            "getConnectedClients" => await networkController.GetConnectedClientsAsync(cancellationToken),

            // Entity Replication
            "getNetworkedEntities" => await networkController.GetNetworkedEntitiesAsync(cancellationToken),
            "getNetworkId" => await HandleGetNetworkIdAsync(args, cancellationToken),
            "getOwnership" => await HandleGetOwnershipAsync(args, cancellationToken),
            "getReplicationState" => await HandleGetReplicationStateAsync(args, cancellationToken),

            _ => throw new InvalidOperationException($"Unknown network command: {command}")
        };
    }

    #region Entity Replication Handlers

    private async Task<uint?> HandleGetNetworkIdAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        return await networkController.GetNetworkIdAsync(entityId, cancellationToken);
    }

    private async Task<int?> HandleGetOwnershipAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        return await networkController.GetOwnershipAsync(entityId, cancellationToken);
    }

    private async Task<ReplicationStateSnapshot?> HandleGetReplicationStateAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        return await networkController.GetReplicationStateAsync(entityId, cancellationToken);
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

    #endregion
}
