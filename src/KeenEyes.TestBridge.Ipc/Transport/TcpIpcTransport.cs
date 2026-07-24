using System.Buffers;
using System.Net;
using System.Net.Sockets;

namespace KeenEyes.TestBridge.Ipc.Transport;

/// <summary>
/// IPC transport implementation using TCP sockets.
/// </summary>
/// <remarks>
/// <para>
/// TCP transport allows remote debugging from another machine. By default,
/// it binds to localhost for security, but can be configured to accept
/// remote connections.
/// </para>
/// <para>
/// Messages are framed with a 4-byte little-endian length prefix followed by the payload.
/// </para>
/// </remarks>
public sealed class TcpIpcTransport : IIpcTransport
{
    private readonly string bindAddress;
    private readonly int port;
    private readonly bool isServer;
    private readonly Lock stateLock = new();
    private readonly SemaphoreSlim sendLock = new(1, 1);

    private TcpListener? listener;
    private TcpClient? client;
    private NetworkStream? stream;
    private CancellationTokenSource? readCts;
    private Task? readTask;
    private bool isConnected;
    private bool disposed;

    /// <summary>
    /// Creates a new TCP transport.
    /// </summary>
    /// <param name="bindAddress">The IP address to bind to (server) or connect to (client).</param>
    /// <param name="port">The TCP port.</param>
    /// <param name="isServer">True if this is the server (listener) side.</param>
    public TcpIpcTransport(string bindAddress, int port, bool isServer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bindAddress);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(port);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(port, 65535);

        this.bindAddress = bindAddress;
        this.port = port;
        this.isServer = isServer;
    }

    /// <inheritdoc />
    public bool IsConnected
    {
        get
        {
            lock (stateLock)
            {
                return isConnected && !disposed;
            }
        }
    }

    /// <inheritdoc />
    public event Action<bool>? ConnectionChanged;

    /// <inheritdoc />
    public event Action<ReadOnlyMemory<byte>>? MessageReceived;

    /// <inheritdoc />
    public async Task ListenAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (!isServer)
        {
            throw new InvalidOperationException("Cannot listen on a client transport.");
        }

        lock (stateLock)
        {
            if (isConnected)
            {
                throw new InvalidOperationException("Already connected.");
            }
        }

        var ipAddress = IPAddress.Parse(bindAddress);
        listener = new TcpListener(ipAddress, port);
        listener.Start(1);

        try
        {
            client = await listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
            client.NoDelay = true;

            lock (stateLock)
            {
                stream = client.GetStream();
                isConnected = true;
            }

            StartReadLoop();
            ConnectionChanged?.Invoke(true);
        }
        catch
        {
            listener.Stop();
            listener = null;
            throw;
        }
        finally
        {
            // Stop listening after first connection (point-to-point)
            listener?.Stop();
        }
    }

    /// <inheritdoc />
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (isServer)
        {
            throw new InvalidOperationException("Cannot connect from a server transport.");
        }

        lock (stateLock)
        {
            if (isConnected)
            {
                throw new InvalidOperationException("Already connected.");
            }
        }

        client = new TcpClient { NoDelay = true };

        try
        {
            await client.ConnectAsync(bindAddress, port, cancellationToken).ConfigureAwait(false);

            lock (stateLock)
            {
                stream = client.GetStream();
                isConnected = true;
            }

            StartReadLoop();
            ConnectionChanged?.Invoke(true);
        }
        catch
        {
            client.Dispose();
            client = null;
            throw;
        }
    }

    /// <inheritdoc />
    public async ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        NetworkStream? networkStream;
        lock (stateLock)
        {
            if (!isConnected || stream == null)
            {
                throw new InvalidOperationException("Not connected.");
            }
            networkStream = stream;
        }

        await IpcFrameProtocol.WriteFrameAsync(networkStream, sendLock, data, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DisconnectAsync()
    {
        lock (stateLock)
        {
            if (!isConnected)
            {
                return;
            }
        }

        // Cancel the read loop
        readCts?.Cancel();

        // Wait for read task to complete
        if (readTask != null)
        {
            try
            {
                await readTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }

        Cleanup();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        readCts?.Cancel();
        Cleanup();
        readCts?.Dispose();
        sendLock.Dispose();
    }

    private void StartReadLoop()
    {
        // Dispose the previous token source so reconnects do not leak one CTS
        // per accepted connection. The prior read loop has already exited by
        // the time a new connection is accepted on this point-to-point transport.
        readCts?.Dispose();
        readCts = new CancellationTokenSource();
        readTask = ReadLoopAsync(readCts.Token);
    }

    private async Task ReadLoopAsync(CancellationToken cancellationToken)
    {
        var headerBuffer = new byte[IpcFrameProtocol.HeaderSize];

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                NetworkStream? networkStream;
                lock (stateLock)
                {
                    if (!isConnected || stream == null)
                    {
                        break;
                    }
                    networkStream = stream;
                }

                // Read header
                var headerBytesRead = await ReadExactAsync(networkStream, headerBuffer, cancellationToken).ConfigureAwait(false);
                if (headerBytesRead < IpcFrameProtocol.HeaderSize)
                {
                    // Connection closed
                    break;
                }

                // Parse message length
                var messageLength = BitConverter.ToInt32(headerBuffer, 0);
                if (messageLength <= 0 || messageLength > IpcFrameProtocol.MaxMessageSize)
                {
                    // Invalid message, disconnect
                    break;
                }

                // Read payload
                var payloadBuffer = ArrayPool<byte>.Shared.Rent(messageLength);
                try
                {
                    var payloadBytesRead = await ReadExactAsync(networkStream, payloadBuffer.AsMemory(0, messageLength), cancellationToken).ConfigureAwait(false);
                    if (payloadBytesRead < messageLength)
                    {
                        // Connection closed mid-message
                        break;
                    }

                    // Dispatch message
                    MessageReceived?.Invoke(payloadBuffer.AsMemory(0, messageLength));
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(payloadBuffer);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected cancellation
        }
        catch (IOException)
        {
            // Connection closed
        }
        catch (SocketException)
        {
            // Connection error
        }
        catch (ObjectDisposedException)
        {
            // Stream disposed
        }

        // Handle disconnection: dispose the socket/stream here so a reconnect
        // does not leak the previous TcpClient/NetworkStream. Mirrors the
        // named-pipe transport, which calls CleanupAsync on read-loop exit.
        Cleanup();
    }

    private static async Task<int> ReadExactAsync(NetworkStream stream, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        var totalRead = 0;
        while (totalRead < buffer.Length)
        {
            var bytesRead = await stream.ReadAsync(buffer[totalRead..], cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                // End of stream
                break;
            }
            totalRead += bytesRead;
        }
        return totalRead;
    }

    private void Cleanup()
    {
        bool wasConnected;
        lock (stateLock)
        {
            wasConnected = isConnected;
            isConnected = false;
            stream = null;
        }

        client?.Dispose();
        client = null;

        listener?.Stop();
        listener = null;

        if (wasConnected)
        {
            ConnectionChanged?.Invoke(false);
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }
}
