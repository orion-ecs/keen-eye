namespace KeenEyes.Network.Transport;

/// <summary>
/// Abstraction for network transport implementations.
/// </summary>
/// <remarks>
/// <para>
/// This interface allows swapping transport implementations without changing
/// networking logic. Implementations may include:
/// </para>
/// <list type="bullet">
/// <item><description>LocalTransport - In-memory for singleplayer/testing</description></item>
/// <item><description>UdpTransport - Raw UDP with reliability layer</description></item>
/// <item><description>WebSocketTransport - Browser-compatible</description></item>
/// <item><description>SteamTransport - Steam networking integration</description></item>
/// </list>
/// </remarks>
public interface INetworkTransport : IDisposable
{
    /// <summary>
    /// Gets the current connection state.
    /// </summary>
    ConnectionState State { get; }

    /// <summary>
    /// Gets whether this transport is running as a server (listening for connections).
    /// </summary>
    bool IsServer { get; }

    /// <summary>
    /// Gets whether this transport is running as a client (connected to a server).
    /// </summary>
    bool IsClient { get; }

    /// <summary>
    /// Raised when the connection state changes.
    /// </summary>
    event Action<ConnectionState>? StateChanged;

    /// <summary>
    /// Raised when a client connects (server only).
    /// </summary>
    /// <remarks>
    /// The int parameter is the client's connection ID.
    /// </remarks>
    event Action<int>? ClientConnected;

    /// <summary>
    /// Raised when a client disconnects (server only).
    /// </summary>
    /// <remarks>
    /// The int parameter is the client's connection ID.
    /// </remarks>
    event Action<int>? ClientDisconnected;

    /// <summary>
    /// Raised when data is received.
    /// </summary>
    /// <remarks>
    /// Parameters: (connectionId, data). For clients, connectionId is always 0 (the server).
    /// The span is only valid for the duration of the callback.
    /// </remarks>
    event DataReceivedHandler? DataReceived;

    /// <summary>
    /// Starts listening for connections (server mode).
    /// </summary>
    /// <param name="port">The port to listen on.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ListenAsync(int port, CancellationToken cancellationToken = default);

    /// <summary>
    /// Connects to a server (client mode).
    /// </summary>
    /// <param name="address">The server address.</param>
    /// <param name="port">The server port.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ConnectAsync(string address, int port, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends data to a specific connection.
    /// </summary>
    /// <param name="connectionId">
    /// The target connection ID. For clients, use 0 to send to the server.
    /// For servers, use the client's connection ID.
    /// </param>
    /// <param name="data">The data to send.</param>
    /// <param name="mode">The delivery mode.</param>
    void Send(int connectionId, ReadOnlySpan<byte> data, DeliveryMode mode);

    /// <summary>
    /// Sends data to all connected clients (server only).
    /// </summary>
    /// <param name="data">The data to send.</param>
    /// <param name="mode">The delivery mode.</param>
    void SendToAll(ReadOnlySpan<byte> data, DeliveryMode mode);

    /// <summary>
    /// Sends data to all connected clients except one (server only).
    /// </summary>
    /// <param name="excludeConnectionId">The connection ID to exclude.</param>
    /// <param name="data">The data to send.</param>
    /// <param name="mode">The delivery mode.</param>
    void SendToAllExcept(int excludeConnectionId, ReadOnlySpan<byte> data, DeliveryMode mode);

    /// <summary>
    /// Disconnects a specific client (server) or from the server (client).
    /// </summary>
    /// <param name="connectionId">
    /// For servers: the client's connection ID to disconnect.
    /// For clients: ignored (disconnects from server).
    /// </param>
    void Disconnect(int connectionId = 0);

    /// <summary>
    /// Processes incoming and outgoing data.
    /// </summary>
    /// <remarks>
    /// Must be called regularly (typically once per frame) to pump the network stack.
    /// Received data is delivered via the <see cref="DataReceived"/> event during this call.
    /// </remarks>
    void Update();

    /// <summary>
    /// Gets the round-trip time (ping) in milliseconds for a connection.
    /// </summary>
    /// <param name="connectionId">The connection ID.</param>
    /// <returns>The RTT in milliseconds, or -1 if unavailable.</returns>
    float GetRoundTripTime(int connectionId);

    /// <summary>
    /// Gets statistics for a connection.
    /// </summary>
    /// <param name="connectionId">The connection ID.</param>
    /// <returns>The connection statistics.</returns>
    ConnectionStatistics GetStatistics(int connectionId);
}

/// <summary>
/// Delegate for handling received network data.
/// </summary>
/// <param name="connectionId">The source connection ID.</param>
/// <param name="data">The received data (only valid during callback).</param>
public delegate void DataReceivedHandler(int connectionId, ReadOnlySpan<byte> data);

/// <summary>
/// Network connection statistics.
/// </summary>
public readonly record struct ConnectionStatistics
{
    /// <summary>
    /// Gets the round-trip time in milliseconds.
    /// </summary>
    public required float RoundTripTimeMs { get; init; }

    /// <summary>
    /// Gets the estimated packet loss percentage (0-100).
    /// </summary>
    public required float PacketLossPercent { get; init; }

    /// <summary>
    /// Gets the total bytes sent.
    /// </summary>
    public required long BytesSent { get; init; }

    /// <summary>
    /// Gets the total bytes received.
    /// </summary>
    public required long BytesReceived { get; init; }

    /// <summary>
    /// Gets the total packets sent.
    /// </summary>
    public required long PacketsSent { get; init; }

    /// <summary>
    /// Gets the total packets received.
    /// </summary>
    public required long PacketsReceived { get; init; }

    /// <summary>
    /// Gets the total packets lost (estimated).
    /// </summary>
    public required long PacketsLost { get; init; }
}
