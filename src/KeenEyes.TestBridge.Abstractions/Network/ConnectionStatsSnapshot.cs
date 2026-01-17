namespace KeenEyes.TestBridge.Network;

/// <summary>
/// Connection statistics for a network client.
/// </summary>
public sealed record ConnectionStatsSnapshot
{
    /// <summary>
    /// Gets the round-trip time to the server in milliseconds.
    /// </summary>
    public required float RoundTripTimeMs { get; init; }

    /// <summary>
    /// Gets the packet loss percentage (0-100).
    /// </summary>
    public required float PacketLossPercent { get; init; }

    /// <summary>
    /// Gets the total bytes sent to the server.
    /// </summary>
    public required long BytesSent { get; init; }

    /// <summary>
    /// Gets the total bytes received from the server.
    /// </summary>
    public required long BytesReceived { get; init; }

    /// <summary>
    /// Gets the total packets sent to the server.
    /// </summary>
    public required long PacketsSent { get; init; }

    /// <summary>
    /// Gets the total packets received from the server.
    /// </summary>
    public required long PacketsReceived { get; init; }
}
