using KeenEyes.TestBridge.Network;

namespace KeenEyes.TestBridge.Client;

/// <summary>
/// Remote implementation of <see cref="INetworkController"/> that communicates over IPC.
/// </summary>
internal sealed class RemoteNetworkController(TestBridgeClient client) : INetworkController
{
    #region Statistics

    /// <inheritdoc />
    public async Task<NetworkStatisticsSnapshot> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<NetworkStatisticsSnapshot>(
            "network.getStatistics",
            null,
            cancellationToken) ?? new NetworkStatisticsSnapshot
            {
                IsConnected = false,
                IsServer = false,
                IsClient = false,
                CurrentTick = 0,
                LocalClientId = 0,
                ClientCount = 0,
                NetworkedEntityCount = 0
            };
    }

    #endregion

    #region Connection State

    /// <inheritdoc />
    public async Task<bool> IsConnectedAsync(CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<bool>(
            "network.isConnected",
            null,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> IsServerAsync(CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<bool>(
            "network.isServer",
            null,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<uint> GetCurrentTickAsync(CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<uint>(
            "network.getCurrentTick",
            null,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<float> GetLatencyAsync(CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<float>(
            "network.getLatency",
            null,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ConnectionStatsSnapshot?> GetConnectionStatsAsync(CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<ConnectionStatsSnapshot?>(
            "network.getConnectionStats",
            null,
            cancellationToken);
    }

    #endregion

    #region Server Operations

    /// <inheritdoc />
    public async Task<IReadOnlyList<ClientSnapshot>> GetConnectedClientsAsync(CancellationToken cancellationToken = default)
    {
        var result = await client.SendRequestAsync<ClientSnapshot[]>(
            "network.getConnectedClients",
            null,
            cancellationToken);
        return result ?? [];
    }

    #endregion

    #region Entity Replication

    /// <inheritdoc />
    public async Task<IReadOnlyList<int>> GetNetworkedEntitiesAsync(CancellationToken cancellationToken = default)
    {
        var result = await client.SendRequestAsync<int[]>(
            "network.getNetworkedEntities",
            null,
            cancellationToken);
        return result ?? [];
    }

    /// <inheritdoc />
    public async Task<uint?> GetNetworkIdAsync(int entityId, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<uint?>(
            "network.getNetworkId",
            new { entityId },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int?> GetOwnershipAsync(int entityId, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<int?>(
            "network.getOwnership",
            new { entityId },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ReplicationStateSnapshot?> GetReplicationStateAsync(int entityId, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<ReplicationStateSnapshot?>(
            "network.getReplicationState",
            new { entityId },
            cancellationToken);
    }

    #endregion
}
