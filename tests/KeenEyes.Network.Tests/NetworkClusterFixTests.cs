using System.Reflection;
using KeenEyes.Network.Components;
using KeenEyes.Network.Prediction;
using KeenEyes.Network.Protocol;
using KeenEyes.Network.Replication;
using KeenEyes.Network.Serialization;
using KeenEyes.Network.Systems;
using KeenEyes.Network.Transport;
using KeenEyes.Network.Transport.Udp;
using KeenEyes.Testing.Network;

namespace KeenEyes.Network.Tests;

// LocalTransport completes synchronously, so Wait() is safe.
#pragma warning disable xUnit1031 // Do not use blocking task operations
#pragma warning disable xUnit1051 // Use TestContext.Current.CancellationToken

/// <summary>
/// Regression tests for the networking bug cluster (#1096-#1105).
/// </summary>
/// <remarks>
/// Reuses the replicated component types declared by the other network test files in
/// this namespace (<see cref="NetPosition"/>, <see cref="NetVelocity"/>,
/// <see cref="NetInput"/>, <see cref="OwnerState"/>). Each test fails on the pre-fix
/// source and passes on the fixed source.
/// </remarks>
public sealed class NetworkClusterFixTests
{
    private const int TickRate = 60;
    private const float TickDelta = 0.02f; // > 1/60s, so exactly one network tick per update.

    #region Serializer helpers

    private static MockNetworkSerializer CreatePositionSerializer()
    {
        var serializer = new MockNetworkSerializer();
        RegisterNetPosition(serializer);
        return serializer;
    }

    private static void RegisterNetPosition(MockNetworkSerializer serializer)
    {
        serializer.RegisterComponentWithDelta<NetPosition>(
            serialize: (ref BitWriter w, NetPosition p) =>
            {
                w.WriteFloat(p.X);
                w.WriteFloat(p.Y);
            },
            deserialize: (ref BitReader r) => new NetPosition { X = r.ReadFloat(), Y = r.ReadFloat() },
            getDirtyMask: (NetPosition current, NetPosition baseline) => current.GetDirtyMask(baseline),
            serializeDelta: (ref BitWriter w, NetPosition current, NetPosition baseline, uint mask) =>
                current.NetworkSerializeDelta(ref w, baseline, mask),
            deserializeDelta: (ref BitReader r, NetPosition baseline, uint mask) =>
            {
                new NetPosition().NetworkDeserializeDelta(ref r, ref baseline, mask);
                return baseline;
            },
            new NetworkComponentInfo
            {
                Type = typeof(NetPosition),
                NetworkTypeId = 1,
                Strategy = SyncStrategy.Authoritative,
                Frequency = 0,
                Priority = 128,
                SupportsInterpolation = true,
                SupportsPrediction = true,
                SupportsDelta = true,
            });
    }

    private static void RegisterNetVelocity(MockNetworkSerializer serializer)
    {
        serializer.RegisterComponentWithDelta<NetVelocity>(
            serialize: (ref BitWriter w, NetVelocity v) => w.WriteFloat(v.VX),
            deserialize: (ref BitReader r) => new NetVelocity { VX = r.ReadFloat() },
            getDirtyMask: (NetVelocity current, NetVelocity baseline) => current.GetDirtyMask(baseline),
            serializeDelta: (ref BitWriter w, NetVelocity current, NetVelocity baseline, uint mask) =>
                current.NetworkSerializeDelta(ref w, baseline, mask),
            deserializeDelta: (ref BitReader r, NetVelocity baseline, uint mask) =>
            {
                new NetVelocity().NetworkDeserializeDelta(ref r, ref baseline, mask);
                return baseline;
            },
            new NetworkComponentInfo
            {
                Type = typeof(NetVelocity),
                NetworkTypeId = 2,
                Strategy = SyncStrategy.Authoritative,
                Frequency = 0,
                Priority = 128,
                SupportsInterpolation = false,
                SupportsPrediction = true,
                SupportsDelta = true,
            });
    }

    private static void RegisterOwnerState(MockNetworkSerializer serializer)
    {
        serializer.RegisterComponent<OwnerState>(
            serialize: (ref BitWriter w, OwnerState c) => w.WriteFloat(c.Value),
            deserialize: (ref BitReader r) => new OwnerState { Value = r.ReadFloat() },
            new NetworkComponentInfo
            {
                Type = typeof(OwnerState),
                NetworkTypeId = 3,
                Strategy = SyncStrategy.OwnerAuthoritative,
                Frequency = 0,
                Priority = 128,
                SupportsInterpolation = false,
                SupportsPrediction = false,
                SupportsDelta = false,
            });
    }

    private sealed class NetInputSerializer : IInputSerializer<NetInput>
    {
        public Type InputType => typeof(NetInput);

        public void Serialize(in NetInput input, ref BitWriter writer)
        {
            writer.WriteUInt32(input.Tick);
            writer.WriteFloat(input.DeltaX);
        }

        public NetInput Deserialize(ref BitReader reader) =>
            new() { Tick = reader.ReadUInt32(), DeltaX = reader.ReadFloat() };

        void IInputSerializer.Serialize(object input, ref BitWriter writer) => Serialize((NetInput)input, ref writer);

        object IInputSerializer.Deserialize(ref BitReader reader) => Deserialize(ref reader);
    }

    #endregion

    #region UDP connect helper

    private static async Task<(UdpTransport Server, UdpTransport Client, int Port)?> CreateConnectedUdpPairAsync(int timeoutMs = 2000)
    {
        var server = new UdpTransport();
        var client = new UdpTransport();

        try
        {
            await server.ListenAsync(0);
            var port = server.LocalPort;

            using var cts = new CancellationTokenSource(timeoutMs);
            await client.ConnectAsync("127.0.0.1", port, cts.Token);
            return (server, client, port);
        }
        catch (Exception e) when (e is TimeoutException or OperationCanceledException)
        {
            server.Dispose();
            client.Dispose();
            return null;
        }
    }

    #endregion

    #region #1096 - UDP ReliableOrdered delivery must not stall behind other delivery modes

    [Fact]
    public async Task Udp_ReliableOrdered_InterleavedWithUnreliableSequenced_BothOrderedMessagesDeliver()
    {
        var pair = await CreateConnectedUdpPairAsync();
        Assert.SkipWhen(pair is null, "UDP networking not available in this environment");

        using var server = pair!.Value.Server;
        using var client = pair.Value.Client;

        await Task.Delay(50);
        server.Update();

        var received = new List<byte>();
        server.DataReceived += (_, data) =>
        {
            if (data.Length > 0)
            {
                received.Add(data[0]);
            }
        };

        // ReliableOrdered[1], then an UnreliableSequenced[2] between the ordered sends,
        // then ReliableOrdered[3]. Before the fix the non-ordered packet consumed the
        // sequence number the ordered receiver was waiting on, so [3] never delivered.
        client.Send(0, [1], DeliveryMode.ReliableOrdered);
        client.Send(0, [2], DeliveryMode.UnreliableSequenced);
        client.Send(0, [3], DeliveryMode.ReliableOrdered);

        await Task.Delay(150);
        server.Update();
        server.Update();

        Assert.Contains((byte)1, received);
        Assert.Contains((byte)3, received);
    }

    #endregion

    #region #1097 - Prediction must not write LastConfirmedTick through a stale ref

    [Fact]
    public void OnServerStateReceived_ServerAddsAbsentComponent_ConfirmedTickSurvivesMigration()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var clientWorld = new World();

        var serializer = new MockNetworkSerializer();
        RegisterNetPosition(serializer);
        RegisterNetVelocity(serializer);

        var clientPlugin = new NetworkClientPlugin(client, new ClientNetworkConfig
        {
            TickRate = TickRate,
            EnablePrediction = true,
            Serializer = serializer,
        });
        clientWorld.InstallPlugin(clientPlugin);

        // Replicated components must be registered on the receiving world; NetVelocity is
        // the registered-but-absent component the server will add (triggering migration).
        clientWorld.Components.Register<NetPosition>();
        clientWorld.Components.Register<NetVelocity>();

        clientPlugin.UpdateTick(5);

        // Locally owned predicted entity that has NetPosition but NOT NetVelocity.
        var entity = clientPlugin.SpawnNetworkedEntity(networkId: 1, ownerId: clientPlugin.LocalClientId);
        clientWorld.Add(entity, new NetPosition { X = 10f, Y = 0f });

        clientWorld.Update(0.016f); // prediction system saves a tick-5 snapshot (buffer exists)

        // Server state for tick 6 (no saved prediction) carries a component the entity lacks.
        // Applying it migrates the entity to a new archetype; before the fix the subsequent
        // LastConfirmedTick write landed in the vacated slot and was lost.
        var serverStates = new Dictionary<Type, object>
        {
            [typeof(NetVelocity)] = new NetVelocity { VX = 7f },
        };
        clientPlugin.PredictionSystem!.OnServerStateReceived(entity, serverTick: 6, serverStates);

        Assert.True(clientWorld.Has<NetVelocity>(entity)); // migration happened
        Assert.Equal(7f, clientWorld.Get<NetVelocity>(entity).VX, 0.001f);
        Assert.Equal(6u, clientWorld.Get<PredictionState>(entity).LastConfirmedTick);

        clientWorld.UninstallPlugin("NetworkClient");
        client.Dispose();
        server.Dispose();
    }

    #endregion

    #region #1098 - Connection events must fire on the game loop, not the receive thread

    [Fact]
    public async Task Udp_ClientConnected_FiresDuringUpdate_NotOnReceiveThread()
    {
        using var server = new UdpTransport();
        using var client = new UdpTransport();

        await server.ListenAsync(0);
        var port = server.LocalPort;

        int? eventThreadId = null;
        server.ClientConnected += _ => eventThreadId = Environment.CurrentManagedThreadId;

        using var cts = new CancellationTokenSource(2000);
        try
        {
            await client.ConnectAsync("127.0.0.1", port, cts.Token);
        }
        catch (Exception e) when (e is TimeoutException or OperationCanceledException)
        {
            Assert.Skip("UDP networking not available in this environment");
            return;
        }

        // The server's background receive loop has processed the connect handshake by the
        // time ConnectAsync returns (it sent the ack). Before the fix ClientConnected had
        // already fired on that background thread; after the fix it is queued for Update().
        await Task.Delay(100);
        Assert.Null(eventThreadId);

        var updateThreadId = Environment.CurrentManagedThreadId;
        server.Update();

        Assert.NotNull(eventThreadId);
        Assert.Equal(updateThreadId, eventThreadId);
    }

    #endregion

    #region #1099 - Delta baseline must key off the acked tick so dropped deltas retransmit

    [Fact]
    public async Task Broadcast_DroppedDelta_IsRetransmittedOnceLossClears()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var serverWorld = new World();
        using var clientWorld = new World();

        var serverPlugin = new NetworkServerPlugin(server, new ServerNetworkConfig
        {
            TickRate = TickRate,
            Serializer = CreatePositionSerializer(),
        });
        serverWorld.InstallPlugin(serverPlugin);

        var clientPlugin = new NetworkClientPlugin(client, new ClientNetworkConfig
        {
            TickRate = TickRate,
            EnablePrediction = false,
            Serializer = CreatePositionSerializer(),
        });
        clientWorld.InstallPlugin(clientPlugin);
        clientWorld.Components.Register<NetPosition>(); // receiving world must know the type

        await server.ListenAsync(7777);
        await clientPlugin.ConnectAsync();
        server.Update();
        client.Update();

        // Server-owned entity starts at X = 0 and syncs to the client.
        var serverEntity = serverWorld.Spawn().With(new NetPosition { X = 0f, Y = 0f }).Build();
        var netId = serverPlugin.RegisterNetworkedEntity(serverEntity);

        serverWorld.Update(TickDelta); // full-sync spawn
        client.Update();
        server.Update(); // absorb the client's ack of the spawn tick

        Assert.True(clientPlugin.NetworkIds.TryGetLocalEntity(netId.Value, out var clientEntity));
        Assert.Equal(0f, clientWorld.Get<NetPosition>(clientEntity).X, 0.001f);

        // Move to X = 50 during a tick where the delta is 100% lost.
        server.SimulatedPacketLossPercent = 100f;
        serverWorld.Set(serverEntity, new NetPosition { X = 50f, Y = 0f });
        serverWorld.Update(TickDelta);
        client.Update();

        // The client never received the change yet.
        Assert.Equal(0f, clientWorld.Get<NetPosition>(clientEntity).X, 0.001f);

        // Loss clears. The changed field must still be re-sent (baseline never advanced
        // past the unacked tick), converging the client to X = 50.
        server.SimulatedPacketLossPercent = 0f;
        for (int i = 0; i < 5; i++)
        {
            serverWorld.Update(TickDelta);
            client.Update();
            server.Update();
        }

        Assert.Equal(50f, clientWorld.Get<NetPosition>(clientEntity).X, 0.001f);

        clientWorld.UninstallPlugin("NetworkClient");
        serverWorld.UninstallPlugin("NetworkServer");
        client.Dispose();
        server.Dispose();
    }

    #endregion

    #region #1100 - Every predicted entity's input must reach the server

    [Fact]
    public async Task ClientSend_TwoPredictedEntitiesSameTick_BothInputsReachServer()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var serverWorld = new World();
        using var clientWorld = new World();

        var serverPlugin = new NetworkServerPlugin(server, new ServerNetworkConfig { TickRate = TickRate });
        serverWorld.InstallPlugin(serverPlugin);

        var clientPlugin = new NetworkClientPlugin(client, new ClientNetworkConfig
        {
            TickRate = TickRate,
            EnablePrediction = true,
            InputSerializer = new NetInputSerializer(),
        });
        clientWorld.InstallPlugin(clientPlugin);

        await server.ListenAsync(7777);
        await clientPlugin.ConnectAsync();
        server.Update();
        client.Update();

        var inputNetIds = new HashSet<uint>();
        server.DataReceived += (_, data) =>
        {
            var reader = new NetworkMessageReader(data);
            reader.ReadHeader(out var type, out uint _);
            if (type == MessageType.ClientInput)
            {
                inputNetIds.Add(reader.ReadNetworkId());
            }
        };

        var localId = clientPlugin.LocalClientId;
        var entity1 = clientPlugin.SpawnNetworkedEntity(networkId: 1, ownerId: localId);
        var entity2 = clientPlugin.SpawnNetworkedEntity(networkId: 2, ownerId: localId);

        // Both locally-owned predicted entities record input for the same tick.
        clientPlugin.UpdateTick(5);
        clientPlugin.RecordInput(entity1, new NetInput { DeltaX = 1f });
        clientPlugin.RecordInput(entity2, new NetInput { DeltaX = 2f });

        clientWorld.Update(0.1f); // NetworkClientSendSystem flushes input for both entities.
        server.Update();

        Assert.Contains(1u, inputNetIds);
        Assert.Contains(2u, inputNetIds);

        clientWorld.UninstallPlugin("NetworkClient");
        serverWorld.UninstallPlugin("NetworkServer");
        client.Dispose();
        server.Dispose();
    }

    #endregion

    #region #1101 - EntitySpawn after a snapshot must not create a duplicate ghost

    [Fact]
    public async Task ClientReceive_SnapshotThenEntitySpawnSameNetworkId_SpawnsOnlyOnce()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var serverWorld = new World();
        using var clientWorld = new World();

        var serverPlugin = new NetworkServerPlugin(server, new ServerNetworkConfig { TickRate = TickRate });
        serverWorld.InstallPlugin(serverPlugin);

        var serializer = CreatePositionSerializer();
        var clientPlugin = new NetworkClientPlugin(client, new ClientNetworkConfig
        {
            TickRate = TickRate,
            EnablePrediction = false,
            Serializer = serializer,
        });
        clientWorld.InstallPlugin(clientPlugin);
        clientWorld.Components.Register<NetPosition>(); // receiving world must know the type

        await server.ListenAsync(7777);
        await clientPlugin.ConnectAsync();
        server.Update();
        client.Update();

        // A fresh client can receive both a FullSnapshot and a NeedsFullSync EntitySpawn
        // for the same network ID. Craft both for network ID 1.
        var snapshot = new byte[256];
        var snapshotWriter = new NetworkMessageWriter(snapshot);
        snapshotWriter.WriteHeader(MessageType.FullSnapshot, 1);
        snapshotWriter.WriteEntityCount(1);
        snapshotWriter.WriteEntitySpawn(networkId: 1, ownerId: 99);
        snapshotWriter.WriteComponentCount(1);
        snapshotWriter.WriteComponent(serializer, typeof(NetPosition), new NetPosition { X = 1f, Y = 0f });
        server.SendToAll(snapshotWriter.GetWrittenSpan(), DeliveryMode.ReliableOrdered);

        var spawn = new byte[256];
        var spawnWriter = new NetworkMessageWriter(spawn);
        spawnWriter.WriteHeader(MessageType.EntitySpawn, 1);
        spawnWriter.WriteEntitySpawn(networkId: 1, ownerId: 99);
        spawnWriter.WriteComponentCount(1);
        spawnWriter.WriteComponent(serializer, typeof(NetPosition), new NetPosition { X = 1f, Y = 0f });
        server.SendToAll(spawnWriter.GetWrittenSpan(), DeliveryMode.ReliableOrdered);

        server.Update();
        client.Update();

        var matching = 0;
        foreach (var entity in clientWorld.Query<NetworkId>())
        {
            if (clientWorld.Get<NetworkId>(entity).Value == 1u)
            {
                matching++;
            }
        }

        Assert.Equal(1, matching);

        clientWorld.UninstallPlugin("NetworkClient");
        serverWorld.UninstallPlugin("NetworkServer");
        client.Dispose();
        server.Dispose();
    }

    #endregion

    #region #1102 - Server must populate ClientState.RoundTripTimeMs from the transport

    [Fact]
    public async Task Server_PopulatesClientRoundTripTime_FromTransport()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var serverWorld = new World();

        // LocalTransport reports RTT as SimulatedLatencyMs * 2.
        server.SimulatedLatencyMs = 50f;

        var serverPlugin = new NetworkServerPlugin(server, new ServerNetworkConfig { TickRate = TickRate });
        serverWorld.InstallPlugin(serverPlugin);

        await server.ListenAsync(7777);
        await client.ConnectAsync("localhost", 7777);
        server.Update();

        var clientId = serverPlugin.GetConnectedClients().First().ClientId;
        Assert.Equal(0f, serverPlugin.GetClientRoundTripTimeMs(clientId), 0.001f);

        serverWorld.Update(TickDelta); // one network tick -> RTT is refreshed

        Assert.Equal(100f, serverPlugin.GetClientRoundTripTimeMs(clientId), 0.001f);

        serverWorld.UninstallPlugin("NetworkServer");
        client.Dispose();
        server.Dispose();
    }

    #endregion

    #region #1103 - Reconciliation must respect the owner-authoritative filter

    [Fact]
    public async Task Reconcile_OwnerAuthoritativeComponent_IsNotClobberedByServerState()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var clientWorld = new World();

        await server.ListenAsync(7777);

        var serializer = new MockNetworkSerializer();
        RegisterNetPosition(serializer);
        RegisterOwnerState(serializer);

        var clientPlugin = new NetworkClientPlugin(client, new ClientNetworkConfig
        {
            ServerAddress = "localhost",
            ServerPort = 7777,
            TickRate = TickRate,
            EnablePrediction = true,
            Serializer = serializer,
        });
        clientWorld.InstallPlugin(clientPlugin);

        await clientPlugin.ConnectAsync();
        client.Update();
        clientPlugin.UpdateTick(5);

        // Locally owned, predicted entity: OwnerState is client-authoritative (100),
        // NetPosition is server-authoritative and mispredicted (client 1, server 50).
        var entity = clientPlugin.SpawnNetworkedEntity(networkId: 1, ownerId: clientPlugin.LocalClientId);
        clientWorld.Add(entity, new OwnerState { Value = 100f });
        clientWorld.Add(entity, new NetPosition { X = 1f, Y = 0f });

        clientWorld.Update(0.016f); // save the tick-5 prediction

        // Server sends authoritative state for tick 5, including a stale OwnerState (999).
        var buffer = new byte[128];
        var writer = new NetworkMessageWriter(buffer);
        writer.WriteHeader(MessageType.ComponentUpdate, 5);
        writer.WriteNetworkId(1);
        writer.WriteComponentCount(2);
        writer.WriteComponent(serializer, typeof(OwnerState), new OwnerState { Value = 999f });
        writer.WriteComponent(serializer, typeof(NetPosition), new NetPosition { X = 50f, Y = 0f });
        server.SendToAll(writer.GetWrittenSpan(), DeliveryMode.ReliableOrdered);
        server.Update();
        client.Update(); // reconciliation

        // Server-authoritative NetPosition reconciled to 50; owner-authoritative OwnerState
        // stayed under local authority at 100 instead of being clobbered to 999.
        Assert.Equal(50f, clientWorld.Get<NetPosition>(entity).X, 0.001f);
        Assert.Equal(100f, clientWorld.Get<OwnerState>(entity).Value, 0.001f);

        clientWorld.UninstallPlugin("NetworkClient");
        client.Dispose();
        server.Dispose();
    }

    #endregion

    #region #1104 - Despawn must release per-entity client buffers

    [Fact]
    public async Task DespawnNetworkedEntity_ReleasesPredictionBuffer()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var clientWorld = new World();

        await server.ListenAsync(7777);

        var clientPlugin = new NetworkClientPlugin(client, new ClientNetworkConfig
        {
            ServerAddress = "localhost",
            ServerPort = 7777,
            TickRate = TickRate,
            EnablePrediction = true,
            Serializer = CreatePositionSerializer(),
        });
        clientWorld.InstallPlugin(clientPlugin);

        await clientPlugin.ConnectAsync();
        client.Update();

        var entity = clientPlugin.SpawnNetworkedEntity(networkId: 1, ownerId: clientPlugin.LocalClientId);
        clientWorld.Add(entity, new NetPosition { X = 0f, Y = 0f });

        clientWorld.Update(0.016f); // prediction system creates a buffer for the entity

        Assert.NotNull(clientPlugin.PredictionSystem);
        Assert.NotNull(clientPlugin.PredictionSystem!.GetPredictionBuffer(entity));

        clientPlugin.DespawnNetworkedEntity(1);

        // The per-entity prediction buffer must be released, not leaked for the client's life.
        Assert.Null(clientPlugin.PredictionSystem.GetPredictionBuffer(entity));

        clientWorld.UninstallPlugin("NetworkClient");
        client.Dispose();
        server.Dispose();
    }

    [Fact]
    public async Task EntityDestroyed_ReleasesServerSendSystemBaseline()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var serverWorld = new World();
        using var clientWorld = new World();

        var serverPlugin = new NetworkServerPlugin(server, new ServerNetworkConfig
        {
            TickRate = TickRate,
            Serializer = CreatePositionSerializer(),
        });
        serverWorld.InstallPlugin(serverPlugin);

        var clientPlugin = new NetworkClientPlugin(client, new ClientNetworkConfig
        {
            TickRate = TickRate,
            EnablePrediction = false,
            Serializer = CreatePositionSerializer(),
        });
        clientWorld.InstallPlugin(clientPlugin);
        clientWorld.Components.Register<NetPosition>(); // receiving world must know the type

        await server.ListenAsync(7777);
        await clientPlugin.ConnectAsync();
        server.Update();
        client.Update();

        var serverEntity = serverWorld.Spawn().With(new NetPosition { X = 1f, Y = 0f }).Build();
        serverPlugin.RegisterNetworkedEntity(serverEntity);

        // Tick so the send system records a pending baseline, then let the client ack it so
        // the confirmed baseline is populated too.
        serverWorld.Update(TickDelta);
        client.Update();
        server.Update();
        serverWorld.Update(TickDelta);

        Assert.True(SendSystemTracksEntity(serverPlugin, serverEntity));

        serverWorld.Despawn(serverEntity);

        // OnEntityDestroyed must wire through to ClearEntityState so the baselines shrink.
        Assert.False(SendSystemTracksEntity(serverPlugin, serverEntity));

        clientWorld.UninstallPlugin("NetworkClient");
        serverWorld.UninstallPlugin("NetworkServer");
        client.Dispose();
        server.Dispose();
    }

    private static bool SendSystemTracksEntity(NetworkServerPlugin plugin, Entity entity)
    {
        var sendSystemField = typeof(NetworkServerPlugin).GetField(
            "sendSystem", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(sendSystemField);
        var sendSystem = sendSystemField!.GetValue(plugin);
        Assert.NotNull(sendSystem);

        foreach (var fieldName in new[] { "lastSentState", "pendingState" })
        {
            var field = sendSystem!.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field?.GetValue(sendSystem) is System.Collections.IDictionary dict && dict.Contains(entity))
            {
                return true;
            }
        }

        return false;
    }

    #endregion

    #region #1105 - UDP ConnectAsync must resolve hostnames instead of throwing

    [Fact]
    public async Task Udp_ConnectAsync_Hostname_DoesNotThrowFormatException()
    {
        using var server = new UdpTransport();
        await server.ListenAsync(0);
        var port = server.LocalPort;

        using var client = new UdpTransport();
        using var cts = new CancellationTokenSource(2000);

        Exception? caught = null;
        try
        {
            await client.ConnectAsync("localhost", port, cts.Token);
        }
        catch (Exception e)
        {
            caught = e;
        }

        // Before the fix, IPAddress.Parse("localhost") threw FormatException before the DNS
        // branch could run, so UDP clients could never connect by hostname. After the fix the
        // hostname resolves via DNS; the connection itself may still fail depending on which
        // address family localhost maps to in this environment (e.g. IPv6-only), but a
        // FormatException must never occur.
        Assert.False(
            caught is FormatException,
            $"ConnectAsync must not throw FormatException for a hostname (#1105); got: {caught}");

        if (caught is null)
        {
            Assert.Equal(ConnectionState.Connected, client.State);
        }
    }

    #endregion
}
