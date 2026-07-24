using System.Buffers;

namespace KeenEyes.TestBridge.Ipc.Transport;

/// <summary>
/// Shared framing helper for the IPC transports. Messages are framed with a
/// 4-byte little-endian length prefix followed by the payload.
/// </summary>
/// <remarks>
/// Both <see cref="TcpIpcTransport"/> and <see cref="NamedPipeTransport"/> use this
/// helper so the write path — and its concurrency guarantees — live in one place.
/// </remarks>
internal static class IpcFrameProtocol
{
    /// <summary>Size, in bytes, of the little-endian length prefix that precedes each payload.</summary>
    public const int HeaderSize = 4;

    /// <summary>Maximum payload size, in bytes, that a single frame may carry (16 MB, sized for screenshots).</summary>
    public const int MaxMessageSize = 16 * 1024 * 1024;

    /// <summary>
    /// Writes a single framed message to <paramref name="stream"/>, serializing the
    /// write and flush behind <paramref name="sendLock"/>.
    /// </summary>
    /// <param name="stream">The destination stream.</param>
    /// <param name="sendLock">
    /// The per-transport send gate. Holding it across the write and flush guarantees
    /// concurrent senders cannot interleave bytes from different frames on the shared
    /// stream and corrupt the length framing.
    /// </param>
    /// <param name="data">The payload to frame and send.</param>
    /// <param name="cancellationToken">A token to cancel the send.</param>
    /// <returns>A task that completes once the frame has been written and flushed.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="data"/> exceeds <see cref="MaxMessageSize"/>.</exception>
    public static async ValueTask WriteFrameAsync(
        Stream stream,
        SemaphoreSlim sendLock,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken)
    {
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

            // Serialize the write+flush so concurrent senders cannot interleave
            // bytes from different frames on the shared stream and corrupt framing.
            await sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await stream.WriteAsync(buffer.AsMemory(0, frameSize), cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                sendLock.Release();
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
