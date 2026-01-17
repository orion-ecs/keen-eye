namespace KeenEyes.TestBridge.Network;

/// <summary>
/// Statistics about network state and connections.
/// </summary>
public sealed record NetworkStatisticsSnapshot
{
    /// <summary>
    /// Gets whether the network plugin is connected.
    /// </summary>
    public required bool IsConnected { get; init; }

    /// <summary>
    /// Gets whether this is a server instance.
    /// </summary>
    public required bool IsServer { get; init; }

    /// <summary>
    /// Gets whether this is a client instance.
    /// </summary>
    public required bool IsClient { get; init; }

    /// <summary>
    /// Gets the current network tick.
    /// </summary>
    public required uint CurrentTick { get; init; }

    /// <summary>
    /// Gets the local client ID (0 for server, assigned ID for clients).
    /// </summary>
    public required int LocalClientId { get; init; }

    /// <summary>
    /// Gets the number of connected clients (server only, 0 for clients).
    /// </summary>
    public required int ClientCount { get; init; }

    /// <summary>
    /// Gets the number of networked entities.
    /// </summary>
    public required int NetworkedEntityCount { get; init; }
}
