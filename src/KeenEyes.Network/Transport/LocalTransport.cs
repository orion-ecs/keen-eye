using System.Collections.Concurrent;
using KeenEyes.Network.Transport;

namespace KeenEyes.Network;

/// <summary>
/// In-memory transport for local singleplayer or testing.
/// </summary>
/// <remarks>
/// <para>
/// This transport passes messages directly between client and server in the same process,
/// similar to how Minecraft handles singleplayer (integrated server). This ensures that
/// singleplayer and multiplayer use identical code paths.
/// </para>
/// <para>
/// Benefits:
/// <list type="bullet">
/// <item><description>Zero network overhead for singleplayer</description></item>
/// <item><description>Same game logic for all modes</description></item>
/// <item><description>Easy testing without network setup</description></item>
/// <item><description>Simulated latency/packet loss for testing</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create paired transports
/// var (serverTransport, clientTransport) = LocalTransport.CreatePair();
///
/// // Use in plugins
/// serverWorld.InstallPlugin(new NetworkServerPlugin(serverTransport));
/// clientWorld.InstallPlugin(new NetworkClientPlugin(clientTransport));
/// </code>
/// </example>
public sealed class LocalTransport : INetworkTransport
{
    private readonly ConcurrentQueue<PendingMessage> incomingMessages = new();
    private readonly Lock stateLock = new();

    private LocalTransport? peer;
    private ConnectionState state = ConnectionState.Disconnected;
    private int nextConnectionId = 1;
    private readonly Dictionary<int, bool> connections = [];
    private bool isServer;
    private bool disposed;

    // Simulation settings
    private float simulatedLatencyMs;
    private float simulatedPacketLossPercent;
    private readonly Random random = new();

    // Statistics
    private long bytesSent;
    private long bytesReceived;
    private long packetsSent;
    private long packetsReceived;

    /// <inheritdoc/>
    public ConnectionState State
    {
        get
        {
            lock (stateLock)
            {
                return state;
            }
        }
        private set
        {
            ConnectionState oldState;
            lock (stateLock)
            {
                oldState = state;
                state = value;
            }

            if (oldState != value)
            {
                StateChanged?.Invoke(value);
            }
        }
    }

    /// <inheritdoc/>
    public bool IsServer => isServer;

    /// <inheritdoc/>
    public bool IsClient => !isServer && State == ConnectionState.Connected;

    /// <inheritdoc/>
    public event Action<ConnectionState>? StateChanged;

    /// <inheritdoc/>
    public event Action<int>? ClientConnected;

    /// <inheritdoc/>
    public event Action<int>? ClientDisconnected;

    /// <inheritdoc/>
    public event DataReceivedHandler? DataReceived;

    /// <summary>
    /// Gets or sets the simulated latency in milliseconds.
    /// </summary>
    /// <remarks>
    /// Messages will be delayed by this amount before delivery.
    /// Useful for testing network code behavior under latency.
    /// </remarks>
    public float SimulatedLatencyMs
    {
        get => simulatedLatencyMs;
        set => simulatedLatencyMs = Math.Max(0, value);
    }

    /// <summary>
    /// Gets or sets the simulated packet loss percentage (0-100).
    /// </summary>
    /// <remarks>
    /// Unreliable messages have this chance of being dropped.
    /// Useful for testing packet loss handling.
    /// </remarks>
    public float SimulatedPacketLossPercent
    {
        get => simulatedPacketLossPercent;
        set => simulatedPacketLossPercent = Math.Clamp(value, 0, 100);
    }

    /// <summary>
    /// Creates a connected pair of local transports for client-server communication.
    /// </summary>
    /// <returns>A tuple of (serverTransport, clientTransport).</returns>
    public static (LocalTransport Server, LocalTransport Client) CreatePair()
    {
        var server = new LocalTransport();
        var client = new LocalTransport();

        server.peer = client;
        client.peer = server;

        return (server, client);
    }

    /// <inheritdoc/>
    public Task ListenAsync(int port, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        lock (stateLock)
        {
            if (state != ConnectionState.Disconnected)
            {
                throw new InvalidOperationException("Transport is already connected or listening.");
            }

            isServer = true;
            state = ConnectionState.Connected;
        }

        StateChanged?.Invoke(ConnectionState.Connected);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task ConnectAsync(string address, int port, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (peer is null)
        {
            throw new InvalidOperationException(
                "LocalTransport requires a peer. Use LocalTransport.CreatePair() to create connected transports.");
        }

        lock (stateLock)
        {
            if (state != ConnectionState.Disconnected)
            {
                throw new InvalidOperationException("Transport is already connected.");
            }

            isServer = false;
            state = ConnectionState.Connecting;
        }

        StateChanged?.Invoke(ConnectionState.Connecting);

        // Notify server of connection
        var connectionId = peer.AcceptConnection();

        lock (stateLock)
        {
            connections[0] = true; // Client uses 0 for server
            state = ConnectionState.Connected;
        }

        StateChanged?.Invoke(ConnectionState.Connected);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Accepts an incoming connection from a client.
    /// </summary>
    private int AcceptConnection()
    {
        int connectionId;
        lock (stateLock)
        {
            connectionId = nextConnectionId++;
            connections[connectionId] = true;
        }

        ClientConnected?.Invoke(connectionId);
        return connectionId;
    }

    /// <inheritdoc/>
    public void Send(int connectionId, ReadOnlySpan<byte> data, DeliveryMode mode)
    {
        ThrowIfDisposed();

        if (peer is null || State != ConnectionState.Connected)
        {
            return;
        }

        // Simulate packet loss for unreliable modes
        if (mode is DeliveryMode.Unreliable or DeliveryMode.UnreliableSequenced)
        {
            if (random.NextDouble() * 100 < simulatedPacketLossPercent)
            {
                return; // Packet "lost"
            }
        }

        var message = new PendingMessage
        {
            ConnectionId = isServer ? connectionId : 0,
            Data = data.ToArray(),
            DeliveryTime = simulatedLatencyMs > 0
                ? DateTime.UtcNow.AddMilliseconds(simulatedLatencyMs)
                : DateTime.MinValue
        };

        peer.incomingMessages.Enqueue(message);

        Interlocked.Add(ref bytesSent, data.Length);
        Interlocked.Increment(ref packetsSent);
    }

    /// <inheritdoc/>
    public void SendToAll(ReadOnlySpan<byte> data, DeliveryMode mode)
    {
        if (!isServer)
        {
            throw new InvalidOperationException("SendToAll can only be called on server.");
        }

        // For LocalTransport, there's only one client
        lock (stateLock)
        {
            foreach (var connectionId in connections.Keys)
            {
                Send(connectionId, data, mode);
            }
        }
    }

    /// <inheritdoc/>
    public void SendToAllExcept(int excludeConnectionId, ReadOnlySpan<byte> data, DeliveryMode mode)
    {
        if (!isServer)
        {
            throw new InvalidOperationException("SendToAllExcept can only be called on server.");
        }

        lock (stateLock)
        {
            foreach (var connectionId in connections.Keys)
            {
                if (connectionId != excludeConnectionId)
                {
                    Send(connectionId, data, mode);
                }
            }
        }
    }

    /// <inheritdoc/>
    public void Disconnect(int connectionId = 0)
    {
        ThrowIfDisposed();

        lock (stateLock)
        {
            if (state == ConnectionState.Disconnected)
            {
                return;
            }

            if (isServer && connectionId != 0)
            {
                // Disconnect specific client
                if (connections.Remove(connectionId))
                {
                    ClientDisconnected?.Invoke(connectionId);
                }

                return;
            }

            // Full disconnect
            state = ConnectionState.Disconnecting;
        }

        StateChanged?.Invoke(ConnectionState.Disconnecting);

        // Notify peer
        if (peer is not null && !isServer)
        {
            lock (peer.stateLock)
            {
                // Find our connection ID on the server and remove it
                var toRemove = peer.connections.Keys.FirstOrDefault();
                if (toRemove != 0 && peer.connections.Remove(toRemove))
                {
                    peer.ClientDisconnected?.Invoke(toRemove);
                }
            }
        }

        lock (stateLock)
        {
            connections.Clear();
            state = ConnectionState.Disconnected;
        }

        StateChanged?.Invoke(ConnectionState.Disconnected);
    }

    /// <inheritdoc/>
    public void Update()
    {
        ThrowIfDisposed();

        var now = DateTime.UtcNow;

        while (incomingMessages.TryPeek(out var message))
        {
            // Check if message is ready (simulated latency)
            if (message.DeliveryTime > now)
            {
                break; // Not ready yet, stop processing
            }

            if (!incomingMessages.TryDequeue(out message))
            {
                break;
            }

            Interlocked.Add(ref bytesReceived, message.Data.Length);
            Interlocked.Increment(ref packetsReceived);

            DataReceived?.Invoke(message.ConnectionId, message.Data);
        }
    }

    /// <inheritdoc/>
    public float GetRoundTripTime(int connectionId)
    {
        // For local transport, RTT is just simulated latency * 2
        return simulatedLatencyMs * 2;
    }

    /// <inheritdoc/>
    public ConnectionStatistics GetStatistics(int connectionId)
    {
        return new ConnectionStatistics
        {
            RoundTripTimeMs = simulatedLatencyMs * 2,
            PacketLossPercent = simulatedPacketLossPercent,
            BytesSent = Interlocked.Read(ref bytesSent),
            BytesReceived = Interlocked.Read(ref bytesReceived),
            PacketsSent = Interlocked.Read(ref packetsSent),
            PacketsReceived = Interlocked.Read(ref packetsReceived),
            PacketsLost = 0 // We don't track this for local transport
        };
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        if (State != ConnectionState.Disconnected)
        {
            Disconnect();
        }

        // Break the peer reference to allow GC
        if (peer is not null)
        {
            peer.peer = null;
            peer = null;
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }

    private sealed class PendingMessage
    {
        public required int ConnectionId { get; init; }
        public required byte[] Data { get; init; }
        public required DateTime DeliveryTime { get; init; }
    }
}
