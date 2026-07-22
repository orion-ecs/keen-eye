using KeenEyes.Network.Components;
using KeenEyes.Network.Prediction;
using KeenEyes.Network.Protocol;
using KeenEyes.Network.Serialization;
using KeenEyes.Network.Systems;
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
/// Replicated velocity component used to exercise multi-component correction magnitude.
/// </summary>
public struct NetVelocity : IComponent, INetworkSerializable, INetworkDeltaSerializable<NetVelocity>
{
    /// <summary>The X velocity.</summary>
    public float VX;

    /// <inheritdoc/>
    public readonly void NetworkSerialize(ref BitWriter writer) => writer.WriteFloat(VX);

    /// <inheritdoc/>
    public void NetworkDeserialize(ref BitReader reader) => VX = reader.ReadFloat();

    /// <inheritdoc/>
    public readonly uint GetDirtyMask(in NetVelocity baseline) =>
        MathF.Abs(VX - baseline.VX) > 0.0001f ? 1u : 0u;

    /// <inheritdoc/>
    public readonly void NetworkSerializeDelta(ref BitWriter writer, in NetVelocity baseline, uint dirtyMask)
    {
        if ((dirtyMask & 1) != 0)
        {
            writer.WriteFloat(VX);
        }
    }

    /// <inheritdoc/>
    public readonly void NetworkDeserializeDelta(ref BitReader reader, ref NetVelocity baseline, uint dirtyMask)
    {
        if ((dirtyMask & 1) != 0)
        {
            baseline.VX = reader.ReadFloat();
        }
    }
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

    private static MockNetworkSerializer CreateSerializerWithVelocity()
    {
        var serializer = CreateSerializer();
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

    #region Correction Magnitude Tests

    [Fact]
    public async Task OnServerStateReceived_PredictionMatches_ResetsCorrectionMagnitudeToZero()
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
        };
        var clientPlugin = new NetworkClientPlugin(client, config);
        clientWorld.InstallPlugin(clientPlugin);

        await clientPlugin.ConnectAsync();
        clientPlugin.UpdateTick(5);

        var predictedEntity = clientPlugin.SpawnNetworkedEntity(networkId: 1, ownerId: clientPlugin.LocalClientId);
        clientWorld.Add(predictedEntity, new NetPosition { X = 10f, Y = 0f });

        clientWorld.Update(0.016f);

        // Simulate a stale magnitude left over from an earlier correction;
        // a matching confirmation must clear it.
        {
            ref var predState = ref clientWorld.Get<PredictionState>(predictedEntity);
            predState.LastCorrectionMagnitude = 0.75f;
        }

        // Server confirms exactly what the client predicted.
        SendComponentUpdate(server, serverTick: 5, networkId: 1, serializer, new NetPosition { X = 10f, Y = 0f });
        client.Update();

        ref readonly var result = ref clientWorld.Get<PredictionState>(predictedEntity);
        Assert.False(result.MispredictionDetected);
        Assert.Equal(0f, result.LastCorrectionMagnitude, 0.0001f);

        clientWorld.UninstallPlugin("NetworkClient");
        client.Dispose();
        server.Dispose();
    }

    [Fact]
    public async Task Reconcile_SingleComponentDiverged_SetsCorrectionMagnitudeToOne()
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
        };
        var clientPlugin = new NetworkClientPlugin(client, config);
        clientWorld.InstallPlugin(clientPlugin);

        await clientPlugin.ConnectAsync();
        clientPlugin.UpdateTick(5);

        var predictedEntity = clientPlugin.SpawnNetworkedEntity(networkId: 1, ownerId: clientPlugin.LocalClientId);
        clientWorld.Add(predictedEntity, new NetPosition { X = 10f, Y = 0f });

        clientWorld.Update(0.016f);

        // The only compared component diverges: 1 of 1 corrected -> magnitude 1.
        SendComponentUpdate(server, serverTick: 5, networkId: 1, serializer, new NetPosition { X = 3f, Y = 0f });
        client.Update();

        ref readonly var result = ref clientWorld.Get<PredictionState>(predictedEntity);
        Assert.True(result.MispredictionDetected);
        Assert.Equal(1f, result.LastCorrectionMagnitude, 0.0001f);

        clientWorld.UninstallPlugin("NetworkClient");
        client.Dispose();
        server.Dispose();
    }

    [Fact]
    public async Task Reconcile_MoreComponentsDiverged_IncreasesCorrectionMagnitude()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var clientWorld = new World();

        await server.ListenAsync(7777);

        var serializer = CreateSerializerWithVelocity();
        var config = new ClientNetworkConfig
        {
            ServerAddress = "localhost",
            ServerPort = 7777,
            EnablePrediction = true,
            Serializer = serializer,
        };
        var clientPlugin = new NetworkClientPlugin(client, config);
        clientWorld.InstallPlugin(clientPlugin);

        await clientPlugin.ConnectAsync();
        clientPlugin.UpdateTick(5);

        var predictedEntity = clientPlugin.SpawnNetworkedEntity(networkId: 1, ownerId: clientPlugin.LocalClientId);
        clientWorld.Add(predictedEntity, new NetPosition { X = 10f, Y = 0f });
        clientWorld.Add(predictedEntity, new NetVelocity { VX = 1f });

        // Save the tick-5 prediction for both components.
        clientWorld.Update(0.016f);

        // Tick 5: only the position diverges -> 1 of 2 compared components corrected.
        SendComponentUpdates(server, serverTick: 5, networkId: 1, serializer,
            (typeof(NetPosition), new NetPosition { X = 3f, Y = 0f }),
            (typeof(NetVelocity), new NetVelocity { VX = 1f }));
        client.Update();

        var partialMagnitude = clientWorld.Get<PredictionState>(predictedEntity).LastCorrectionMagnitude;
        Assert.Equal(0.5f, partialMagnitude, 0.0001f);

        // Save the tick-6 prediction (post-reconciliation values).
        clientPlugin.UpdateTick(6);
        clientWorld.Update(0.016f);

        // Tick 6: both components diverge -> 2 of 2 compared components corrected.
        SendComponentUpdates(server, serverTick: 6, networkId: 1, serializer,
            (typeof(NetPosition), new NetPosition { X = 50f, Y = 20f }),
            (typeof(NetVelocity), new NetVelocity { VX = 9f }));
        client.Update();

        var fullMagnitude = clientWorld.Get<PredictionState>(predictedEntity).LastCorrectionMagnitude;
        Assert.Equal(1f, fullMagnitude, 0.0001f);

        // The magnitude grows monotonically with the breadth of the divergence.
        Assert.True(fullMagnitude > partialMagnitude);

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

    [Fact]
    public void InterpolationSystem_FirstSnapshotPairAheadOfLocalClock_ProducesCorrectFactor()
    {
        using var world = new World();

        var buffers = new Dictionary<Entity, SnapshotBuffer>();
        var system = new InterpolationSystem(
            interpolationDelayMs: 50f,
            interpolator: new NetPositionInterpolator(),
            getSnapshotBuffer: entity => buffers.GetValueOrDefault(entity));
        world.AddSystem(system, SystemPhase.Update);

        // Joining a long-running server: the plugin's tick-derived snapshot timestamps
        // start around 100s while the system's render clock starts at zero.
        var entity = world.Spawn()
            .With(default(Interpolated))
            .With(new InterpolationState { FromTime = 100.0, ToTime = 100.1 })
            .With(new NetPosition { X = 0f, Y = 0f })
            .Build();

        var buffer = new SnapshotBuffer();
        buffer.PushSnapshot(typeof(NetPosition), new NetPosition { X = 0f, Y = 0f });
        buffer.PushSnapshot(typeof(NetPosition), new NetPosition { X = 10f, Y = 0f });
        buffers[entity] = buffer;

        // On the very first update the clock must adopt the snapshot time basis:
        // render time = 100.1 - 0.05 = 100.05 -> factor (100.05 - 100.0) / 0.1 = 0.5.
        // Without origin sync the factor would clamp to 0 until ~100s of frame time
        // had accumulated.
        world.Update(0.016f);

        Assert.Equal(0.5f, world.Get<InterpolationState>(entity).Factor, 0.001f);
        Assert.Equal(5f, world.Get<NetPosition>(entity).X, 0.001f);
    }

    [Fact]
    public void InterpolationSystem_AfterLongStall_ResynchronizesToSnapshotTimestamps()
    {
        using var world = new World();

        var buffers = new Dictionary<Entity, SnapshotBuffer>();
        var system = new InterpolationSystem(
            interpolationDelayMs: 50f,
            interpolator: new NetPositionInterpolator(),
            getSnapshotBuffer: entity => buffers.GetValueOrDefault(entity));
        world.AddSystem(system, SystemPhase.Update);

        var entity = world.Spawn()
            .With(default(Interpolated))
            .With(new InterpolationState { FromTime = 0.0, ToTime = 0.1 })
            .With(new NetPosition { X = 0f, Y = 0f })
            .Build();

        var buffer = new SnapshotBuffer();
        buffer.PushSnapshot(typeof(NetPosition), new NetPosition { X = 0f, Y = 0f });
        buffer.PushSnapshot(typeof(NetPosition), new NetPosition { X = 10f, Y = 0f });
        buffers[entity] = buffer;

        // Normal operation: the render clock and snapshot timestamps share the origin.
        world.Update(0.1f);
        Assert.Equal(0.5f, world.Get<InterpolationState>(entity).Factor, 0.001f);

        // Simulated stall/reconnect: the next snapshots arrive with timestamps ~50s
        // ahead of the local render clock.
        buffer.PushSnapshot(typeof(NetPosition), new NetPosition { X = 20f, Y = 0f });
        buffer.PushSnapshot(typeof(NetPosition), new NetPosition { X = 30f, Y = 0f });
        world.Set(entity, new InterpolationState { FromTime = 50.0, ToTime = 50.1 });

        // One frame later the clock snaps to the new basis instead of clamping the
        // factor to 0 for the next ~50 seconds of frame time.
        // Render time = 50.1 - 0.05 = 50.05 -> factor 0.5 -> X = 25 (blend of 20 and 30).
        world.Update(0.016f);

        Assert.Equal(0.5f, world.Get<InterpolationState>(entity).Factor, 0.001f);
        Assert.Equal(25f, world.Get<NetPosition>(entity).X, 0.001f);
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

    private static void SendComponentUpdates(
        LocalTransport server,
        uint serverTick,
        uint networkId,
        MockNetworkSerializer serializer,
        params (Type Type, object Value)[] components)
    {
        Span<byte> buffer = stackalloc byte[256];
        var writer = new NetworkMessageWriter(buffer);
        writer.WriteHeader(MessageType.ComponentUpdate, serverTick);
        writer.WriteNetworkId(networkId);
        writer.WriteComponentCount((byte)components.Length);
        foreach (var (type, value) in components)
        {
            writer.WriteComponent(serializer, type, value);
        }

        server.SendToAll(writer.GetWrittenSpan(), DeliveryMode.ReliableOrdered);
        server.Update();
    }
}
