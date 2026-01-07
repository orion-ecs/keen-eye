namespace KeenEyes.Mcp.TestBridge.Connection;

/// <summary>
/// Connection status information for the TestBridge IPC connection.
/// </summary>
public sealed record ConnectionStatus
{
    /// <summary>
    /// Gets whether the connection is currently active.
    /// </summary>
    public required bool IsConnected { get; init; }

    /// <summary>
    /// Gets the last measured ping latency in milliseconds.
    /// </summary>
    public double? LastPingMs { get; init; }

    /// <summary>
    /// Gets the time of the last successful ping.
    /// </summary>
    public DateTimeOffset? LastPingTime { get; init; }

    /// <summary>
    /// Gets the configured pipe name (for named pipe transport).
    /// </summary>
    public string? PipeName { get; init; }

    /// <summary>
    /// Gets the configured host (for TCP transport).
    /// </summary>
    public string? Host { get; init; }

    /// <summary>
    /// Gets the configured port (for TCP transport).
    /// </summary>
    public int? Port { get; init; }

    /// <summary>
    /// Gets the transport mode being used.
    /// </summary>
    public string? TransportMode { get; init; }

    /// <summary>
    /// Gets how long the connection has been active.
    /// </summary>
    public TimeSpan? ConnectionUptime { get; init; }

    /// <summary>
    /// Gets the number of consecutive ping failures.
    /// </summary>
    public int ConsecutivePingFailures { get; init; }
}
