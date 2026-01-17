using KeenEyes.Network;
using KeenEyes.Network.Components;
using KeenEyes.TestBridge.Network;

namespace KeenEyes.TestBridge.NetworkImpl;

/// <summary>
/// In-process implementation of <see cref="INetworkController"/>.
/// </summary>
internal sealed class NetworkControllerImpl(World world) : INetworkController
{
    #region Statistics

    /// <inheritdoc />
    public Task<NetworkStatisticsSnapshot> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        // Try to get network plugin (either server or client)
        var isServer = world.TryGetExtension<NetworkServerPlugin>(out var serverPlugin);
        var isClient = world.TryGetExtension<NetworkClientPlugin>(out var clientPlugin);

        if (!isServer && !isClient)
        {
            // No network plugin installed
            return Task.FromResult(new NetworkStatisticsSnapshot
            {
                IsConnected = false,
                IsServer = false,
                IsClient = false,
                CurrentTick = 0,
                LocalClientId = 0,
                ClientCount = 0,
                NetworkedEntityCount = 0
            });
        }

        // Count networked entities
        var networkedEntityCount = 0;
        foreach (var _ in world.Query<NetworkId>())
        {
            networkedEntityCount++;
        }

        if (isServer)
        {
            return Task.FromResult(new NetworkStatisticsSnapshot
            {
                IsConnected = true,
                IsServer = true,
                IsClient = false,
                CurrentTick = serverPlugin!.CurrentTick,
                LocalClientId = serverPlugin.LocalClientId,
                ClientCount = serverPlugin.ClientCount,
                NetworkedEntityCount = networkedEntityCount
            });
        }
        else
        {
            return Task.FromResult(new NetworkStatisticsSnapshot
            {
                IsConnected = clientPlugin!.IsConnected,
                IsServer = false,
                IsClient = true,
                CurrentTick = clientPlugin.CurrentTick,
                LocalClientId = clientPlugin.LocalClientId,
                ClientCount = 0,
                NetworkedEntityCount = networkedEntityCount
            });
        }
    }

    #endregion

    #region Connection State

    /// <inheritdoc />
    public Task<bool> IsConnectedAsync(CancellationToken cancellationToken = default)
    {
        if (world.TryGetExtension<NetworkServerPlugin>(out _))
        {
            return Task.FromResult(true); // Server is always "connected" when listening
        }

        if (world.TryGetExtension<NetworkClientPlugin>(out var clientPlugin))
        {
            return Task.FromResult(clientPlugin.IsConnected);
        }

        return Task.FromResult(false);
    }

    /// <inheritdoc />
    public Task<bool> IsServerAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(world.TryGetExtension<NetworkServerPlugin>(out _));
    }

    /// <inheritdoc />
    public Task<uint> GetCurrentTickAsync(CancellationToken cancellationToken = default)
    {
        if (world.TryGetExtension<NetworkServerPlugin>(out var serverPlugin))
        {
            return Task.FromResult(serverPlugin.CurrentTick);
        }

        if (world.TryGetExtension<NetworkClientPlugin>(out var clientPlugin))
        {
            return Task.FromResult(clientPlugin.CurrentTick);
        }

        return Task.FromResult(0u);
    }

    /// <inheritdoc />
    public Task<float> GetLatencyAsync(CancellationToken cancellationToken = default)
    {
        if (world.TryGetExtension<NetworkClientPlugin>(out var clientPlugin))
        {
            return Task.FromResult(clientPlugin.RoundTripTimeMs);
        }

        return Task.FromResult(0f);
    }

    /// <inheritdoc />
    public Task<ConnectionStatsSnapshot?> GetConnectionStatsAsync(CancellationToken cancellationToken = default)
    {
        if (!world.TryGetExtension<NetworkClientPlugin>(out var clientPlugin) || !clientPlugin.IsConnected)
        {
            return Task.FromResult<ConnectionStatsSnapshot?>(null);
        }

        // Get stats from transport if available (connectionId 0 = server connection for clients)
        var transport = clientPlugin.Transport;
        var stats = transport.GetStatistics(0);

        return Task.FromResult<ConnectionStatsSnapshot?>(new ConnectionStatsSnapshot
        {
            RoundTripTimeMs = clientPlugin.RoundTripTimeMs,
            PacketLossPercent = stats.PacketLossPercent,
            BytesSent = stats.BytesSent,
            BytesReceived = stats.BytesReceived,
            PacketsSent = stats.PacketsSent,
            PacketsReceived = stats.PacketsReceived
        });
    }

    #endregion

    #region Server Operations

    /// <inheritdoc />
    public Task<IReadOnlyList<ClientSnapshot>> GetConnectedClientsAsync(CancellationToken cancellationToken = default)
    {
        if (!world.TryGetExtension<NetworkServerPlugin>(out var serverPlugin))
        {
            return Task.FromResult<IReadOnlyList<ClientSnapshot>>([]);
        }

        var clients = new List<ClientSnapshot>();
        foreach (var clientState in serverPlugin.GetConnectedClients())
        {
            clients.Add(new ClientSnapshot
            {
                ClientId = clientState.ClientId,
                LastAckedTick = clientState.LastAckedTick,
                NeedsFullSnapshot = clientState.NeedsFullSnapshot,
                RoundTripTimeMs = clientState.RoundTripTimeMs
            });
        }

        return Task.FromResult<IReadOnlyList<ClientSnapshot>>(clients);
    }

    #endregion

    #region Entity Replication

    /// <inheritdoc />
    public Task<IReadOnlyList<int>> GetNetworkedEntitiesAsync(CancellationToken cancellationToken = default)
    {
        var entities = new List<int>();
        foreach (var entity in world.Query<NetworkId>())
        {
            entities.Add(entity.Id);
        }
        return Task.FromResult<IReadOnlyList<int>>(entities);
    }

    /// <inheritdoc />
    public Task<uint?> GetNetworkIdAsync(int entityId, CancellationToken cancellationToken = default)
    {
        var entity = new Entity(entityId, 0);
        if (!world.IsAlive(entity) || !world.Has<NetworkId>(entity))
        {
            return Task.FromResult<uint?>(null);
        }

        ref readonly var networkId = ref world.Get<NetworkId>(entity);
        return Task.FromResult<uint?>(networkId.Value);
    }

    /// <inheritdoc />
    public Task<int?> GetOwnershipAsync(int entityId, CancellationToken cancellationToken = default)
    {
        var entity = new Entity(entityId, 0);
        if (!world.IsAlive(entity) || !world.Has<NetworkOwner>(entity))
        {
            return Task.FromResult<int?>(null);
        }

        ref readonly var owner = ref world.Get<NetworkOwner>(entity);
        return Task.FromResult<int?>(owner.ClientId);
    }

    /// <inheritdoc />
    public Task<ReplicationStateSnapshot?> GetReplicationStateAsync(int entityId, CancellationToken cancellationToken = default)
    {
        var entity = new Entity(entityId, 0);
        if (!world.IsAlive(entity) || !world.Has<NetworkState>(entity))
        {
            return Task.FromResult<ReplicationStateSnapshot?>(null);
        }

        ref readonly var state = ref world.Get<NetworkState>(entity);
        ref readonly var networkId = ref world.Get<NetworkId>(entity);
        ref readonly var owner = ref world.Get<NetworkOwner>(entity);

        return Task.FromResult<ReplicationStateSnapshot?>(new ReplicationStateSnapshot
        {
            NetworkId = networkId.Value,
            OwnerId = owner.ClientId,
            LastSentTick = state.LastSentTick,
            LastReceivedTick = state.LastReceivedTick,
            NeedsFullSync = state.NeedsFullSync,
            IsLocallyOwned = world.Has<LocallyOwned>(entity),
            IsRemotelyOwned = world.Has<RemotelyOwned>(entity),
            IsPredicted = world.Has<Predicted>(entity),
            IsInterpolated = world.Has<Interpolated>(entity)
        });
    }

    #endregion
}
