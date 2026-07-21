using KeenEyes.Network.Components;
using KeenEyes.Network.Prediction;
using KeenEyes.Network.Protocol;
using KeenEyes.Network.Serialization;
using KeenEyes.Network.Transport;
using KeenEyes.Testing.Network;

namespace KeenEyes.Network.Tests;

// LocalTransport completes synchronously, so Wait() is safe
#pragma warning disable xUnit1031 // Do not use blocking task operations
#pragma warning disable xUnit1051 // Use TestContext.Current.CancellationToken

/// <summary>
/// Replicated position component used to exercise prediction reconciliation and interpolation.
/// </summary>
public struct NetPosition : IComponent, INetworkSerializable, INetworkDeltaSerializable<NetPosition>
{
    /// <summary>The X coordinate.</summary>
    public float X;

    /// <summary>The Y coordinate.</summary>
    public float Y;

    /// <inheritdoc/>
    public readonly void NetworkSerialize(ref BitWriter writer)
    {
        writer.WriteFloat(X);
        writer.WriteFloat(Y);
    }

    /// <inheritdoc/>
    public void NetworkDeserialize(ref BitReader reader)
    {
        X = reader.ReadFloat();
        Y = reader.ReadFloat();
    }

    /// <inheritdoc/>
    public readonly uint GetDirtyMask(in NetPosition baseline)
    {
        uint mask = 0;
        if (MathF.Abs(X - baseline.X) > 0.0001f)
        {
            mask |= 1;
        }

        if (MathF.Abs(Y - baseline.Y) > 0.0001f)
        {
            mask |= 2;
        }

        return mask;
    }

    /// <inheritdoc/>
    public readonly void NetworkSerializeDelta(ref BitWriter writer, in NetPosition baseline, uint dirtyMask)
    {
        if ((dirtyMask & 1) != 0)
        {
            writer.WriteFloat(X);
        }

        if ((dirtyMask & 2) != 0)
        {
            writer.WriteFloat(Y);
        }
    }

    /// <inheritdoc/>
    public readonly void NetworkDeserializeDelta(ref BitReader reader, ref NetPosition baseline, uint dirtyMask)
    {
        if ((dirtyMask & 1) != 0)
        {
            baseline.X = reader.ReadFloat();
        }

        if ((dirtyMask & 2) != 0)
        {
            baseline.Y = reader.ReadFloat();
        }
    }
}

/// <summary>
/// Player input used to drive prediction replay during reconciliation.
/// </summary>
public struct NetInput : INetworkInput
{
    /// <inheritdoc/>
    public uint Tick { get; set; }

    /// <summary>The delta applied to <see cref="NetPosition.X"/> when the input is replayed.</summary>
    public float DeltaX;
}

/// <summary>
/// Test interpolator that linearly blends <see cref="NetPosition"/> values.
/// </summary>
public sealed class NetPositionInterpolator : INetworkInterpolator
{
    /// <inheritdoc/>
    public bool IsInterpolatable(Type type) => type == typeof(NetPosition);

    /// <inheritdoc/>
    public object? Interpolate(Type type, object from, object to, float factor)
    {
        if (type != typeof(NetPosition))
        {
            return null;
        }

        var a = (NetPosition)from;
        var b = (NetPosition)to;
        return new NetPosition
        {
            X = a.X + ((b.X - a.X) * factor),
            Y = a.Y + ((b.Y - a.Y) * factor),
        };
    }
}

/// <summary>
/// Tests that the client receive path routes predicted entities through reconciliation
/// and that the interpolation system is wired up for remote entities.
/// </summary>
public sealed class PredictionReconciliationTests
{
    private static MockNetworkSerializer CreateSerializer()
    {
        var serializer = new MockNetworkSerializer();
        serializer.RegisterComponentWithDelta<NetPosition>(
            serialize: (ref BitWriter w, NetPosition p) =>
            {
                w.WriteFloat(p.X);
                w.WriteFloat(p.Y);
            },
            deserialize: (ref BitReader r) => new NetPosition
            {
                X = r.ReadFloat(),
                Y = r.ReadFloat(),
            },
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

        return serializer;
    }

    #region Reconciliation Tests

    [Fact]
    public async Task OnServerStateReceived_PredictedEntityWithMismatch_ReconcilesAndReplaysInput()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var clientWorld = new World();

        await server.ListenAsync(7777);

        var serializer = CreateSerializer();
        var config = new ClientNetworkConfig
        {
            ServerAddress = "localhost",
            ServerPort = 7777,
            EnablePrediction = true,
            Serializer = serializer,
            InputApplicator = (entity, input) =>
            {
                if (input is NetInput netInput)
                {
                    ref var pos = ref clientWorld.Get<NetPosition>(entity);
                    pos.X += netInput.DeltaX;
                }
            },
        };
        var clientPlugin = new NetworkClientPlugin(client, config);
        clientWorld.InstallPlugin(clientPlugin);

        await clientPlugin.ConnectAsync();

        // Client is at server tick 5 when it makes its prediction.
        clientPlugin.UpdateTick(5);

        // Spawn a locally owned, predicted entity and predict X = 10 for tick 5 (a misprediction).
        var predictedEntity = clientPlugin.SpawnNetworkedEntity(networkId: 1, ownerId: clientPlugin.LocalClientId);
        clientWorld.Add(predictedEntity, new NetPosition { X = 10f, Y = 0f });

        // Buffer the tick-6 input that must be replayed after rollback.
        clientPlugin.GetOrCreateInputBuffer<NetInput>(predictedEntity)
            .Add(new NetInput { Tick = 6, DeltaX = 5f });

        // Run one client frame so the prediction system snapshots the predicted state at tick 5.
        clientWorld.Update(0.016f);
        Assert.Equal(10f, clientWorld.Get<NetPosition>(predictedEntity).X, 0.001f);

        // Server sends authoritative state for tick 5: X = 3 (differs from predicted X = 10).
        SendComponentUpdate(server, serverTick: 5, networkId: 1, serializer, new NetPosition { X = 3f, Y = 0f });
        client.Update(); // receive -> reconciliation

        // Rolled back to authoritative X = 3, then replayed the tick-6 input (+5) -> X = 8.
        // This is neither the blind server value (3) nor the discarded prediction (10).
        Assert.Equal(8f, clientWorld.Get<NetPosition>(predictedEntity).X, 0.001f);

        ref readonly var predState = ref clientWorld.Get<PredictionState>(predictedEntity);
        Assert.True(predState.MispredictionDetected);
        Assert.Equal(5u, predState.LastConfirmedTick);

        clientWorld.UninstallPlugin("NetworkClient");
        client.Dispose();
        server.Dispose();
    }

    [Fact]
    public async Task OnServerStateReceived_PredictedEntityMatches_DoesNotReconcile()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var clientWorld = new World();

        await server.ListenAsync(7777);

        var serializer = CreateSerializer();
        var replayCount = 0;
        var config = new ClientNetworkConfig
        {
            ServerAddress = "localhost",
            ServerPort = 7777,
            EnablePrediction = true,
            Serializer = serializer,
            InputApplicator = (_, _) => replayCount++,
        };
        var clientPlugin = new NetworkClientPlugin(client, config);
        clientWorld.InstallPlugin(clientPlugin);

        await clientPlugin.ConnectAsync();
        clientPlugin.UpdateTick(5);

        var predictedEntity = clientPlugin.SpawnNetworkedEntity(networkId: 1, ownerId: clientPlugin.LocalClientId);
        clientWorld.Add(predictedEntity, new NetPosition { X = 10f, Y = 0f });
        clientPlugin.GetOrCreateInputBuffer<NetInput>(predictedEntity)
            .Add(new NetInput { Tick = 6, DeltaX = 5f });

        clientWorld.Update(0.016f);

        // Server confirms exactly what the client predicted (X = 10).
        SendComponentUpdate(server, serverTick: 5, networkId: 1, serializer, new NetPosition { X = 10f, Y = 0f });
        client.Update();

        // No reconciliation: state stays at the predicted value (no visual pop) and no input replayed.
        Assert.Equal(10f, clientWorld.Get<NetPosition>(predictedEntity).X, 0.001f);
        Assert.Equal(0, replayCount);

        ref readonly var predState = ref clientWorld.Get<PredictionState>(predictedEntity);
        Assert.False(predState.MispredictionDetected);
        Assert.Equal(5u, predState.LastConfirmedTick);

        clientWorld.UninstallPlugin("NetworkClient");
        client.Dispose();
        server.Dispose();
    }

    [Fact]
    public async Task OnDataReceived_PredictionDisabled_AppliesServerStateDirectly()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var clientWorld = new World();

        await server.ListenAsync(7777);

        var serializer = CreateSerializer();
        var config = new ClientNetworkConfig
        {
            ServerAddress = "localhost",
            ServerPort = 7777,
            EnablePrediction = false,
            Serializer = serializer,
        };
        var clientPlugin = new NetworkClientPlugin(client, config);
        clientWorld.InstallPlugin(clientPlugin);

        await clientPlugin.ConnectAsync();

        // With prediction disabled, a locally owned entity is not marked Predicted.
        var entity = clientPlugin.SpawnNetworkedEntity(networkId: 1, ownerId: clientPlugin.LocalClientId);
        Assert.True(clientWorld.Has<LocallyOwned>(entity));
        Assert.False(clientWorld.Has<Predicted>(entity));
        clientWorld.Add(entity, new NetPosition { X = 10f, Y = 0f });

        // Authoritative state is applied directly (blind overwrite), preserving old behavior.
        SendComponentUpdate(server, serverTick: 5, networkId: 1, serializer, new NetPosition { X = 3f, Y = 0f });
        client.Update();

        Assert.Equal(3f, clientWorld.Get<NetPosition>(entity).X, 0.001f);

        clientWorld.UninstallPlugin("NetworkClient");
        client.Dispose();
        server.Dispose();
    }

    #endregion

    #region Interpolation Tests

    [Fact]
    public void InterpolationSystem_RemoteEntityWithTwoSnapshots_ProducesIntermediateBlend()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var clientWorld = new World();

        var serializer = CreateSerializer();
        var config = new ClientNetworkConfig
        {
            EnablePrediction = false,
            Serializer = serializer,
            Interpolator = new NetPositionInterpolator(),
            InterpolationDelayMs = 50f,
        };
        var clientPlugin = new NetworkClientPlugin(client, config);
        clientWorld.InstallPlugin(clientPlugin);

        // Remote entity: RemotelyOwned + Interpolated + InterpolationState + snapshot buffer.
        var remoteEntity = clientPlugin.SpawnNetworkedEntity(networkId: 1, ownerId: 99);
        Assert.True(clientWorld.Has<Interpolated>(remoteEntity));
        clientWorld.Add(remoteEntity, new NetPosition { X = 0f, Y = 0f });

        // Two snapshots straddling the render window: from X = 0 to X = 10.
        var snapshotBuffer = clientPlugin.GetSnapshotBuffer(remoteEntity);
        Assert.NotNull(snapshotBuffer);
        snapshotBuffer!.PushSnapshot(typeof(NetPosition), new NetPosition { X = 0f, Y = 0f });
        snapshotBuffer.PushSnapshot(typeof(NetPosition), new NetPosition { X = 10f, Y = 0f });

        // Interpolation window [0, 0.1]s. A single 0.1s frame with a 50ms render delay
        // renders at t = 0.05, i.e. exactly halfway through the window (factor 0.5).
        clientWorld.Set(remoteEntity, new InterpolationState { FromTime = 0.0, ToTime = 0.1 });

        clientWorld.Update(0.1f);

        Assert.Equal(0.5f, clientWorld.Get<InterpolationState>(remoteEntity).Factor, 0.001f);

        // Blended to the midpoint, not snapped to either snapshot.
        Assert.Equal(5f, clientWorld.Get<NetPosition>(remoteEntity).X, 0.001f);

        clientWorld.UninstallPlugin("NetworkClient");
        client.Dispose();
        server.Dispose();
    }

    #endregion

    private static void SendComponentUpdate(
        LocalTransport server,
        uint serverTick,
        uint networkId,
        MockNetworkSerializer serializer,
        NetPosition state)
    {
        Span<byte> buffer = stackalloc byte[64];
        var writer = new NetworkMessageWriter(buffer);
        writer.WriteHeader(MessageType.ComponentUpdate, serverTick);
        writer.WriteNetworkId(networkId);
        writer.WriteComponentCount(1);
        writer.WriteComponent(serializer, typeof(NetPosition), state);
        server.SendToAll(writer.GetWrittenSpan(), DeliveryMode.ReliableOrdered);
        server.Update();
    }
}
