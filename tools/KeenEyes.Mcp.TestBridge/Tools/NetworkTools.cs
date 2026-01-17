using System.ComponentModel;
using KeenEyes.Mcp.TestBridge.Connection;
using KeenEyes.TestBridge.Network;
using ModelContextProtocol.Server;

namespace KeenEyes.Mcp.TestBridge.Tools;

/// <summary>
/// MCP tools for network debugging: connection state, replication, and statistics.
/// </summary>
/// <remarks>
/// <para>
/// These tools expose network plugin debugging infrastructure via MCP, allowing inspection
/// of networked entities, ownership, and replication state in running games.
/// </para>
/// <para>
/// Note: These tools require either NetworkClientPlugin or NetworkServerPlugin to be installed
/// in the target world.
/// </para>
/// </remarks>
[McpServerToolType]
public sealed class NetworkTools(BridgeConnectionManager connection)
{
    #region Statistics

    [McpServerTool(Name = "network_get_statistics")]
    [Description("Get network statistics including connection state, tick, client count, and entity count.")]
    public async Task<NetworkStatisticsResult> GetStatistics()
    {
        var bridge = connection.GetBridge();
        var stats = await bridge.Network.GetStatisticsAsync();
        return NetworkStatisticsResult.FromSnapshot(stats);
    }

    #endregion

    #region Connection State

    [McpServerTool(Name = "network_is_connected")]
    [Description("Check if the network plugin is connected (client connected to server, or server listening).")]
    public async Task<NetworkBoolResult> IsConnected()
    {
        var bridge = connection.GetBridge();
        var isConnected = await bridge.Network.IsConnectedAsync();
        return new NetworkBoolResult
        {
            Success = true,
            Value = isConnected
        };
    }

    [McpServerTool(Name = "network_is_server")]
    [Description("Check if this is a server instance.")]
    public async Task<NetworkBoolResult> IsServer()
    {
        var bridge = connection.GetBridge();
        var isServer = await bridge.Network.IsServerAsync();
        return new NetworkBoolResult
        {
            Success = true,
            Value = isServer
        };
    }

    [McpServerTool(Name = "network_get_tick")]
    [Description("Get the current network tick.")]
    public async Task<NetworkUIntResult> GetCurrentTick()
    {
        var bridge = connection.GetBridge();
        var tick = await bridge.Network.GetCurrentTickAsync();
        return new NetworkUIntResult
        {
            Success = true,
            Value = tick
        };
    }

    [McpServerTool(Name = "network_get_latency")]
    [Description("Get the round-trip latency to the server in milliseconds (client only).")]
    public async Task<NetworkFloatResult> GetLatency()
    {
        var bridge = connection.GetBridge();
        var latency = await bridge.Network.GetLatencyAsync();
        return new NetworkFloatResult
        {
            Success = true,
            Value = latency
        };
    }

    [McpServerTool(Name = "network_get_connection_stats")]
    [Description("Get detailed connection statistics including RTT, packet loss, and bandwidth (client only).")]
    public async Task<ConnectionStatsResult> GetConnectionStats()
    {
        var bridge = connection.GetBridge();
        var stats = await bridge.Network.GetConnectionStatsAsync();

        if (stats == null)
        {
            return new ConnectionStatsResult
            {
                Success = false,
                Error = "Not a client or not connected"
            };
        }

        return ConnectionStatsResult.FromSnapshot(stats);
    }

    #endregion

    #region Server Operations

    [McpServerTool(Name = "network_get_clients")]
    [Description("Get all connected clients (server only).")]
    public async Task<ConnectedClientsResult> GetConnectedClients()
    {
        var bridge = connection.GetBridge();
        var clients = await bridge.Network.GetConnectedClientsAsync();
        return ConnectedClientsResult.FromClients(clients);
    }

    #endregion

    #region Entity Replication

    [McpServerTool(Name = "network_list_networked_entities")]
    [Description("List all entity IDs that have a NetworkId component.")]
    public async Task<EntityListResult> GetNetworkedEntities()
    {
        var bridge = connection.GetBridge();
        var entities = await bridge.Network.GetNetworkedEntitiesAsync();
        return new EntityListResult
        {
            Success = true,
            EntityIds = entities,
            Count = entities.Count
        };
    }

    [McpServerTool(Name = "network_get_network_id")]
    [Description("Get the network ID assigned to an entity.")]
    public async Task<NetworkUIntNullableResult> GetNetworkId(
        [Description("The entity ID to query")]
        int entityId)
    {
        var bridge = connection.GetBridge();
        var networkId = await bridge.Network.GetNetworkIdAsync(entityId);

        if (networkId == null)
        {
            return new NetworkUIntNullableResult
            {
                Success = false,
                Error = $"Entity {entityId} has no network ID"
            };
        }

        return new NetworkUIntNullableResult
        {
            Success = true,
            Value = networkId
        };
    }

    [McpServerTool(Name = "network_get_ownership")]
    [Description("Get the owner client ID for a networked entity (0 for server-owned).")]
    public async Task<NetworkIntNullableResult> GetOwnership(
        [Description("The entity ID to query")]
        int entityId)
    {
        var bridge = connection.GetBridge();
        var ownerId = await bridge.Network.GetOwnershipAsync(entityId);

        if (ownerId == null)
        {
            return new NetworkIntNullableResult
            {
                Success = false,
                Error = $"Entity {entityId} has no network owner"
            };
        }

        return new NetworkIntNullableResult
        {
            Success = true,
            Value = ownerId
        };
    }

    [McpServerTool(Name = "network_get_replication_state")]
    [Description("Get the full replication state for a networked entity, including ownership, ticks, and prediction/interpolation flags.")]
    public async Task<ReplicationStateResult> GetReplicationState(
        [Description("The entity ID to query")]
        int entityId)
    {
        var bridge = connection.GetBridge();
        var state = await bridge.Network.GetReplicationStateAsync(entityId);

        if (state == null)
        {
            return new ReplicationStateResult
            {
                Success = false,
                Error = $"Entity {entityId} has no replication state"
            };
        }

        return ReplicationStateResult.FromSnapshot(state);
    }

    #endregion
}

#region Result Types

/// <summary>
/// Result for network statistics query.
/// </summary>
public sealed record NetworkStatisticsResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public bool IsConnected { get; init; }
    public bool IsServer { get; init; }
    public bool IsClient { get; init; }
    public uint CurrentTick { get; init; }
    public int LocalClientId { get; init; }
    public int ClientCount { get; init; }
    public int NetworkedEntityCount { get; init; }

    public static NetworkStatisticsResult FromSnapshot(NetworkStatisticsSnapshot snapshot) => new()
    {
        Success = true,
        IsConnected = snapshot.IsConnected,
        IsServer = snapshot.IsServer,
        IsClient = snapshot.IsClient,
        CurrentTick = snapshot.CurrentTick,
        LocalClientId = snapshot.LocalClientId,
        ClientCount = snapshot.ClientCount,
        NetworkedEntityCount = snapshot.NetworkedEntityCount
    };
}

/// <summary>
/// Result for connection statistics query.
/// </summary>
public sealed record ConnectionStatsResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public float RoundTripTimeMs { get; init; }
    public float PacketLossPercent { get; init; }
    public long BytesSent { get; init; }
    public long BytesReceived { get; init; }
    public long PacketsSent { get; init; }
    public long PacketsReceived { get; init; }

    public static ConnectionStatsResult FromSnapshot(ConnectionStatsSnapshot snapshot) => new()
    {
        Success = true,
        RoundTripTimeMs = snapshot.RoundTripTimeMs,
        PacketLossPercent = snapshot.PacketLossPercent,
        BytesSent = snapshot.BytesSent,
        BytesReceived = snapshot.BytesReceived,
        PacketsSent = snapshot.PacketsSent,
        PacketsReceived = snapshot.PacketsReceived
    };
}

/// <summary>
/// Result for connected clients query.
/// </summary>
public sealed record ConnectedClientsResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public IReadOnlyList<ClientInfo> Clients { get; init; } = [];
    public int Count { get; init; }

    public static ConnectedClientsResult FromClients(IReadOnlyList<ClientSnapshot> clients) => new()
    {
        Success = true,
        Clients = clients.Select(c => new ClientInfo
        {
            ClientId = c.ClientId,
            LastAckedTick = c.LastAckedTick,
            NeedsFullSnapshot = c.NeedsFullSnapshot,
            RoundTripTimeMs = c.RoundTripTimeMs
        }).ToList(),
        Count = clients.Count
    };
}

/// <summary>
/// Information about a connected client.
/// </summary>
public sealed record ClientInfo
{
    public int ClientId { get; init; }
    public uint LastAckedTick { get; init; }
    public bool NeedsFullSnapshot { get; init; }
    public float RoundTripTimeMs { get; init; }
}

/// <summary>
/// Result for replication state query.
/// </summary>
public sealed record ReplicationStateResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public uint NetworkId { get; init; }
    public int OwnerId { get; init; }
    public uint LastSentTick { get; init; }
    public uint LastReceivedTick { get; init; }
    public bool NeedsFullSync { get; init; }
    public bool IsLocallyOwned { get; init; }
    public bool IsRemotelyOwned { get; init; }
    public bool IsPredicted { get; init; }
    public bool IsInterpolated { get; init; }

    public static ReplicationStateResult FromSnapshot(ReplicationStateSnapshot snapshot) => new()
    {
        Success = true,
        NetworkId = snapshot.NetworkId,
        OwnerId = snapshot.OwnerId,
        LastSentTick = snapshot.LastSentTick,
        LastReceivedTick = snapshot.LastReceivedTick,
        NeedsFullSync = snapshot.NeedsFullSync,
        IsLocallyOwned = snapshot.IsLocallyOwned,
        IsRemotelyOwned = snapshot.IsRemotelyOwned,
        IsPredicted = snapshot.IsPredicted,
        IsInterpolated = snapshot.IsInterpolated
    };
}

/// <summary>
/// Result for boolean operations.
/// </summary>
public sealed record NetworkBoolResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public bool Value { get; init; }
}

/// <summary>
/// Result for unsigned integer operations.
/// </summary>
public sealed record NetworkUIntResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public uint Value { get; init; }
}

/// <summary>
/// Result for float operations.
/// </summary>
public sealed record NetworkFloatResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public float Value { get; init; }
}

/// <summary>
/// Result for nullable unsigned integer operations.
/// </summary>
public sealed record NetworkUIntNullableResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public uint? Value { get; init; }
}

/// <summary>
/// Result for nullable integer operations.
/// </summary>
public sealed record NetworkIntNullableResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public int? Value { get; init; }
}

#endregion
