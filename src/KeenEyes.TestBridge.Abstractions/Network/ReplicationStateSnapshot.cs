namespace KeenEyes.TestBridge.Network;

/// <summary>
/// Snapshot of an entity's network replication state.
/// </summary>
public sealed record ReplicationStateSnapshot
{
    /// <summary>
    /// Gets the network ID assigned to this entity.
    /// </summary>
    public required uint NetworkId { get; init; }

    /// <summary>
    /// Gets the owner client ID (0 for server-owned).
    /// </summary>
    public required int OwnerId { get; init; }

    /// <summary>
    /// Gets the last tick this entity was sent to clients (server only).
    /// </summary>
    public required uint LastSentTick { get; init; }

    /// <summary>
    /// Gets the last tick received from the server (client only).
    /// </summary>
    public required uint LastReceivedTick { get; init; }

    /// <summary>
    /// Gets whether this entity needs a full synchronization.
    /// </summary>
    public required bool NeedsFullSync { get; init; }

    /// <summary>
    /// Gets whether this entity is locally owned.
    /// </summary>
    public required bool IsLocallyOwned { get; init; }

    /// <summary>
    /// Gets whether this entity is remotely owned.
    /// </summary>
    public required bool IsRemotelyOwned { get; init; }

    /// <summary>
    /// Gets whether this entity is predicted (client-side prediction enabled).
    /// </summary>
    public required bool IsPredicted { get; init; }

    /// <summary>
    /// Gets whether this entity is interpolated (remote entity interpolation enabled).
    /// </summary>
    public required bool IsInterpolated { get; init; }
}
