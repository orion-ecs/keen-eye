using KeenEyes.Network.Protocol;
using KeenEyes.Network.Serialization;
using KeenEyes.Network.Transport;
using KeenEyes.Network.Transport.Tcp;
using KeenEyes.Network.Transport.Udp;
using KeenEyes.Testing.Network;

namespace KeenEyes.Network.Tests;

#pragma warning disable xUnit1031 // Do not use blocking task operations
#pragma warning disable xUnit1051 // Use TestContext.Current.CancellationToken

/// <summary>
/// Regression tests for #1278: the replication layer must never hand the transport a
/// message larger than <see cref="INetworkTransport.MaxMessageSize"/>, and an oversized
/// payload must be dropped with a diagnostic instead of crashing the server game loop.
/// </summary>
public sealed class FullSnapshotChunkingTests
{
    private const int TickRate = 60;
    private const float TickDelta = 0.02f;

    private static MockNetworkSerializer CreatePositionSerializer()
    {
        var serializer = new MockNetworkSerializer();
        serializer.RegisterComponent<NetPosition>(
            serialize: (ref BitWriter w, NetPosition p) =>
            {
                w.WriteFloat(p.X);
                w.WriteFloat(p.Y);
            },
            deserialize: (ref BitReader r) => new NetPosition { X = r.ReadFloat(), Y = r.ReadFloat() },
            new NetworkComponentInfo
            {
                Type = typeof(NetPosition),
                NetworkTypeId = 1,
                Strategy = SyncStrategy.Authoritative,
                Frequency = 0,
                Priority = 128,
                SupportsInterpolation = false,
                SupportsPrediction = false,
                SupportsDelta = false,
            });
        return serializer;
    }

    #region MaxMessageSize contract

    [Fact]
    public void UdpTransport_MaxMessageSize_IsMtuBudget()
    {
        using var transport = new UdpTransport();
        Assert.Equal(1192, transport.MaxMessageSize);
    }

    [Fact]
    public void TcpTransport_MaxMessageSize_IsFramingLimit()
    {
        using var transport = new TcpTransport();
        Assert.Equal(1024 * 1024, transport.MaxMessageSize);
    }

    [Fact]
    public void LocalTransport_MaxMessageSize_IsUnbounded()
    {
        var (server, client) = LocalTransport.CreatePair();
        using (server)
        using (client)
        {
            Assert.Equal(int.MaxValue, server.MaxMessageSize);
        }
    }

    #endregion

    #region Chunking (deterministic, via recording transport)

    [Fact]
    public void SendFullSnapshot_ManyEntities_EveryMessageFitsTransportBudget()
    {
        using var transport = new RecordingTransport(maxMessageSize: 200);
        using var world = new World();
        var plugin = new NetworkServerPlugin(transport, new ServerNetworkConfig
        {
            TickRate = TickRate,
            Serializer = CreatePositionSerializer(),
        });
        world.InstallPlugin(plugin);

        for (var i = 0; i < 30; i++)
        {
            var entity = world.Spawn().With(new NetPosition { X = i, Y = -i }).Build();
            plugin.RegisterNetworkedEntity(entity);
        }

        plugin.SendFullSnapshot(clientId: 1);

        var snapshotMessages = transport.SentMessages
            .Where(m => PeekType(m.Data) == MessageType.FullSnapshot)
            .ToList();

        Assert.True(snapshotMessages.Count > 1, "30 entities cannot fit one 200-byte message");
        Assert.All(transport.SentMessages, m => Assert.True(
            m.Data.Length <= transport.MaxMessageSize,
            $"message of {m.Data.Length} bytes exceeds the {transport.MaxMessageSize}-byte budget"));

        // Every registered entity arrives across the chunked messages exactly once.
        var serializer = CreatePositionSerializer();
        var seen = new List<uint>();
        foreach (var message in snapshotMessages)
        {
            var reader = new NetworkMessageReader(message.Data);
            reader.ReadHeader(out _, out _);
            var count = reader.ReadEntityCount();
            for (var i = 0; i < count; i++)
            {
                reader.ReadEntitySpawn(out var networkId, out _);
                seen.Add(networkId);

                var componentCount = reader.ReadComponentCount();
                for (var c = 0; c < componentCount; c++)
                {
                    _ = reader.ReadComponent(serializer, out _);
                }
            }
        }

        Assert.Equal(30, seen.Count);
        Assert.Equal(30, seen.Distinct().Count());

        world.UninstallPlugin("NetworkServer");
    }

    [Fact]
    public void SendFullSnapshot_EntityLargerThanBudget_IsSkippedWithoutThrow()
    {
        // Budget so small that the per-entity state (~20 bytes) cannot fit even alone.
        using var transport = new RecordingTransport(maxMessageSize: 12);
        using var world = new World();
        var plugin = new NetworkServerPlugin(transport, new ServerNetworkConfig
        {
            TickRate = TickRate,
            Serializer = CreatePositionSerializer(),
        });
        world.InstallPlugin(plugin);

        var entity = world.Spawn().With(new NetPosition { X = 1, Y = 2 }).Build();
        plugin.RegisterNetworkedEntity(entity);

        // Must not throw; the oversized entity is skipped with a diagnostic.
        plugin.SendFullSnapshot(clientId: 1);

        Assert.All(transport.SentMessages, m =>
            Assert.True(m.Data.Length <= transport.MaxMessageSize));

        world.UninstallPlugin("NetworkServer");
    }

    [Fact]
    public void SendToAll_PayloadLargerThanBudget_IsDroppedWithoutThrow()
    {
        using var transport = new RecordingTransport(maxMessageSize: 100);
        using var world = new World();
        var plugin = new NetworkServerPlugin(transport, new ServerNetworkConfig
        {
            TickRate = TickRate,
            Serializer = CreatePositionSerializer(),
        });
        world.InstallPlugin(plugin);

        plugin.SendToAll(new byte[500], DeliveryMode.ReliableOrdered);
        plugin.SendToClient(1, new byte[500], DeliveryMode.ReliableOrdered);
        plugin.SendToAllExcept(1, new byte[500], DeliveryMode.ReliableOrdered);

        Assert.Empty(transport.SentMessages);

        world.UninstallPlugin("NetworkServer");
    }

    private static MessageType PeekType(byte[] data)
    {
        var reader = new NetworkMessageReader(data);
        return reader.PeekMessageType();
    }

    #endregion

    #region End-to-end over UDP

    [Fact]
    public async Task FullSnapshot_LargerThanUdpBudget_ClientReceivesAllEntities()
    {
        using var server = new UdpTransport();
        using var client = new UdpTransport();

        await server.ListenAsync(0);
        var port = server.LocalPort;

        using var serverWorld = new World();
        using var clientWorld = new World();

        var serverPlugin = new NetworkServerPlugin(server, new ServerNetworkConfig
        {
            TickRate = TickRate,
            Serializer = CreatePositionSerializer(),
        });
        serverWorld.InstallPlugin(serverPlugin);

        // 100 entities x ~18 bytes of snapshot state comfortably exceeds the 1192-byte
        // UDP budget; before the fix this send threw and killed the server update.
        const int EntityCount = 100;
        for (var i = 0; i < EntityCount; i++)
        {
            var entity = serverWorld.Spawn().With(new NetPosition { X = i, Y = i }).Build();
            serverPlugin.RegisterNetworkedEntity(entity);
        }

        var clientPlugin = new NetworkClientPlugin(client, new ClientNetworkConfig
        {
            TickRate = TickRate,
            EnablePrediction = false,
            Serializer = CreatePositionSerializer(),
            ServerAddress = "127.0.0.1",
            ServerPort = port,
        });
        clientWorld.InstallPlugin(clientPlugin);
        clientWorld.Components.Register<NetPosition>();

        using var cts = new CancellationTokenSource(2000);
        try
        {
            await clientPlugin.ConnectAsync(cts.Token);
        }
        catch (Exception e) when (e is TimeoutException or OperationCanceledException)
        {
            Assert.Skip("UDP networking not available in this environment");
            return;
        }

        // Pump both ends until the snapshot chunks are delivered and applied.
        var received = 0;
        for (var i = 0; i < 100 && received < EntityCount; i++)
        {
            serverWorld.Update(TickDelta);
            clientWorld.Update(TickDelta);
            await Task.Delay(10);
            received = clientWorld.Query<NetPosition>().Count();
        }

        Assert.Equal(EntityCount, received);

        clientWorld.UninstallPlugin("NetworkClient");
        serverWorld.UninstallPlugin("NetworkServer");
    }

    #endregion

    /// <summary>
    /// Minimal server-side transport that records every accepted send and enforces a
    /// configurable <see cref="MaxMessageSize"/> the way a datagram transport would.
    /// </summary>
    private sealed class RecordingTransport(int maxMessageSize) : INetworkTransport
    {
        public List<(int ConnectionId, byte[] Data, DeliveryMode Mode)> SentMessages { get; } = [];

        public ConnectionState State => ConnectionState.Connected;
        public bool IsServer => true;
        public bool IsClient => false;
        public int MaxMessageSize { get; } = maxMessageSize;

#pragma warning disable CS0067 // Events required by the interface are never raised here
        public event Action<ConnectionState>? StateChanged;
        public event Action<int>? ClientConnected;
        public event Action<int>? ClientDisconnected;
        public event DataReceivedHandler? DataReceived;
#pragma warning restore CS0067

        public Task ListenAsync(int port, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ConnectAsync(string address, int port, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public void Send(int connectionId, ReadOnlySpan<byte> data, DeliveryMode mode)
        {
            if (data.Length > MaxMessageSize)
            {
                throw new ArgumentException(
                    $"Data exceeds maximum payload size of {MaxMessageSize} bytes.", nameof(data));
            }

            SentMessages.Add((connectionId, data.ToArray(), mode));
        }

        public void SendToAll(ReadOnlySpan<byte> data, DeliveryMode mode) => Send(0, data, mode);

        public void SendToAllExcept(int excludeConnectionId, ReadOnlySpan<byte> data, DeliveryMode mode)
            => Send(0, data, mode);

        public void Disconnect(int connectionId = 0)
        {
        }

        public void Update()
        {
        }

        public float GetRoundTripTime(int connectionId) => 0f;

        public ConnectionStatistics GetStatistics(int connectionId) => default;

        public void Dispose()
        {
        }
    }
}
