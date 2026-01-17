namespace KeenEyes.TestBridge.Network;

/// <summary>
/// Controller interface for network debugging and inspection operations.
/// </summary>
/// <remarks>
/// <para>
/// This controller provides access to network statistics, connection state,
/// and entity replication data for both client and server network plugins.
/// It enables inspection of networked entities, ownership, and replication state.
/// </para>
/// <para>
/// <strong>Note:</strong> Requires either NetworkClientPlugin or NetworkServerPlugin
/// to be installed on the world for full functionality.
/// </para>
/// </remarks>
public interface INetworkController
{
    #region Statistics

    /// <summary>
    /// Gets statistics about network state and connections.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Network statistics snapshot.</returns>
    Task<NetworkStatisticsSnapshot> GetStatisticsAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Connection State

    /// <summary>
    /// Gets whether the network plugin is connected (client connected to server, or server listening).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if connected; false otherwise.</returns>
    Task<bool> IsConnectedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets whether this is a server instance.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if server; false if client or no network plugin.</returns>
    Task<bool> IsServerAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current network tick.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current tick value.</returns>
    Task<uint> GetCurrentTickAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the round-trip latency to the server in milliseconds (client only).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Round-trip time in milliseconds, or 0 if not a client or not connected.</returns>
    Task<float> GetLatencyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed connection statistics for the local connection (client only).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Connection statistics, or null if not a client or not connected.</returns>
    Task<ConnectionStatsSnapshot?> GetConnectionStatsAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Server Operations

    /// <summary>
    /// Gets all connected clients (server only).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of connected client snapshots, or empty list if not a server.</returns>
    Task<IReadOnlyList<ClientSnapshot>> GetConnectedClientsAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Entity Replication

    /// <summary>
    /// Gets all networked entity IDs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of entity IDs that have NetworkId components.</returns>
    Task<IReadOnlyList<int>> GetNetworkedEntitiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the network ID for an entity.
    /// </summary>
    /// <param name="entityId">The entity ID to query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The network ID, or null if the entity has no network ID.</returns>
    Task<uint?> GetNetworkIdAsync(int entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the owner client ID for a networked entity.
    /// </summary>
    /// <param name="entityId">The entity ID to query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The owner client ID (0 for server), or null if the entity has no NetworkOwner component.</returns>
    Task<int?> GetOwnershipAsync(int entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the replication state for a networked entity.
    /// </summary>
    /// <param name="entityId">The entity ID to query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Replication state snapshot, or null if the entity has no NetworkState component.</returns>
    Task<ReplicationStateSnapshot?> GetReplicationStateAsync(int entityId, CancellationToken cancellationToken = default);

    #endregion
}
