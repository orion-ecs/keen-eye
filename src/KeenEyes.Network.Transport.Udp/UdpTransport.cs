using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using KeenEyes.Network.Transport;

namespace KeenEyes.Network.Transport.Udp;

/// <summary>
/// UDP-based network transport with optional reliability layer.
/// </summary>
/// <remarks>
/// <para>
/// UDP provides lower latency than TCP but requires explicit reliability handling.
/// This transport supports multiple delivery modes:
/// </para>
/// <list type="bullet">
/// <item><description>Unreliable - Fire and forget (lowest latency)</description></item>
/// <item><description>UnreliableSequenced - Drops out-of-order packets</description></item>
/// <item><description>ReliableUnordered - ACK/resend, any order delivery</description></item>
/// <item><description>ReliableOrdered - ACK/resend, ordered delivery</description></item>
/// </list>
/// <para>
/// Connection management uses a simple handshake protocol with keepalive packets.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Server
/// var serverTransport = new UdpTransport();
/// await serverTransport.ListenAsync(7777);
///
/// // Client
/// var clientTransport = new UdpTransport();
/// await clientTransport.ConnectAsync("192.168.1.100", 7777);
/// </code>
/// </example>
public sealed class UdpTransport : INetworkTransport
{
    // Protocol constants
    private const byte ProtocolVersion = 1;
    private const int MaxPacketSize = 1200; // Safe MTU for most networks
    private const int HeaderSize = 8; // version(1) + type(1) + sequence(2) + ack(2) + flags(2)
    private const int MaxPayloadSize = MaxPacketSize - HeaderSize;
    private const int ConnectionTimeoutMs = 10000;
    private const int KeepaliveIntervalMs = 1000;
    private const int ResendIntervalMs = 100;
    private const int MaxResendAttempts = 10;
    private const ushort SequenceWindowSize = 1024;

    // Packet types
    private const byte PacketConnect = 0;
    private const byte PacketConnectAck = 1;
    private const byte PacketDisconnect = 2;
    private const byte PacketData = 3;
    private const byte PacketAck = 4;
    private const byte PacketKeepalive = 5;

    private readonly ConcurrentDictionary<int, UdpConnection> connections = new();

    // Connection, disconnection, and data events are all enqueued here from the
    // background receive loop and drained in order on the game-loop thread during
    // Update(). Firing ClientConnected/ClientDisconnected directly from the receive
    // thread would let consumers mutate World/plugin state off the loop (see #1098).
    private readonly ConcurrentQueue<TransportEvent> events = new();
    private readonly Lock stateLock = new();

    private UdpClient? socket;
    private CancellationTokenSource? receiveCts;
    private ConnectionState state = ConnectionState.Disconnected;
    private int nextConnectionId = 1;
    private bool isServer;
    private bool disposed;
    private IPEndPoint? serverEndpoint;
    private UdpConnection? serverConnection;
    private readonly Stopwatch rttStopwatch = new();

    // Statistics
    private long bytesSent;
    private long bytesReceived;
    private long packetsSent;
    private long packetsReceived;
    private long packetsLost;

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

    /// <summary>
    /// Gets the local port the transport is bound to.
    /// </summary>
    /// <remarks>
    /// For servers, returns the port the socket is bound to after calling <see cref="ListenAsync"/>.
    /// Useful when listening on port 0 to get the OS-assigned ephemeral port.
    /// For clients, returns the ephemeral port assigned by the OS.
    /// </remarks>
    public int LocalPort => socket?.Client.LocalEndPoint is IPEndPoint ep ? ep.Port : -1;

    /// <inheritdoc/>
    public event Action<ConnectionState>? StateChanged;

    /// <inheritdoc/>
    public event Action<int>? ClientConnected;

    /// <inheritdoc/>
    public event Action<int>? ClientDisconnected;

    /// <inheritdoc/>
    public event DataReceivedHandler? DataReceived;

    /// <inheritdoc/>
    public async Task ListenAsync(int port, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        lock (stateLock)
        {
            if (state != ConnectionState.Disconnected)
            {
                throw new InvalidOperationException("Transport is already connected or listening.");
            }

            isServer = true;
        }

        socket = new UdpClient(port);
        receiveCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        State = ConnectionState.Connected;

        // Start receiving in background
        _ = ReceiveLoopAsync(receiveCts.Token);
    }

    /// <inheritdoc/>
    public async Task ConnectAsync(string address, int port, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

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

        try
        {
            socket = new UdpClient();

            // Only parse literal IPs directly; hostnames must resolve via DNS first.
            // Parsing a hostname (e.g. "localhost") with IPAddress.Parse throws
            // FormatException, which previously made the DNS branch unreachable.
            if (IPAddress.TryParse(address, out var literalAddress))
            {
                serverEndpoint = new IPEndPoint(literalAddress, port);
            }
            else
            {
                var addresses = await Dns.GetHostAddressesAsync(address, cancellationToken);
                if (addresses.Length == 0)
                {
                    throw new SocketException((int)SocketError.HostNotFound);
                }

                serverEndpoint = new IPEndPoint(addresses[0], port);
            }

            receiveCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Create server connection tracker
            serverConnection = new UdpConnection(0, serverEndpoint);

            // Bind to ephemeral port before starting receive loop
            // (UdpClient doesn't bind until first Send/Receive, but we need to receive first)
            socket.Client.Bind(new IPEndPoint(IPAddress.Any, 0));

            // Start receiving
            _ = ReceiveLoopAsync(receiveCts.Token);

            // Send connect packet
            var connectPacket = new byte[HeaderSize];
            connectPacket[0] = ProtocolVersion;
            connectPacket[1] = PacketConnect;

            rttStopwatch.Restart();
            await socket.SendAsync(connectPacket, serverEndpoint, cancellationToken);

            // Wait for connect ack
            var timeout = Task.Delay(ConnectionTimeoutMs, cancellationToken);
            while (State == ConnectionState.Connecting)
            {
                if (timeout.IsCompleted)
                {
                    throw new TimeoutException("Connection timed out.");
                }

                await Task.Delay(10, cancellationToken);
            }

            if (State != ConnectionState.Connected)
            {
                throw new SocketException((int)SocketError.ConnectionRefused);
            }
        }
        catch
        {
            State = ConnectionState.Disconnected;
            socket?.Dispose();
            socket = null;
            throw;
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && socket is not null)
        {
            try
            {
                var result = await socket.ReceiveAsync(cancellationToken);
                ProcessPacket(result.Buffer, result.RemoteEndPoint);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (SocketException)
            {
                // Socket closed or error
                break;
            }
        }
    }

    private void ProcessPacket(byte[] data, IPEndPoint remoteEndpoint)
    {
        if (data.Length < HeaderSize)
        {
            return;
        }

        var version = data[0];
        if (version != ProtocolVersion)
        {
            return;
        }

        var packetType = data[1];
        var sequence = BitConverter.ToUInt16(data, 2);
        var ack = BitConverter.ToUInt16(data, 4);
        var flags = BitConverter.ToUInt16(data, 6);

        Interlocked.Add(ref bytesReceived, data.Length);
        Interlocked.Increment(ref packetsReceived);

        if (isServer)
        {
            ProcessServerPacket(data, remoteEndpoint, packetType, sequence, ack, flags);
        }
        else
        {
            ProcessClientPacket(data, packetType, sequence, ack, flags);
        }
    }

    private void ProcessServerPacket(byte[] data, IPEndPoint remoteEndpoint, byte packetType, ushort sequence, ushort ack, ushort flags)
    {
        // Find or create connection
        var connection = FindConnectionByEndpoint(remoteEndpoint);

        switch (packetType)
        {
            case PacketConnect:
                if (connection is null)
                {
                    var connectionId = Interlocked.Increment(ref nextConnectionId) - 1;
                    connection = new UdpConnection(connectionId, remoteEndpoint);
                    connections[connectionId] = connection;

                    // Send connect ack
                    var ackPacket = new byte[HeaderSize + 4];
                    ackPacket[0] = ProtocolVersion;
                    ackPacket[1] = PacketConnectAck;
                    BitConverter.TryWriteBytes(ackPacket.AsSpan(HeaderSize), connectionId);

                    socket?.Send(ackPacket, remoteEndpoint);
                    Interlocked.Add(ref bytesSent, ackPacket.Length);
                    Interlocked.Increment(ref packetsSent);

                    // Defer to Update() so the consumer handles it on the game loop.
                    events.Enqueue(new TransportEvent { Kind = TransportEventKind.Connected, ConnectionId = connectionId });
                }

                break;

            case PacketDisconnect:
                if (connection is not null)
                {
                    HandleDisconnection(connection.Id);
                }

                break;

            case PacketData:
                if (connection is not null)
                {
                    connection.LastReceiveTime = DateTime.UtcNow;
                    ProcessDataPacket(connection, data, sequence, flags);
                }

                break;

            case PacketAck:
                if (connection is not null)
                {
                    connection.LastReceiveTime = DateTime.UtcNow;
                    ProcessAckPacket(connection, ack, flags);
                }

                break;

            case PacketKeepalive:
                if (connection is not null)
                {
                    connection.LastReceiveTime = DateTime.UtcNow;
                }

                break;
        }
    }

    private void ProcessClientPacket(byte[] data, byte packetType, ushort sequence, ushort ack, ushort flags)
    {
        switch (packetType)
        {
            case PacketConnectAck:
                if (State == ConnectionState.Connecting && data.Length >= HeaderSize + 4)
                {
                    serverConnection!.RoundTripTimeMs = (float)rttStopwatch.Elapsed.TotalMilliseconds;
                    State = ConnectionState.Connected;
                }

                break;

            case PacketDisconnect:
                HandleDisconnection(0);
                break;

            case PacketData:
                if (serverConnection is not null)
                {
                    serverConnection.LastReceiveTime = DateTime.UtcNow;
                    ProcessDataPacket(serverConnection, data, sequence, flags);
                }

                break;

            case PacketAck:
                if (serverConnection is not null)
                {
                    serverConnection.LastReceiveTime = DateTime.UtcNow;
                    ProcessAckPacket(serverConnection, ack, flags);
                }

                break;

            case PacketKeepalive:
                if (serverConnection is not null)
                {
                    serverConnection.LastReceiveTime = DateTime.UtcNow;
                }

                break;
        }
    }

    private void ProcessDataPacket(UdpConnection connection, byte[] data, ushort sequence, ushort flags)
    {
        var deliveryMode = (DeliveryMode)(flags & 0x03);
        var payload = data.AsSpan(HeaderSize);

        Interlocked.Add(ref connection.BytesReceived, data.Length);
        Interlocked.Increment(ref connection.PacketsReceived);

        switch (deliveryMode)
        {
            case DeliveryMode.Unreliable:
                // Always deliver
                EnqueueMessage(connection.Id, payload);
                break;

            case DeliveryMode.UnreliableSequenced:
                // Only deliver if newer
                if (IsSequenceNewer(sequence, connection.LastReceivedSequence))
                {
                    connection.LastReceivedSequence = sequence;
                    EnqueueMessage(connection.Id, payload);
                }

                break;

            case DeliveryMode.ReliableUnordered:
                // Send ack
                SendAck(connection, sequence, DeliveryMode.ReliableUnordered);

                // Deliver if not duplicate
                if (!connection.ReceivedSequences.Contains(sequence))
                {
                    connection.ReceivedSequences.Add(sequence);
                    if (IsSequenceNewer(sequence, connection.HighestReceivedUnorderedSequence))
                    {
                        connection.HighestReceivedUnorderedSequence = sequence;
                    }

                    CleanupUnorderedSequences(connection);
                    EnqueueMessage(connection.Id, payload);
                }

                break;

            case DeliveryMode.ReliableOrdered:
                // Send ack (tagged as ordered so the sender clears the right pending map).
                SendAck(connection, sequence, DeliveryMode.ReliableOrdered);

                // ReliableOrdered uses a dedicated sequence space (see #1096): non-ordered
                // packets no longer consume sequence numbers the ordered receiver waits on,
                // so a non-ordered send between two ordered sends can never stall delivery.
                if (!connection.OrderedReceivedSequences.Contains(sequence))
                {
                    connection.OrderedReceivedSequences.Add(sequence);
                    connection.OrderedBuffer[sequence] = payload.ToArray();
                    DeliverOrderedMessages(connection);
                }

                break;
        }
    }

    private void DeliverOrderedMessages(UdpConnection connection)
    {
        while (connection.OrderedBuffer.TryGetValue(connection.NextExpectedSequence, out var data))
        {
            connection.OrderedBuffer.Remove(connection.NextExpectedSequence);
            EnqueueMessage(connection.Id, data);
            connection.NextExpectedSequence++;
        }

        CleanupOrderedSequences(connection);
    }

    private static void CleanupUnorderedSequences(UdpConnection connection)
    {
        // Remove reliable-unordered dedup entries that are too old relative to the
        // highest received unordered sequence.
        var minSequence = (ushort)(connection.HighestReceivedUnorderedSequence - SequenceWindowSize);
        connection.ReceivedSequences.RemoveWhere(s => !IsSequenceNewer(s, minSequence));
    }

    private static void CleanupOrderedSequences(UdpConnection connection)
    {
        // Remove ordered dedup entries that fall outside the sliding window behind the
        // next expected ordered sequence.
        var minSequence = (ushort)(connection.NextExpectedSequence - SequenceWindowSize);
        connection.OrderedReceivedSequences.RemoveWhere(s => !IsSequenceNewer(s, minSequence));
    }

    private static bool IsSequenceNewer(ushort a, ushort b)
    {
        // Handle wraparound - if difference is more than half the sequence space, it wrapped
        return (short)(a - b) > 0;
    }

    private static void ProcessAckPacket(UdpConnection connection, ushort ack, ushort flags)
    {
        // ReliableOrdered and ReliableUnordered have independent sequence spaces, so the
        // ack carries its delivery mode (in the flags field) to pick the right pending map.
        var mode = (DeliveryMode)(flags & 0x03);
        var pendingMap = mode == DeliveryMode.ReliableOrdered
            ? connection.PendingReliableOrdered
            : connection.PendingReliable;

        if (pendingMap.TryRemove(ack, out var pending))
        {
            // Calculate RTT from this ack
            var rtt = (float)(DateTime.UtcNow - pending.SendTime).TotalMilliseconds;
            connection.RoundTripTimeMs = connection.RoundTripTimeMs * 0.8f + rtt * 0.2f; // Smoothed RTT
        }
    }

    private void SendAck(UdpConnection connection, ushort sequence, DeliveryMode mode)
    {
        var ackPacket = new byte[HeaderSize];
        ackPacket[0] = ProtocolVersion;
        ackPacket[1] = PacketAck;
        BitConverter.TryWriteBytes(ackPacket.AsSpan(4), sequence); // ack field
        BitConverter.TryWriteBytes(ackPacket.AsSpan(6), (ushort)mode); // flags: which sequence space

        socket?.Send(ackPacket, connection.Endpoint);
        Interlocked.Add(ref bytesSent, ackPacket.Length);
        Interlocked.Increment(ref packetsSent);
    }

    private void EnqueueMessage(int connectionId, ReadOnlySpan<byte> data)
    {
        events.Enqueue(new TransportEvent
        {
            Kind = TransportEventKind.Data,
            ConnectionId = connectionId,
            Data = data.ToArray()
        });
    }

    private UdpConnection? FindConnectionByEndpoint(IPEndPoint endpoint)
    {
        foreach (var connection in connections.Values)
        {
            if (connection.Endpoint.Equals(endpoint))
            {
                return connection;
            }
        }

        return null;
    }

    /// <inheritdoc/>
    public void Send(int connectionId, ReadOnlySpan<byte> data, DeliveryMode mode)
    {
        ThrowIfDisposed();

        if (State != ConnectionState.Connected)
        {
            return;
        }

        if (data.Length > MaxPayloadSize)
        {
            throw new ArgumentException($"Data exceeds maximum payload size of {MaxPayloadSize} bytes.", nameof(data));
        }

        UdpConnection? connection;
        if (isServer)
        {
            if (!connections.TryGetValue(connectionId, out connection))
            {
                return;
            }
        }
        else
        {
            connection = serverConnection;
        }

        if (connection is null || socket is null)
        {
            return;
        }

        // ReliableOrdered draws from its own sequence space so that Unreliable/
        // UnreliableSequenced sends interleaved with ordered sends do not consume
        // sequence numbers the ordered receiver is waiting on (see #1096).
        var sequence = mode == DeliveryMode.ReliableOrdered
            ? connection.NextSendOrderedSequence++
            : connection.NextSendSequence++;

        var packet = new byte[HeaderSize + data.Length];
        packet[0] = ProtocolVersion;
        packet[1] = PacketData;
        BitConverter.TryWriteBytes(packet.AsSpan(2), sequence);
        BitConverter.TryWriteBytes(packet.AsSpan(6), (ushort)mode);
        data.CopyTo(packet.AsSpan(HeaderSize));

        socket.Send(packet, connection.Endpoint);

        Interlocked.Add(ref connection.BytesSent, packet.Length);
        Interlocked.Increment(ref connection.PacketsSent);
        Interlocked.Add(ref bytesSent, packet.Length);
        Interlocked.Increment(ref packetsSent);

        // Track for reliable modes in the pending map matching the sequence space.
        if (mode == DeliveryMode.ReliableOrdered)
        {
            connection.PendingReliableOrdered[sequence] = new PendingReliablePacket
            {
                Data = packet,
                SendTime = DateTime.UtcNow,
                Attempts = 1
            };
        }
        else if (mode == DeliveryMode.ReliableUnordered)
        {
            connection.PendingReliable[sequence] = new PendingReliablePacket
            {
                Data = packet,
                SendTime = DateTime.UtcNow,
                Attempts = 1
            };
        }
    }

    /// <inheritdoc/>
    public void SendToAll(ReadOnlySpan<byte> data, DeliveryMode mode)
    {
        ThrowIfDisposed();

        if (!isServer)
        {
            throw new InvalidOperationException("SendToAll can only be called on server.");
        }

        foreach (var connectionId in connections.Keys)
        {
            Send(connectionId, data, mode);
        }
    }

    /// <inheritdoc/>
    public void SendToAllExcept(int excludeConnectionId, ReadOnlySpan<byte> data, DeliveryMode mode)
    {
        ThrowIfDisposed();

        if (!isServer)
        {
            throw new InvalidOperationException("SendToAllExcept can only be called on server.");
        }

        foreach (var connectionId in connections.Keys)
        {
            if (connectionId != excludeConnectionId)
            {
                Send(connectionId, data, mode);
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
        }

        if (isServer && connectionId != 0)
        {
            // Disconnect specific client
            if (connections.TryGetValue(connectionId, out var connection))
            {
                SendDisconnectPacket(connection.Endpoint);
                HandleDisconnection(connectionId);
            }

            return;
        }

        // Full disconnect
        State = ConnectionState.Disconnecting;

        if (isServer)
        {
            foreach (var conn in connections.Values)
            {
                SendDisconnectPacket(conn.Endpoint);
            }

            connections.Clear();
        }
        else if (serverEndpoint is not null)
        {
            SendDisconnectPacket(serverEndpoint);
        }

        receiveCts?.Cancel();
        socket?.Dispose();
        socket = null;

        State = ConnectionState.Disconnected;
    }

    private void SendDisconnectPacket(IPEndPoint endpoint)
    {
        if (socket is null)
        {
            return;
        }

        var packet = new byte[HeaderSize];
        packet[0] = ProtocolVersion;
        packet[1] = PacketDisconnect;

        try
        {
            socket.Send(packet, endpoint);
        }
        catch (Exception ex) when (ex is SocketException or ObjectDisposedException)
        {
            // Ignore send errors during disconnect
        }
    }

    private void HandleDisconnection(int connectionId)
    {
        if (isServer)
        {
            if (connections.TryRemove(connectionId, out _))
            {
                // Defer to Update() so the consumer handles it on the game loop (see #1098).
                events.Enqueue(new TransportEvent { Kind = TransportEventKind.Disconnected, ConnectionId = connectionId });
            }
        }
        else
        {
            if (State != ConnectionState.Disconnected)
            {
                State = ConnectionState.Disconnected;
            }
        }
    }

    /// <inheritdoc/>
    public void Update()
    {
        ThrowIfDisposed();

        var now = DateTime.UtcNow;

        // Drain connection, disconnection, and data events in the order they occurred.
        // Dispatching here (rather than from the background receive loop) keeps consumer
        // state mutations on the game-loop thread (see #1098).
        while (events.TryDequeue(out var evt))
        {
            switch (evt.Kind)
            {
                case TransportEventKind.Connected:
                    ClientConnected?.Invoke(evt.ConnectionId);
                    break;

                case TransportEventKind.Disconnected:
                    ClientDisconnected?.Invoke(evt.ConnectionId);
                    break;

                case TransportEventKind.Data:
                    DataReceived?.Invoke(evt.ConnectionId, evt.Data!);
                    break;
            }
        }

        // Handle reliable packet resends and timeouts
        if (isServer)
        {
            foreach (var connectionId in connections.Keys.ToArray())
            {
                if (connections.TryGetValue(connectionId, out var connection))
                {
                    UpdateConnection(connection, now);

                    // Check for timeout
                    if ((now - connection.LastReceiveTime).TotalMilliseconds > ConnectionTimeoutMs)
                    {
                        HandleDisconnection(connectionId);
                    }
                }
            }
        }
        else if (serverConnection is not null && State == ConnectionState.Connected)
        {
            UpdateConnection(serverConnection, now);

            // Check for timeout
            if ((now - serverConnection.LastReceiveTime).TotalMilliseconds > ConnectionTimeoutMs)
            {
                HandleDisconnection(0);
            }
        }
    }

    private void UpdateConnection(UdpConnection connection, DateTime now)
    {
        // Resend reliable packets from both sequence spaces.
        ResendPending(connection, connection.PendingReliable, now);
        ResendPending(connection, connection.PendingReliableOrdered, now);

        // Send keepalive if needed
        if ((now - connection.LastSendTime).TotalMilliseconds > KeepaliveIntervalMs)
        {
            SendKeepalive(connection);
        }
    }

    private void ResendPending(
        UdpConnection connection,
        ConcurrentDictionary<ushort, PendingReliablePacket> pendingMap,
        DateTime now)
    {
        foreach (var kvp in pendingMap)
        {
            var pending = kvp.Value;
            var elapsed = (now - pending.SendTime).TotalMilliseconds;

            if (elapsed > (double)ResendIntervalMs * pending.Attempts)
            {
                if (pending.Attempts >= MaxResendAttempts)
                {
                    // Give up on this packet
                    pendingMap.TryRemove(kvp.Key, out _);
                    Interlocked.Increment(ref packetsLost);
                    Interlocked.Increment(ref connection.PacketsLost);
                }
                else
                {
                    // Resend
                    pending.Attempts++;
                    socket?.Send(pending.Data, connection.Endpoint);
                    Interlocked.Add(ref bytesSent, pending.Data.Length);
                    Interlocked.Increment(ref packetsSent);
                }
            }
        }
    }

    private void SendKeepalive(UdpConnection connection)
    {
        if (socket is null)
        {
            return;
        }

        var packet = new byte[HeaderSize];
        packet[0] = ProtocolVersion;
        packet[1] = PacketKeepalive;

        socket.Send(packet, connection.Endpoint);
        connection.LastSendTime = DateTime.UtcNow;

        Interlocked.Add(ref bytesSent, packet.Length);
        Interlocked.Increment(ref packetsSent);
    }

    /// <inheritdoc/>
    public float GetRoundTripTime(int connectionId)
    {
        if (isServer)
        {
            return connections.TryGetValue(connectionId, out var connection)
                ? connection.RoundTripTimeMs
                : -1;
        }

        return serverConnection?.RoundTripTimeMs ?? -1;
    }

    /// <inheritdoc/>
    public ConnectionStatistics GetStatistics(int connectionId)
    {
        UdpConnection? connection;

        if (isServer)
        {
            connections.TryGetValue(connectionId, out connection);
        }
        else
        {
            connection = serverConnection;
        }

        if (connection is null)
        {
            return new ConnectionStatistics
            {
                RoundTripTimeMs = -1,
                PacketLossPercent = 0,
                BytesSent = 0,
                BytesReceived = 0,
                PacketsSent = 0,
                PacketsReceived = 0,
                PacketsLost = 0
            };
        }

        var sent = Interlocked.Read(ref connection.PacketsSent);
        var lost = Interlocked.Read(ref connection.PacketsLost);
        var lossPercent = sent > 0 ? (float)lost / sent * 100 : 0;

        return new ConnectionStatistics
        {
            RoundTripTimeMs = connection.RoundTripTimeMs,
            PacketLossPercent = lossPercent,
            BytesSent = Interlocked.Read(ref connection.BytesSent),
            BytesReceived = Interlocked.Read(ref connection.BytesReceived),
            PacketsSent = sent,
            PacketsReceived = Interlocked.Read(ref connection.PacketsReceived),
            PacketsLost = lost
        };
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        if (State != ConnectionState.Disconnected)
        {
            Disconnect();
        }

        disposed = true;
        receiveCts?.Dispose();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }

    private sealed class UdpConnection(int id, IPEndPoint endpoint)
    {
        public int Id { get; } = id;
        public IPEndPoint Endpoint { get; } = endpoint;
        public DateTime LastReceiveTime { get; set; } = DateTime.UtcNow;
        public DateTime LastSendTime { get; set; } = DateTime.UtcNow;
        public float RoundTripTimeMs { get; set; }

        public ushort NextSendSequence;
        public ushort NextSendOrderedSequence;
        public ushort LastReceivedSequence;
        public ushort NextExpectedSequence;
        public ushort HighestReceivedUnorderedSequence;

        public HashSet<ushort> ReceivedSequences { get; } = [];
        public HashSet<ushort> OrderedReceivedSequences { get; } = [];
        public Dictionary<ushort, byte[]> OrderedBuffer { get; } = [];
        public ConcurrentDictionary<ushort, PendingReliablePacket> PendingReliable { get; } = new();
        public ConcurrentDictionary<ushort, PendingReliablePacket> PendingReliableOrdered { get; } = new();

        public long BytesSent;
        public long BytesReceived;
        public long PacketsSent;
        public long PacketsReceived;
        public long PacketsLost;
    }

    private sealed class PendingReliablePacket
    {
        public required byte[] Data { get; init; }
        public required DateTime SendTime { get; init; }
        public int Attempts { get; set; }
    }

    private enum TransportEventKind : byte
    {
        Connected,
        Disconnected,
        Data,
    }

    private sealed class TransportEvent
    {
        public required TransportEventKind Kind { get; init; }
        public required int ConnectionId { get; init; }
        public byte[]? Data { get; init; }
    }
}
