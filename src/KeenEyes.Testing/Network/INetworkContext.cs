namespace KeenEyes.Testing.Network;

/// <summary>
/// Arguments for data received events.
/// </summary>
/// <param name="Data">The received data.</param>
/// <param name="SenderEndpoint">The endpoint that sent the data.</param>
/// <param name="Channel">The channel the data was received on.</param>
public readonly record struct NetworkDataReceivedEventArgs(object Data, string SenderEndpoint, int Channel);

/// <summary>
/// Arguments for connection events.
/// </summary>
/// <param name="Endpoint">The endpoint involved in the connection event.</param>
/// <param name="Reason">Optional reason for disconnection.</param>
public readonly record struct NetworkConnectionEventArgs(string Endpoint, string? Reason = null);

/// <summary>
/// Interface for network context that provides networking capabilities for games.
/// </summary>
/// <remarks>
/// <para>
/// INetworkContext provides a high-level abstraction for network communication,
/// suitable for both testing and production use. The interface supports:
/// </para>
/// <list type="bullet">
/// <item>Connection management (connect, disconnect, reconnect)</item>
/// <item>Reliable and unreliable message sending</item>
/// <item>Latency and packet loss simulation for testing</item>
/// <item>Event-driven data reception</item>
/// </list>
/// <para>
/// This is an interface-only design. A mock implementation (MockNetworkContext) will be
/// provided in a future update for testing networked game code without actual network connections.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Future usage with MockNetworkContext
/// var network = new MockNetworkContext();
/// network.SetLatency(50); // Simulate 50ms latency
/// network.SetPacketLoss(0.01f); // Simulate 1% packet loss
///
/// network.Connect("localhost:7777");
/// network.Send(new PlayerMoveMessage { X = 10, Y = 20 });
///
/// while (network.TryReceive(out var message))
/// {
///     ProcessMessage(message);
/// }
/// </code>
/// </example>
public interface INetworkContext : IDisposable
{
    /// <summary>
    /// Gets the current connection state.
    /// </summary>
    NetworkConnectionState ConnectionState { get; }

    /// <summary>
    /// Gets whether currently connected to a server.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Gets the current round-trip latency in milliseconds.
    /// </summary>
    /// <remarks>
    /// Returns 0 if not connected or latency is unknown.
    /// </remarks>
    float Latency { get; }

    /// <summary>
    /// Gets the current packet loss ratio (0 to 1).
    /// </summary>
    /// <remarks>
    /// Returns 0 if not connected or packet loss is unknown.
    /// </remarks>
    float PacketLoss { get; }

    /// <summary>
    /// Gets the endpoint currently connected to, or null if not connected.
    /// </summary>
    string? ConnectedEndpoint { get; }

    /// <summary>
    /// Connects to a server at the specified endpoint.
    /// </summary>
    /// <param name="endpoint">The server endpoint (e.g., "localhost:7777" or "192.168.1.1:7777").</param>
    /// <exception cref="InvalidOperationException">Thrown if already connecting or connected.</exception>
    void Connect(string endpoint);

    /// <summary>
    /// Disconnects from the current server.
    /// </summary>
    /// <param name="reason">Optional reason for disconnection.</param>
    void Disconnect(string? reason = null);

    /// <summary>
    /// Sends data to the connected server.
    /// </summary>
    /// <typeparam name="T">The type of data to send.</typeparam>
    /// <param name="data">The data to send.</param>
    /// <param name="reliable">Whether to use reliable delivery. Defaults to true.</param>
    /// <param name="channel">The channel to send on. Defaults to 0.</param>
    /// <exception cref="InvalidOperationException">Thrown if not connected.</exception>
    void Send<T>(T data, bool reliable = true, int channel = 0) where T : notnull;

    /// <summary>
    /// Sends data to a specific endpoint (for server-side broadcasting).
    /// </summary>
    /// <typeparam name="T">The type of data to send.</typeparam>
    /// <param name="endpoint">The target endpoint.</param>
    /// <param name="data">The data to send.</param>
    /// <param name="reliable">Whether to use reliable delivery. Defaults to true.</param>
    /// <param name="channel">The channel to send on. Defaults to 0.</param>
    void SendTo<T>(string endpoint, T data, bool reliable = true, int channel = 0) where T : notnull;

    /// <summary>
    /// Tries to receive data from the network queue.
    /// </summary>
    /// <typeparam name="T">The expected type of data.</typeparam>
    /// <param name="data">When this method returns, contains the received data if available.</param>
    /// <returns>True if data was received; otherwise, false.</returns>
    bool TryReceive<T>(out T? data) where T : class;

    /// <summary>
    /// Tries to receive any data from the network queue.
    /// </summary>
    /// <param name="data">When this method returns, contains the received data if available.</param>
    /// <param name="senderEndpoint">When this method returns, contains the sender endpoint if data was received.</param>
    /// <returns>True if data was received; otherwise, false.</returns>
    bool TryReceiveAny(out object? data, out string? senderEndpoint);

    /// <summary>
    /// Updates the network context, processing incoming and outgoing messages.
    /// </summary>
    /// <remarks>
    /// Call this method each frame or tick to process network events.
    /// </remarks>
    void Update();

    /// <summary>
    /// Occurs when a connection is successfully established.
    /// </summary>
    event Action<NetworkConnectionEventArgs>? OnConnected;

    /// <summary>
    /// Occurs when disconnected from the server.
    /// </summary>
    event Action<NetworkConnectionEventArgs>? OnDisconnected;

    /// <summary>
    /// Occurs when data is received.
    /// </summary>
    event Action<NetworkDataReceivedEventArgs>? OnDataReceived;

    /// <summary>
    /// Occurs when a connection attempt fails.
    /// </summary>
    event Action<NetworkConnectionEventArgs>? OnConnectionFailed;
}

/// <summary>
/// Options for configuring network behavior.
/// </summary>
/// <remarks>
/// Used by mock implementations to simulate network conditions.
/// </remarks>
public sealed class NetworkOptions
{
    /// <summary>
    /// Gets or sets the simulated latency in milliseconds.
    /// </summary>
    public float SimulatedLatency { get; set; }

    /// <summary>
    /// Gets or sets the simulated latency variance in milliseconds.
    /// </summary>
    /// <remarks>
    /// Actual latency will vary between (SimulatedLatency - Variance) and (SimulatedLatency + Variance).
    /// </remarks>
    public float SimulatedLatencyVariance { get; set; }

    /// <summary>
    /// Gets or sets the simulated packet loss ratio (0 to 1).
    /// </summary>
    public float SimulatedPacketLoss { get; set; }

    /// <summary>
    /// Gets or sets the simulated packet duplication ratio (0 to 1).
    /// </summary>
    public float SimulatedPacketDuplication { get; set; }

    /// <summary>
    /// Gets or sets the simulated out-of-order packet ratio (0 to 1).
    /// </summary>
    public float SimulatedPacketReordering { get; set; }

    /// <summary>
    /// Gets or sets the connection timeout in milliseconds.
    /// </summary>
    public float ConnectionTimeout { get; set; } = 5000f;

    /// <summary>
    /// Gets or sets whether to automatically reconnect on disconnection.
    /// </summary>
    public bool AutoReconnect { get; set; }

    /// <summary>
    /// Gets or sets the delay between reconnection attempts in milliseconds.
    /// </summary>
    public float ReconnectDelay { get; set; } = 1000f;

    /// <summary>
    /// Gets or sets the maximum number of reconnection attempts.
    /// </summary>
    public int MaxReconnectAttempts { get; set; } = 5;
}
