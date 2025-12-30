namespace KeenEyes.Network.Protocol;

/// <summary>
/// Network message types for the replication protocol.
/// </summary>
public enum MessageType : byte
{
    /// <summary>
    /// No message / invalid.
    /// </summary>
    None = 0,

    // Connection messages (0x01-0x0F)

    /// <summary>
    /// Client requests to connect.
    /// </summary>
    ConnectionRequest = 0x01,

    /// <summary>
    /// Server accepts connection.
    /// </summary>
    ConnectionAccepted = 0x02,

    /// <summary>
    /// Server rejects connection.
    /// </summary>
    ConnectionRejected = 0x03,

    /// <summary>
    /// Graceful disconnect.
    /// </summary>
    Disconnect = 0x04,

    /// <summary>
    /// Ping request for latency measurement.
    /// </summary>
    Ping = 0x05,

    /// <summary>
    /// Pong response for latency measurement.
    /// </summary>
    Pong = 0x06,

    // Entity replication (0x10-0x1F)

    /// <summary>
    /// Full world snapshot for late joiners.
    /// </summary>
    FullSnapshot = 0x10,

    /// <summary>
    /// Delta update with changed entities.
    /// </summary>
    DeltaSnapshot = 0x11,

    /// <summary>
    /// Entity spawned on server.
    /// </summary>
    EntitySpawn = 0x12,

    /// <summary>
    /// Entity despawned on server.
    /// </summary>
    EntityDespawn = 0x13,

    /// <summary>
    /// Component added to entity.
    /// </summary>
    ComponentAdd = 0x14,

    /// <summary>
    /// Component removed from entity.
    /// </summary>
    ComponentRemove = 0x15,

    /// <summary>
    /// Component updated on entity (full serialization).
    /// </summary>
    ComponentUpdate = 0x16,

    /// <summary>
    /// Component delta update (only changed fields).
    /// </summary>
    ComponentDelta = 0x17,

    // Client input (0x20-0x2F)

    /// <summary>
    /// Client input for prediction.
    /// </summary>
    ClientInput = 0x20,

    /// <summary>
    /// Client acknowledges received tick.
    /// </summary>
    ClientAck = 0x21,

    // Ownership (0x30-0x3F)

    /// <summary>
    /// Ownership transferred.
    /// </summary>
    OwnershipTransfer = 0x30,

    /// <summary>
    /// Client requests ownership.
    /// </summary>
    OwnershipRequest = 0x31,

    // Custom/RPC (0x40+)

    /// <summary>
    /// Remote procedure call.
    /// </summary>
    Rpc = 0x40,

    /// <summary>
    /// Reliable event.
    /// </summary>
    ReliableEvent = 0x41,

    /// <summary>
    /// Unreliable event.
    /// </summary>
    UnreliableEvent = 0x42,
}
