namespace KeenEyes.Testing.Network;

/// <summary>
/// A mock implementation of <see cref="INetworkContext"/> for testing network code
/// without real network connections.
/// </summary>
/// <remarks>
/// <para>
/// MockNetworkContext simulates network behavior for testing, providing:
/// <list type="bullet">
///   <item><description>Message queuing and delivery simulation</description></item>
///   <item><description>Connection state management</description></item>
///   <item><description>Latency and packet loss simulation</description></item>
///   <item><description>Sent message tracking for verification</description></item>
/// </list>
/// </para>
/// <para>
/// Use the SimulateXxx methods to inject events and messages for testing.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var network = new MockNetworkContext();
///
/// network.Connect("server:7777");
/// network.SimulateConnect();
/// network.IsConnected.Should().BeTrue();
///
/// network.Send(new PlayerMoveMessage { X = 10, Y = 20 });
/// network.SentMessages.Should().ContainSingle();
///
/// network.SimulateReceive(new ServerResponse { Ok = true }, "server:7777");
/// network.TryReceive&lt;ServerResponse&gt;(out var response).Should().BeTrue();
/// </code>
/// </example>
public sealed class MockNetworkContext : INetworkContext
{
    private readonly Queue<ReceivedMessage> receiveQueue = new();
    private int packetLossCounter;
    private NetworkConnectionState connectionState = NetworkConnectionState.Disconnected;
    private string? connectedEndpoint;
    private bool disposed;

    /// <summary>
    /// Gets or sets the network options.
    /// </summary>
    public NetworkOptions Options { get; set; } = new();

    /// <summary>
    /// Gets the list of all sent messages for verification.
    /// </summary>
    public List<SentMessage> SentMessages { get; } = [];

    /// <summary>
    /// Gets the number of connection attempts.
    /// </summary>
    public int ConnectAttemptCount { get; private set; }

    /// <summary>
    /// Gets the number of disconnections.
    /// </summary>
    public int DisconnectCount { get; private set; }

    /// <summary>
    /// Gets the number of Update() calls.
    /// </summary>
    public int UpdateCount { get; private set; }

    /// <summary>
    /// Gets or sets whether connection attempts should automatically succeed.
    /// </summary>
    public bool AutoConnect { get; set; }

    /// <summary>
    /// Gets or sets whether connection attempts should automatically fail.
    /// </summary>
    public bool AutoFail { get; set; }

    #region INetworkContext Properties

    /// <inheritdoc />
    public NetworkConnectionState ConnectionState => connectionState;

    /// <inheritdoc />
    public bool IsConnected => connectionState == NetworkConnectionState.Connected;

    /// <inheritdoc />
    public float Latency { get; private set; }

    /// <inheritdoc />
    public float PacketLoss { get; private set; }

    /// <inheritdoc />
    public string? ConnectedEndpoint => connectedEndpoint;

    #endregion

    #region Events

    /// <inheritdoc />
    public event Action<NetworkConnectionEventArgs>? OnConnected;

    /// <inheritdoc />
    public event Action<NetworkConnectionEventArgs>? OnDisconnected;

    /// <inheritdoc />
    public event Action<NetworkDataReceivedEventArgs>? OnDataReceived;

    /// <inheritdoc />
    public event Action<NetworkConnectionEventArgs>? OnConnectionFailed;

    #endregion

    #region Connection Methods

    /// <inheritdoc />
    public void Connect(string endpoint)
    {
        if (connectionState == NetworkConnectionState.Connecting ||
            connectionState == NetworkConnectionState.Connected)
        {
            throw new InvalidOperationException("Already connecting or connected.");
        }

        ConnectAttemptCount++;
        connectedEndpoint = endpoint;
        connectionState = NetworkConnectionState.Connecting;

        if (AutoConnect)
        {
            SimulateConnect();
        }
        else if (AutoFail)
        {
            SimulateConnectionFailed("Auto-fail enabled");
        }
    }

    /// <inheritdoc />
    public void Disconnect(string? reason = null)
    {
        if (connectionState == NetworkConnectionState.Disconnected)
        {
            return;
        }

        DisconnectCount++;
        var endpoint = connectedEndpoint;
        connectionState = NetworkConnectionState.Disconnecting;
        connectionState = NetworkConnectionState.Disconnected;
        connectedEndpoint = null;

        OnDisconnected?.Invoke(new NetworkConnectionEventArgs(endpoint ?? "unknown", reason));
    }

    #endregion

    #region Send Methods

    /// <inheritdoc />
    public void Send<T>(T data, bool reliable = true, int channel = 0) where T : notnull
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("Not connected.");
        }

        SendTo(connectedEndpoint!, data, reliable, channel);
    }

    /// <inheritdoc />
    public void SendTo<T>(string endpoint, T data, bool reliable = true, int channel = 0) where T : notnull
    {
        // Simulate packet loss for unreliable messages using deterministic counter
        if (!reliable && PacketLoss > 0)
        {
            packetLossCounter++;
            // Deterministic packet loss: lose every Nth packet where N = 1/PacketLoss
            var lossInterval = (int)(1f / PacketLoss);
            if (lossInterval > 0 && packetLossCounter % lossInterval == 0)
            {
                return; // Message "lost"
            }
        }

        SentMessages.Add(new SentMessage(
            endpoint,
            data,
            data.GetType(),
            reliable,
            channel,
            DateTime.UtcNow));
    }

    #endregion

    #region Receive Methods

    /// <inheritdoc />
    public bool TryReceive<T>(out T? data) where T : class
    {
        while (receiveQueue.Count > 0)
        {
            var message = receiveQueue.Dequeue();
            if (message.Data is T typedData)
            {
                data = typedData;
                return true;
            }
            // Put back non-matching messages? For simplicity, we'll discard them
        }

        data = default;
        return false;
    }

    /// <inheritdoc />
    public bool TryReceiveAny(out object? data, out string? senderEndpoint)
    {
        if (receiveQueue.Count > 0)
        {
            var message = receiveQueue.Dequeue();
            data = message.Data;
            senderEndpoint = message.SenderEndpoint;
            return true;
        }

        data = null;
        senderEndpoint = null;
        return false;
    }

    #endregion

    #region Update

    /// <inheritdoc />
    public void Update()
    {
        UpdateCount++;
    }

    #endregion

    #region Simulation Methods

    /// <summary>
    /// Simulates a successful connection.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if not in connecting state.</exception>
    public void SimulateConnect()
    {
        if (connectionState != NetworkConnectionState.Connecting)
        {
            throw new InvalidOperationException("Must be in Connecting state to simulate connection.");
        }

        connectionState = NetworkConnectionState.Connected;
        OnConnected?.Invoke(new NetworkConnectionEventArgs(connectedEndpoint!));
    }

    /// <summary>
    /// Simulates a disconnection.
    /// </summary>
    /// <param name="reason">The disconnection reason.</param>
    public void SimulateDisconnect(string? reason = null)
    {
        var endpoint = connectedEndpoint ?? "unknown";
        connectionState = NetworkConnectionState.Disconnected;
        connectedEndpoint = null;
        OnDisconnected?.Invoke(new NetworkConnectionEventArgs(endpoint, reason));
    }

    /// <summary>
    /// Simulates a connection failure.
    /// </summary>
    /// <param name="reason">The failure reason.</param>
    public void SimulateConnectionFailed(string? reason = null)
    {
        var endpoint = connectedEndpoint ?? "unknown";
        connectionState = NetworkConnectionState.Disconnected;
        connectedEndpoint = null;
        OnConnectionFailed?.Invoke(new NetworkConnectionEventArgs(endpoint, reason));
    }

    /// <summary>
    /// Simulates receiving data from the network.
    /// </summary>
    /// <typeparam name="T">The type of data received.</typeparam>
    /// <param name="data">The received data.</param>
    /// <param name="senderEndpoint">The sender endpoint.</param>
    /// <param name="channel">The channel the data was received on.</param>
    public void SimulateReceive<T>(T data, string senderEndpoint, int channel = 0) where T : notnull
    {
        receiveQueue.Enqueue(new ReceivedMessage(data, senderEndpoint, channel));
        OnDataReceived?.Invoke(new NetworkDataReceivedEventArgs(data, senderEndpoint, channel));
    }

    /// <summary>
    /// Sets the simulated latency.
    /// </summary>
    /// <param name="latencyMs">The latency in milliseconds.</param>
    public void SimulateLatency(float latencyMs)
    {
        Latency = latencyMs;
        Options.SimulatedLatency = latencyMs;
    }

    /// <summary>
    /// Sets the simulated packet loss ratio.
    /// </summary>
    /// <param name="ratio">The packet loss ratio (0 to 1).</param>
    public void SimulatePacketLoss(float ratio)
    {
        PacketLoss = Math.Clamp(ratio, 0f, 1f);
        Options.SimulatedPacketLoss = PacketLoss;
    }

    #endregion

    #region Test Control

    /// <summary>
    /// Resets all state.
    /// </summary>
    public void Reset()
    {
        receiveQueue.Clear();
        SentMessages.Clear();
        connectionState = NetworkConnectionState.Disconnected;
        connectedEndpoint = null;
        ConnectAttemptCount = 0;
        DisconnectCount = 0;
        UpdateCount = 0;
        Latency = 0;
        PacketLoss = 0;
        AutoConnect = false;
        AutoFail = false;
        Options = new NetworkOptions();
    }

    /// <summary>
    /// Clears sent messages without resetting other state.
    /// </summary>
    public void ClearSentMessages()
    {
        SentMessages.Clear();
    }

    /// <summary>
    /// Clears the receive queue without resetting other state.
    /// </summary>
    public void ClearReceiveQueue()
    {
        receiveQueue.Clear();
    }

    /// <summary>
    /// Gets the number of messages waiting in the receive queue.
    /// </summary>
    public int ReceiveQueueCount => receiveQueue.Count;

    #endregion

    /// <inheritdoc />
    public void Dispose()
    {
        if (!disposed)
        {
            disposed = true;
            Reset();
        }
    }

    private sealed record ReceivedMessage(object Data, string SenderEndpoint, int Channel);
}

/// <summary>
/// A recorded sent message.
/// </summary>
/// <param name="Endpoint">The target endpoint.</param>
/// <param name="Data">The message data.</param>
/// <param name="DataType">The type of the data.</param>
/// <param name="Reliable">Whether reliable delivery was requested.</param>
/// <param name="Channel">The channel sent on.</param>
/// <param name="Timestamp">When the message was sent.</param>
public sealed record SentMessage(
    string Endpoint,
    object Data,
    Type DataType,
    bool Reliable,
    int Channel,
    DateTime Timestamp);
