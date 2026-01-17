namespace KeenEyes.TestBridge.Network;

/// <summary>
/// Snapshot of a connected client's state (server-side view).
/// </summary>
public sealed record ClientSnapshot
{
    /// <summary>
    /// Gets the client ID.
    /// </summary>
    public required int ClientId { get; init; }

    /// <summary>
    /// Gets the last acknowledged server tick from this client.
    /// </summary>
    public required uint LastAckedTick { get; init; }

    /// <summary>
    /// Gets whether the client needs a full snapshot.
    /// </summary>
    public required bool NeedsFullSnapshot { get; init; }

    /// <summary>
    /// Gets the round-trip time to this client in milliseconds.
    /// </summary>
    public required float RoundTripTimeMs { get; init; }
}
