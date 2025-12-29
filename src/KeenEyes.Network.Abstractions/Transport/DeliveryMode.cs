namespace KeenEyes.Network.Transport;

/// <summary>
/// Specifies how a network message should be delivered.
/// </summary>
public enum DeliveryMode
{
    /// <summary>
    /// Fire and forget. No delivery guarantee, no ordering.
    /// </summary>
    /// <remarks>
    /// Use for frequently updated state where the next update will correct any loss.
    /// Examples: position updates, health bars.
    /// Lowest latency, lowest overhead.
    /// </remarks>
    Unreliable = 0,

    /// <summary>
    /// Unreliable but sequenced. Out-of-order packets are dropped.
    /// </summary>
    /// <remarks>
    /// Use for state where old values are useless.
    /// Examples: player input, aim direction.
    /// Low latency, prevents processing stale data.
    /// </remarks>
    UnreliableSequenced = 1,

    /// <summary>
    /// Guaranteed delivery, but order not guaranteed.
    /// </summary>
    /// <remarks>
    /// Use for important events where order doesn't matter.
    /// Examples: achievement unlocked, item picked up.
    /// Retransmits on loss, allows reordering.
    /// </remarks>
    ReliableUnordered = 2,

    /// <summary>
    /// Guaranteed delivery in order.
    /// </summary>
    /// <remarks>
    /// Use for ordered event streams.
    /// Examples: chat messages, RPC calls, transaction logs.
    /// Highest reliability, may introduce head-of-line blocking.
    /// </remarks>
    ReliableOrdered = 3,
}
