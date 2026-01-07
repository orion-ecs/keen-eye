using System.Buffers;
using System.IO.Pipes;

namespace KeenEyes.TestBridge.Ipc.Transport;

/// <summary>
/// IPC transport implementation using named pipes.
/// </summary>
/// <remarks>
/// <para>
/// Named pipes provide efficient local IPC communication. On Windows, this uses
/// Windows named pipes. On Linux and macOS, this uses Unix domain sockets.
/// </para>
/// <para>
/// Messages are framed with a 4-byte little-endian length prefix followed by the payload.
/// </para>
/// </remarks>
public sealed class NamedPipeTransport : IIpcTransport
{
    private const int HeaderSize = 4;
    private const int MaxMessageSize = 16 * 1024 * 1024; // 16MB for screenshots

    private readonly string pipeName;
    private readonly bool isServer;
    private readonly Lock stateLock = new();

    private NamedPipeServerStream? serverPipe;
    private NamedPipeClientStream? clientPipe;
    private PipeStream? activePipe;
    private CancellationTokenSource? readCts;
    private Task? readTask;
    private bool isConnected;
    private bool disposed;

    /// <summary>
    /// Creates a new named pipe transport.
    /// </summary>
    /// <param name="pipeName">The pipe name.</param>
    /// <param name="isServer">True if this is the server (listener) side.</param>
    public NamedPipeTransport(string pipeName, bool isServer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipeName);
        this.pipeName = pipeName;
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

        serverPipe = new NamedPipeServerStream(
            pipeName,
            PipeDirection.InOut,
            1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous);

        try
        {
            await serverPipe.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);

            lock (stateLock)
            {
                activePipe = serverPipe;
                isConnected = true;
            }

            StartReadLoop();
            ConnectionChanged?.Invoke(true);
        }
        catch
        {
            await serverPipe.DisposeAsync().ConfigureAwait(false);
            serverPipe = null;
            throw;
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

        clientPipe = new NamedPipeClientStream(
            ".",
            pipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous);

        try
        {
            await clientPipe.ConnectAsync(cancellationToken).ConfigureAwait(false);

            lock (stateLock)
            {
                activePipe = clientPipe;
                isConnected = true;
            }

            StartReadLoop();
            ConnectionChanged?.Invoke(true);
        }
        catch
        {
            await clientPipe.DisposeAsync().ConfigureAwait(false);
            clientPipe = null;
            throw;
        }
    }

    /// <inheritdoc />
    public async ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        PipeStream? pipe;
        lock (stateLock)
        {
            if (!isConnected || activePipe == null)
            {
                throw new InvalidOperationException("Not connected.");
            }
            pipe = activePipe;
        }

        if (data.Length > MaxMessageSize)
        {
            throw new ArgumentException($"Message size {data.Length} exceeds maximum {MaxMessageSize}.", nameof(data));
        }

        // Create framed message: [4-byte length][payload]
        var frameSize = HeaderSize + data.Length;
        var buffer = ArrayPool<byte>.Shared.Rent(frameSize);

        try
        {
            // Write length header (little-endian)
            BitConverter.TryWriteBytes(buffer.AsSpan(0, HeaderSize), data.Length);

            // Write payload
            data.Span.CopyTo(buffer.AsSpan(HeaderSize));

            await pipe.WriteAsync(buffer.AsMemory(0, frameSize), cancellationToken).ConfigureAwait(false);
            await pipe.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
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

        await CleanupAsync().ConfigureAwait(false);
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

        serverPipe?.Dispose();
        clientPipe?.Dispose();
        readCts?.Dispose();

        lock (stateLock)
        {
            isConnected = false;
            activePipe = null;
        }
    }

    private void StartReadLoop()
    {
        readCts = new CancellationTokenSource();
        readTask = ReadLoopAsync(readCts.Token);
    }

    private async Task ReadLoopAsync(CancellationToken cancellationToken)
    {
        var headerBuffer = new byte[HeaderSize];

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                PipeStream? pipe;
                lock (stateLock)
                {
                    if (!isConnected || activePipe == null)
                    {
                        break;
                    }
                    pipe = activePipe;
                }

                // Read header
                var headerBytesRead = await ReadExactAsync(pipe, headerBuffer, cancellationToken).ConfigureAwait(false);
                if (headerBytesRead < HeaderSize)
                {
                    // Connection closed
                    break;
                }

                // Parse message length
                var messageLength = BitConverter.ToInt32(headerBuffer, 0);
                if (messageLength <= 0 || messageLength > MaxMessageSize)
                {
                    // Invalid message, disconnect
                    break;
                }

                // Read payload
                var payloadBuffer = ArrayPool<byte>.Shared.Rent(messageLength);
                try
                {
                    var payloadBytesRead = await ReadExactAsync(pipe, payloadBuffer.AsMemory(0, messageLength), cancellationToken).ConfigureAwait(false);
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
            // Pipe closed
        }
        catch (ObjectDisposedException)
        {
            // Pipe disposed
        }

        // Handle disconnection
        await CleanupAsync().ConfigureAwait(false);
    }

    private static async Task<int> ReadExactAsync(PipeStream pipe, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        var totalRead = 0;
        while (totalRead < buffer.Length)
        {
            var bytesRead = await pipe.ReadAsync(buffer[totalRead..], cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                // End of stream
                break;
            }
            totalRead += bytesRead;
        }
        return totalRead;
    }

    private async Task CleanupAsync()
    {
        bool wasConnected;
        lock (stateLock)
        {
            wasConnected = isConnected;
            isConnected = false;
            activePipe = null;
        }

        if (serverPipe != null)
        {
            await serverPipe.DisposeAsync().ConfigureAwait(false);
            serverPipe = null;
        }

        if (clientPipe != null)
        {
            await clientPipe.DisposeAsync().ConfigureAwait(false);
            clientPipe = null;
        }

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
