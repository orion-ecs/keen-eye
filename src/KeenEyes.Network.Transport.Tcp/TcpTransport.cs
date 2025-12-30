using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using KeenEyes.Network.Transport;

namespace KeenEyes.Network.Transport.Tcp;

/// <summary>
/// TCP-based network transport providing reliable ordered delivery.
/// </summary>
/// <remarks>
/// <para>
/// TCP provides inherently reliable, ordered delivery making it suitable for
/// games that prioritize reliability over minimal latency. All delivery modes
/// map to TCP's reliable ordered semantics.
/// </para>
/// <para>
/// Messages are framed using a 4-byte length prefix to handle TCP's stream nature.
/// </para>
/// <para>
/// For games requiring unreliable delivery or lower latency, consider UdpTransport
/// from the KeenEyes.Network.Transport.Udp package.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Server
/// var serverTransport = new TcpTransport();
/// await serverTransport.ListenAsync(7777);
///
/// // Client
/// var clientTransport = new TcpTransport();
/// await clientTransport.ConnectAsync("localhost", 7777);
/// </code>
/// </example>
public sealed class TcpTransport : INetworkTransport
{
    private const int HeaderSize = 4; // 4 bytes for message length
    private const int MaxMessageSize = 1024 * 1024; // 1 MB max message

    private readonly ConcurrentDictionary<int, ClientConnection> connections = new();
    private readonly ConcurrentQueue<PendingMessage> incomingMessages = new();
    private readonly Lock stateLock = new();

    private TcpListener? listener;
    private TcpClient? clientSocket;
    private NetworkStream? clientStream;
    private ClientConnection? serverConnection;
    private CancellationTokenSource? listenerCts;
    private ConnectionState state = ConnectionState.Disconnected;
    private int nextConnectionId = 1;
    private bool isServer;
    private bool disposed;

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

        listener = new TcpListener(IPAddress.Any, port);
        listener.Start();

        listenerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        State = ConnectionState.Connected;

        // Start accepting connections in background
        _ = AcceptConnectionsAsync(listenerCts.Token);
    }

    private async Task AcceptConnectionsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && listener is not null)
        {
            try
            {
                var client = await listener.AcceptTcpClientAsync(cancellationToken);
                var connectionId = Interlocked.Increment(ref nextConnectionId) - 1;

                var connection = new ClientConnection(connectionId, client);
                connections[connectionId] = connection;

                // Start receiving for this client
                _ = ReceiveFromClientAsync(connection, cancellationToken);

                ClientConnected?.Invoke(connectionId);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (SocketException)
            {
                // Listener was stopped
                break;
            }
        }
    }

    private async Task ReceiveFromClientAsync(ClientConnection connection, CancellationToken cancellationToken)
    {
        var headerBuffer = new byte[HeaderSize];

        try
        {
            while (!cancellationToken.IsCancellationRequested && connection.IsConnected)
            {
                // Read message length header
                var headerBytesRead = await ReadExactAsync(connection.Stream, headerBuffer, HeaderSize, cancellationToken);
                if (headerBytesRead < HeaderSize)
                {
                    break; // Connection closed
                }

                var messageLength = BitConverter.ToInt32(headerBuffer, 0);
                if (messageLength <= 0 || messageLength > MaxMessageSize)
                {
                    break; // Invalid message, disconnect
                }

                // Read message body
                var messageBuffer = new byte[messageLength];
                var bodyBytesRead = await ReadExactAsync(connection.Stream, messageBuffer, messageLength, cancellationToken);
                if (bodyBytesRead < messageLength)
                {
                    break; // Connection closed
                }

                Interlocked.Add(ref connection.BytesReceived, HeaderSize + messageLength);
                Interlocked.Increment(ref connection.PacketsReceived);
                Interlocked.Add(ref bytesReceived, HeaderSize + messageLength);
                Interlocked.Increment(ref packetsReceived);

                incomingMessages.Enqueue(new PendingMessage
                {
                    ConnectionId = connection.Id,
                    Data = messageBuffer
                });
            }
        }
        catch (IOException)
        {
            // Connection lost
        }
        catch (OperationCanceledException)
        {
            // Cancelled
        }
        finally
        {
            HandleClientDisconnection(connection.Id);
        }
    }

    private void HandleClientDisconnection(int connectionId)
    {
        if (connections.TryRemove(connectionId, out var connection))
        {
            connection.Dispose();
            ClientDisconnected?.Invoke(connectionId);
        }
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
            clientSocket = new TcpClient();
            await clientSocket.ConnectAsync(address, port, cancellationToken);
            clientStream = clientSocket.GetStream();

            serverConnection = new ClientConnection(0, clientSocket);

            State = ConnectionState.Connected;

            // Start receiving in background
            _ = ReceiveFromServerAsync(cancellationToken);
        }
        catch
        {
            State = ConnectionState.Disconnected;
            throw;
        }
    }

    private async Task ReceiveFromServerAsync(CancellationToken cancellationToken)
    {
        if (clientStream is null || serverConnection is null)
        {
            return;
        }

        var headerBuffer = new byte[HeaderSize];

        try
        {
            while (!cancellationToken.IsCancellationRequested && State == ConnectionState.Connected)
            {
                // Read message length header
                var headerBytesRead = await ReadExactAsync(clientStream, headerBuffer, HeaderSize, cancellationToken);
                if (headerBytesRead < HeaderSize)
                {
                    break; // Connection closed
                }

                var messageLength = BitConverter.ToInt32(headerBuffer, 0);
                if (messageLength <= 0 || messageLength > MaxMessageSize)
                {
                    break; // Invalid message, disconnect
                }

                // Read message body
                var messageBuffer = new byte[messageLength];
                var bodyBytesRead = await ReadExactAsync(clientStream, messageBuffer, messageLength, cancellationToken);
                if (bodyBytesRead < messageLength)
                {
                    break; // Connection closed
                }

                Interlocked.Add(ref serverConnection.BytesReceived, HeaderSize + messageLength);
                Interlocked.Increment(ref serverConnection.PacketsReceived);
                Interlocked.Add(ref bytesReceived, HeaderSize + messageLength);
                Interlocked.Increment(ref packetsReceived);

                incomingMessages.Enqueue(new PendingMessage
                {
                    ConnectionId = 0, // Server is always ID 0 for clients
                    Data = messageBuffer
                });
            }
        }
        catch (IOException)
        {
            // Connection lost
        }
        catch (OperationCanceledException)
        {
            // Cancelled
        }
        finally
        {
            if (State == ConnectionState.Connected)
            {
                State = ConnectionState.Disconnected;
            }
        }
    }

    private static async Task<int> ReadExactAsync(NetworkStream stream, byte[] buffer, int count, CancellationToken cancellationToken)
    {
        var totalRead = 0;
        while (totalRead < count)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(totalRead, count - totalRead), cancellationToken);
            if (read == 0)
            {
                return totalRead; // Connection closed
            }

            totalRead += read;
        }

        return totalRead;
    }

    /// <inheritdoc/>
    public void Send(int connectionId, ReadOnlySpan<byte> data, DeliveryMode mode)
    {
        ThrowIfDisposed();

        if (State != ConnectionState.Connected)
        {
            return;
        }

        NetworkStream? stream;
        ClientConnection? connection;

        if (isServer)
        {
            if (!connections.TryGetValue(connectionId, out connection))
            {
                return;
            }

            stream = connection.Stream;
        }
        else
        {
            stream = clientStream;
            connection = serverConnection;
        }

        if (stream is null || connection is null)
        {
            return;
        }

        try
        {
            // Prepare length-prefixed message
            var header = BitConverter.GetBytes(data.Length);
            stream.Write(header);
            stream.Write(data);
            stream.Flush();

            Interlocked.Add(ref connection.BytesSent, HeaderSize + data.Length);
            Interlocked.Increment(ref connection.PacketsSent);
            Interlocked.Add(ref bytesSent, HeaderSize + data.Length);
            Interlocked.Increment(ref packetsSent);
        }
        catch (IOException)
        {
            // Connection lost
            if (isServer)
            {
                HandleClientDisconnection(connectionId);
            }
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
            HandleClientDisconnection(connectionId);
            return;
        }

        // Full disconnect
        State = ConnectionState.Disconnecting;

        if (isServer)
        {
            listenerCts?.Cancel();
            listener?.Stop();
            listener = null;

            foreach (var connId in connections.Keys.ToArray())
            {
                HandleClientDisconnection(connId);
            }
        }
        else
        {
            serverConnection?.Dispose();
            serverConnection = null;
            clientStream?.Dispose();
            clientStream = null;
            clientSocket?.Dispose();
            clientSocket = null;
        }

        State = ConnectionState.Disconnected;
    }

    /// <inheritdoc/>
    public void Update()
    {
        ThrowIfDisposed();

        while (incomingMessages.TryDequeue(out var message))
        {
            DataReceived?.Invoke(message.ConnectionId, message.Data);
        }
    }

    /// <inheritdoc/>
    public float GetRoundTripTime(int connectionId)
    {
        // TCP doesn't provide RTT directly
        // For accurate RTT, implement ping/pong at application level
        return -1;
    }

    /// <inheritdoc/>
    public ConnectionStatistics GetStatistics(int connectionId)
    {
        ClientConnection? connection;

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

        return new ConnectionStatistics
        {
            RoundTripTimeMs = -1, // TCP doesn't expose RTT
            PacketLossPercent = 0, // TCP guarantees delivery
            BytesSent = Interlocked.Read(ref connection.BytesSent),
            BytesReceived = Interlocked.Read(ref connection.BytesReceived),
            PacketsSent = Interlocked.Read(ref connection.PacketsSent),
            PacketsReceived = Interlocked.Read(ref connection.PacketsReceived),
            PacketsLost = 0 // TCP guarantees delivery
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
        listenerCts?.Dispose();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }

    private sealed class ClientConnection(int id, TcpClient client) : IDisposable
    {
        public int Id { get; } = id;
        public NetworkStream Stream { get; } = client.GetStream();
        public bool IsConnected => client.Connected;

        public long BytesSent;
        public long BytesReceived;
        public long PacketsSent;
        public long PacketsReceived;

        public void Dispose()
        {
            Stream.Dispose();
            client.Dispose();
        }
    }

    private sealed class PendingMessage
    {
        public required int ConnectionId { get; init; }
        public required byte[] Data { get; init; }
    }
}
