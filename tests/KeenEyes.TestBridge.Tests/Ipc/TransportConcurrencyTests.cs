using System.Net;
using System.Net.Sockets;
using System.Reflection;
using KeenEyes.TestBridge.Ipc.Transport;

namespace KeenEyes.TestBridge.Tests.Ipc;

/// <summary>
/// Regression tests for the IPC transport bug cluster: concurrent-send frame
/// interleaving (#1181) and the TCP read-loop socket leak on disconnect (#1180).
/// </summary>
public class TransportConcurrencyTests
{
    #region #1181 - concurrent send frame integrity (shared write path)

    /// <summary>
    /// Deterministically proves that <see cref="IpcFrameProtocol.WriteFrameAsync"/> — the
    /// write path shared by both transports — serializes concurrent frame writes so their
    /// bytes cannot interleave on the shared stream.
    /// </summary>
    /// <remarks>
    /// The write is driven over a stream whose <c>WriteAsync</c> yields between small chunks,
    /// so <em>without</em> the send lock two concurrent frame writes are guaranteed to
    /// interleave and corrupt the framing. Real OS sockets/pipes on some platforms serialize
    /// writes internally, hiding the hazard; this stream removes that platform dependency and
    /// makes the fix's effect observable on every run.
    /// </remarks>
    [Fact]
    public async Task WriteFrameAsync_ConcurrentWrites_DoNotInterleaveOnSharedStream()
    {
        const int payloadSize = 4096;
        const int chunkSize = 64;
        const int senderCount = 8;
        var cancellationToken = TestContext.Current.CancellationToken;

        var sink = new ChunkedYieldingStream(chunkSize);
        using var sendLock = new SemaphoreSlim(1, 1);

        var tasks = new Task[senderCount];
        for (var i = 0; i < senderCount; i++)
        {
            var payload = new byte[payloadSize];
            Array.Fill(payload, (byte)i);
            tasks[i] = IpcFrameProtocol.WriteFrameAsync(sink, sendLock, payload, cancellationToken).AsTask();
        }

        await Task.WhenAll(tasks);

        // Parse the concatenated bytes back into frames. If two writes interleaved, a frame's
        // payload will contain bytes from more than one message ("not solid") or the framing
        // desyncs and the trailing bytes will not parse into exactly senderCount frames.
        var bytes = sink.ToArray();
        var frames = ParseFrames(bytes, payloadSize);

        frames.Count.ShouldBe(
            senderCount,
            "Concurrent frame writes interleaved and the length framing no longer parses into whole frames.");
        foreach (var (solid, length) in frames)
        {
            length.ShouldBe(payloadSize);
            solid.ShouldBeTrue("A frame contained bytes from more than one message — concurrent writes interleaved.");
        }
    }

    [Fact]
    public async Task TcpTransport_ConcurrentSends_PreserveFrameIntegrity()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var port = GetFreePort();

        using var server = new TcpIpcTransport("127.0.0.1", port, isServer: true);
        using var client = new TcpIpcTransport("127.0.0.1", port, isServer: false);

        var listenTask = server.ListenAsync(cancellationToken);
        await client.ConnectAsync(cancellationToken);
        await listenTask;

        await AssertConcurrentSendsPreserveFramingAsync(server, client, cancellationToken);
    }

    [Fact]
    public async Task NamedPipeTransport_ConcurrentSends_PreserveFrameIntegrity()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var pipeName = $"KeenEyes.TestBridge.Tests.{Guid.NewGuid():N}";

        using var server = new NamedPipeTransport(pipeName, isServer: true);
        using var client = new NamedPipeTransport(pipeName, isServer: false);

        var listenTask = server.ListenAsync(cancellationToken);
        await client.ConnectAsync(cancellationToken);
        await listenTask;

        await AssertConcurrentSendsPreserveFramingAsync(server, client, cancellationToken);
    }

    /// <summary>
    /// End-to-end check over a real transport pair: many concurrent <c>SendAsync</c> calls
    /// must all arrive as intact, correctly framed messages. Complements the deterministic
    /// <see cref="WriteFrameAsync_ConcurrentWrites_DoNotInterleaveOnSharedStream"/> test.
    /// </summary>
    private static async Task AssertConcurrentSendsPreserveFramingAsync(
        IIpcTransport sender,
        IIpcTransport receiver,
        CancellationToken cancellationToken)
    {
        const int payloadSize = 256 * 1024;
        const int senderCount = 16;
        const int rounds = 2;
        var expected = senderCount * rounds;

        var received = new System.Collections.Concurrent.ConcurrentQueue<(int Length, bool Solid)>();
        var allReceived = new TaskCompletionSource();
        var countLock = new Lock();
        var count = 0;

        receiver.MessageReceived += memory =>
        {
            var span = memory.Span;
            var first = span.Length > 0 ? span[0] : (byte)0;
            var solid = true;
            for (var i = 1; i < span.Length; i++)
            {
                if (span[i] != first)
                {
                    solid = false;
                    break;
                }
            }

            received.Enqueue((span.Length, solid));
            lock (countLock)
            {
                count++;
                if (count >= expected)
                {
                    allReceived.TrySetResult();
                }
            }
        };

        for (var round = 0; round < rounds; round++)
        {
            var tasks = new Task[senderCount];
            for (var i = 0; i < senderCount; i++)
            {
                var value = (byte)((round * senderCount) + i);
                var payload = new byte[payloadSize];
                Array.Fill(payload, value);
                tasks[i] = sender.SendAsync(payload, cancellationToken).AsTask();
            }

            await Task.WhenAll(tasks);
        }

        var completed = await Task.WhenAny(allReceived.Task, Task.Delay(TimeSpan.FromSeconds(20), cancellationToken));
        completed.ShouldBe(
            allReceived.Task,
            "Not every frame was received — interleaved writes corrupted the length framing and frames were lost.");

        received.Count.ShouldBe(expected);
        foreach (var (length, solid) in received)
        {
            length.ShouldBe(payloadSize, "A received frame had the wrong length — writes interleaved and corrupted framing.");
            solid.ShouldBeTrue("A received frame contained bytes from more than one message — concurrent writes interleaved.");
        }
    }

    #endregion

    #region #1180 - TCP read-loop socket leak on disconnect

    [Fact]
    public async Task TcpTransport_ReadLoopDisconnect_DisposesSocketInsteadOfLeaking()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var port = GetFreePort();

        using var server = new TcpIpcTransport("127.0.0.1", port, isServer: true);
        using var client = new TcpIpcTransport("127.0.0.1", port, isServer: false);

        var listenTask = server.ListenAsync(cancellationToken);
        await client.ConnectAsync(cancellationToken);
        await listenTask;

        server.IsConnected.ShouldBeTrue();

        // The accepted TcpClient is held in a private field; capture it to prove the
        // read-loop disconnect path releases it rather than leaking one socket per
        // reconnect. (Test-only reflection is permitted per project conventions.)
        var clientField = typeof(TcpIpcTransport).GetField(
            "client",
            BindingFlags.Instance | BindingFlags.NonPublic);
        clientField.ShouldNotBeNull();
        clientField.GetValue(server).ShouldNotBeNull("Server should hold the accepted TcpClient while connected.");

        // Close the peer so the server's read loop observes the closed connection
        // and runs its disconnect handling (the buggy path).
        await client.DisconnectAsync();

        // The fixed disconnect path calls Cleanup(), which disposes the socket/stream
        // and clears the field. The old path only flipped isConnected and left the
        // TcpClient/NetworkStream leaked (field stayed populated).
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        while (clientField.GetValue(server) is not null && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20, cancellationToken);
        }

        server.IsConnected.ShouldBeFalse();
        clientField.GetValue(server).ShouldBeNull(
            "Read-loop disconnect must dispose and release the accepted socket; a non-null field means it was leaked.");
    }

    #endregion

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }

    /// <summary>
    /// Parses a byte buffer of concatenated [4-byte length][payload] frames. Returns one
    /// entry per whole frame with whether the payload was "solid" (all bytes equal) and its
    /// declared length. Stops when the remaining bytes cannot form another complete frame.
    /// </summary>
    private static List<(bool Solid, int Length)> ParseFrames(byte[] bytes, int expectedLength)
    {
        var frames = new List<(bool Solid, int Length)>();
        var offset = 0;
        while (offset + 4 <= bytes.Length)
        {
            var length = BitConverter.ToInt32(bytes, offset);
            offset += 4;

            // A corrupted length is itself proof of interleaving; record an impossible frame.
            if (length < 0 || offset + length > bytes.Length)
            {
                frames.Add((false, length));
                break;
            }

            var first = length > 0 ? bytes[offset] : (byte)0;
            var solid = true;
            for (var i = 0; i < length; i++)
            {
                if (bytes[offset + i] != first)
                {
                    solid = false;
                    break;
                }
            }

            frames.Add((solid, length));
            offset += length;

            // Guard against a runaway parse when framing has desynced badly.
            if (length != expectedLength)
            {
                break;
            }
        }

        return frames;
    }

    /// <summary>
    /// A write-only stream that appends each write to an in-memory buffer in small chunks,
    /// yielding between chunks. Concurrent unsynchronized writes therefore interleave
    /// deterministically, which lets a test observe whether the caller serialized its writes.
    /// </summary>
    private sealed class ChunkedYieldingStream(int chunkSize) : Stream
    {
        private readonly List<byte> buffer = [];
        private readonly Lock bufferLock = new();

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public byte[] ToArray()
        {
            lock (bufferLock)
            {
                return buffer.ToArray();
            }
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> memory, CancellationToken cancellationToken = default)
        {
            for (var offset = 0; offset < memory.Length; offset += chunkSize)
            {
                var length = Math.Min(chunkSize, memory.Length - offset);
                var chunk = memory.Slice(offset, length).ToArray();
                lock (bufferLock)
                {
                    buffer.AddRange(chunk);
                }

                // Yield so a concurrent (unserialized) writer can interleave its own chunk.
                await Task.Yield();
            }
        }

        public override void Flush()
        {
            // No-op: writes are captured synchronously.
        }

        public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
