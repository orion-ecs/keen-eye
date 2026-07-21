using KeenEyes.Capabilities;
using KeenEyes.Network.Replication;
using KeenEyes.Network.Serialization;
using KeenEyes.Network.Transport;
using KeenEyes.Testing.Network;

namespace KeenEyes.Network.Tests;

// LocalTransport completes synchronously, so Wait() is safe
#pragma warning disable xUnit1031 // Do not use blocking task operations
#pragma warning disable xUnit1051 // Use TestContext.Current.CancellationToken

/// <summary>
/// Networked test component recording a one-dimensional position.
/// </summary>
public struct HistoryPosition : IComponent
{
    /// <summary>The position along the X axis.</summary>
    public float X;
}

/// <summary>
/// Tests for <see cref="ServerStateHistory"/> and its integration with the server
/// send system: tick-indexed capture, ring eviction, despawn cleanup, at-or-before
/// lookup, and end-to-end recording across network ticks.
/// </summary>
public sealed class ServerStateHistoryTests
{
    private static MockNetworkSerializer CreateSerializer()
    {
        var serializer = new MockNetworkSerializer();
        serializer.RegisterComponent<HistoryPosition>(
            serialize: (ref BitWriter w, HistoryPosition c) => w.WriteFloat(c.X),
            deserialize: (ref BitReader r) => new HistoryPosition { X = r.ReadFloat() });
        return serializer;
    }

    private static float PositionAt(IReadOnlyDictionary<Type, object> state) =>
        ((HistoryPosition)state[typeof(HistoryPosition)]).X;

    #region Ring unit tests

    [Fact]
    public void Constructor_WithZeroCapacity_ThrowsArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ServerStateHistory(0));
    }

    [Fact]
    public void Constructor_WithNegativeCapacity_ThrowsArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ServerStateHistory(-1));
    }

    [Fact]
    public void TryGetState_UnknownEntity_ReturnsFalseWithEmptyState()
    {
        var history = new ServerStateHistory(8);
        using var world = new World();
        var entity = world.Spawn().With(new HistoryPosition { X = 1f }).Build();

        var found = history.TryGetState(entity, 1, out var state);

        Assert.False(found);
        Assert.Empty(state);
    }

    [Fact]
    public void Capture_ThenTryGetState_ReturnsRecordedValue()
    {
        var history = new ServerStateHistory(8);
        var serializer = CreateSerializer();
        using var world = new World();
        var entity = world.Spawn().With(new HistoryPosition { X = 12.5f }).Build();

        history.Capture(entity, 1, world, serializer);

        Assert.True(history.TryGetState(entity, 1, out var state));
        Assert.Equal(12.5f, PositionAt(state), 0.001f);
    }

    [Fact]
    public void Capture_OnlyRecordsNetworkSerializableComponents()
    {
        var history = new ServerStateHistory(8);
        var serializer = CreateSerializer();
        using var world = new World();
        // HistoryPosition is registered; NetworkState (added by nothing here) is not present.
        var entity = world.Spawn().With(new HistoryPosition { X = 3f }).Build();

        history.Capture(entity, 1, world, serializer);

        Assert.True(history.TryGetState(entity, 1, out var state));
        Assert.Single(state);
        Assert.True(state.ContainsKey(typeof(HistoryPosition)));
    }

    [Fact]
    public void Capture_MultipleTicks_EachTickRetainsItsOwnValue()
    {
        var history = new ServerStateHistory(8);
        var serializer = CreateSerializer();
        using var world = new World();
        var entity = world.Spawn().With(new HistoryPosition { X = 0f }).Build();

        for (uint tick = 1; tick <= 3; tick++)
        {
            ref var pos = ref world.Get<HistoryPosition>(entity);
            pos.X = tick * 10f;
            history.Capture(entity, tick, world, serializer);
        }

        Assert.True(history.TryGetState(entity, 1, out var s1));
        Assert.True(history.TryGetState(entity, 2, out var s2));
        Assert.True(history.TryGetState(entity, 3, out var s3));
        Assert.Equal(10f, PositionAt(s1), 0.001f);
        Assert.Equal(20f, PositionAt(s2), 0.001f);
        Assert.Equal(30f, PositionAt(s3), 0.001f);
    }

    [Fact]
    public void Capture_BeyondCapacity_EvictsOldestTick()
    {
        var history = new ServerStateHistory(4);
        var serializer = CreateSerializer();
        using var world = new World();
        var entity = world.Spawn().With(new HistoryPosition { X = 0f }).Build();

        // Capture ticks 1..5 into a capacity-4 ring; tick 5 reuses tick 1's slot.
        for (uint tick = 1; tick <= 5; tick++)
        {
            ref var pos = ref world.Get<HistoryPosition>(entity);
            pos.X = tick * 100f;
            history.Capture(entity, tick, world, serializer);
        }

        Assert.False(history.TryGetState(entity, 1, out _));
        Assert.True(history.TryGetState(entity, 2, out var s2));
        Assert.True(history.TryGetState(entity, 5, out var s5));
        Assert.Equal(200f, PositionAt(s2), 0.001f);
        Assert.Equal(500f, PositionAt(s5), 0.001f);
    }

    [Fact]
    public void Remove_DroppedEntity_ClearsHistory()
    {
        var history = new ServerStateHistory(8);
        var serializer = CreateSerializer();
        using var world = new World();
        var entity = world.Spawn().With(new HistoryPosition { X = 7f }).Build();
        history.Capture(entity, 1, world, serializer);
        Assert.True(history.TryGetState(entity, 1, out _));

        var removed = history.Remove(entity);

        Assert.True(removed);
        Assert.False(history.TryGetState(entity, 1, out _));
    }

    [Fact]
    public void Remove_UnknownEntity_ReturnsFalse()
    {
        var history = new ServerStateHistory(8);
        using var world = new World();
        var entity = world.Spawn().Build();

        Assert.False(history.Remove(entity));
    }

    #endregion

    #region TryGetStateAtOrBefore

    [Fact]
    public void TryGetStateAtOrBefore_ExactTick_ReturnsThatTick()
    {
        var history = new ServerStateHistory(64);
        var serializer = CreateSerializer();
        using var world = new World();
        var entity = world.Spawn().With(new HistoryPosition { X = 5f }).Build();
        history.Capture(entity, 20, world, serializer);

        Assert.True(history.TryGetStateAtOrBefore(entity, 20, out var matched, out var state));
        Assert.Equal(20u, matched);
        Assert.Equal(5f, PositionAt(state), 0.001f);
    }

    [Fact]
    public void TryGetStateAtOrBefore_BetweenTicks_ReturnsEarlierTick()
    {
        var history = new ServerStateHistory(64);
        var serializer = CreateSerializer();
        using var world = new World();
        var entity = world.Spawn().With(new HistoryPosition { X = 0f }).Build();

        ref var pos = ref world.Get<HistoryPosition>(entity);
        pos.X = 10f;
        history.Capture(entity, 10, world, serializer);
        pos = ref world.Get<HistoryPosition>(entity);
        pos.X = 20f;
        history.Capture(entity, 20, world, serializer);

        // Tick 15 falls between recorded ticks 10 and 20; nearest at-or-before is 10.
        Assert.True(history.TryGetStateAtOrBefore(entity, 15, out var matched, out var state));
        Assert.Equal(10u, matched);
        Assert.Equal(10f, PositionAt(state), 0.001f);
    }

    [Fact]
    public void TryGetStateAtOrBefore_AfterNewestTick_ReturnsNewest()
    {
        var history = new ServerStateHistory(64);
        var serializer = CreateSerializer();
        using var world = new World();
        var entity = world.Spawn().With(new HistoryPosition { X = 42f }).Build();
        history.Capture(entity, 20, world, serializer);

        // Query a tick past the newest recorded tick.
        Assert.True(history.TryGetStateAtOrBefore(entity, 30, out var matched, out var state));
        Assert.Equal(20u, matched);
        Assert.Equal(42f, PositionAt(state), 0.001f);
    }

    [Fact]
    public void TryGetStateAtOrBefore_BeforeAllTicks_ReturnsFalse()
    {
        var history = new ServerStateHistory(64);
        var serializer = CreateSerializer();
        using var world = new World();
        var entity = world.Spawn().With(new HistoryPosition { X = 1f }).Build();
        history.Capture(entity, 20, world, serializer);

        Assert.False(history.TryGetStateAtOrBefore(entity, 5, out var matched, out var state));
        Assert.Equal(0u, matched);
        Assert.Empty(state);
    }

    [Fact]
    public void TryGetStateAtOrBefore_UnknownEntity_ReturnsFalse()
    {
        var history = new ServerStateHistory(64);
        using var world = new World();
        var entity = world.Spawn().Build();

        Assert.False(history.TryGetStateAtOrBefore(entity, 10, out var matched, out var state));
        Assert.Equal(0u, matched);
        Assert.Empty(state);
    }

    #endregion

    #region Plugin / config integration

    [Fact]
    public void StateHistory_DefaultConfig_IsDisabled()
    {
        var (server, _) = LocalTransport.CreatePair();
        using var world = new World();
        var plugin = new NetworkServerPlugin(server, new ServerNetworkConfig { Serializer = CreateSerializer() });
        world.InstallPlugin(plugin);

        Assert.Null(plugin.StateHistory);

        world.UninstallPlugin("NetworkServer");
        server.Dispose();
    }

    [Fact]
    public void StateHistory_PositiveConfig_IsEnabledWithCapacity()
    {
        var (server, _) = LocalTransport.CreatePair();
        using var world = new World();
        var plugin = new NetworkServerPlugin(server, new ServerNetworkConfig
        {
            Serializer = CreateSerializer(),
            StateHistoryTicks = 120,
        });
        world.InstallPlugin(plugin);

        Assert.NotNull(plugin.StateHistory);
        Assert.Equal(120, plugin.StateHistory.Capacity);

        world.UninstallPlugin("NetworkServer");
        server.Dispose();
    }

    [Fact]
    public async Task ServerTick_HistoryDisabled_CapturesNothing()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var world = new World();
        var plugin = new NetworkServerPlugin(server, new ServerNetworkConfig
        {
            TickRate = 50,
            Serializer = CreateSerializer(),
        });
        world.InstallPlugin(plugin);

        await server.ListenAsync(7777);
        await client.ConnectAsync("localhost", 7777);
        server.Update();
        client.Update();

        var entity = world.Spawn().With(new HistoryPosition { X = 1f }).Build();
        plugin.RegisterNetworkedEntity(entity);

        world.Update(0.02f); // fire one network tick

        Assert.Null(plugin.StateHistory);

        world.UninstallPlugin("NetworkServer");
        client.Dispose();
        server.Dispose();
    }

    [Fact]
    public async Task ServerTicks_HistoryEnabled_RecordsHistoricalValuesAcrossTicks()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var world = new World();
        var plugin = new NetworkServerPlugin(server, new ServerNetworkConfig
        {
            TickRate = 50, // 0.02s per tick, so one tick per Update(0.02f)
            Serializer = CreateSerializer(),
            StateHistoryTicks = 120,
        });
        world.InstallPlugin(plugin);

        await server.ListenAsync(7777);
        await client.ConnectAsync("localhost", 7777);
        server.Update();
        client.Update();

        var entity = world.Spawn().With(new HistoryPosition { X = 0f }).Build();
        plugin.RegisterNetworkedEntity(entity);

        // Move the entity one unit of 10 per tick and advance three network ticks.
        for (int i = 1; i <= 3; i++)
        {
            ref var pos = ref world.Get<HistoryPosition>(entity);
            pos.X = i * 10f;
            world.Update(0.02f);
        }

        var history = plugin.StateHistory;
        Assert.NotNull(history);

        // currentTick advanced 1..3; each tick recorded the entity's position at that time.
        Assert.True(history.TryGetState(entity, 1, out var s1));
        Assert.True(history.TryGetState(entity, 2, out var s2));
        Assert.True(history.TryGetState(entity, 3, out var s3));

        var x1 = PositionAt(s1);
        var x2 = PositionAt(s2);
        var x3 = PositionAt(s3);

        Assert.Equal(10f, x1, 0.001f);
        Assert.Equal(20f, x2, 0.001f);
        Assert.Equal(30f, x3, 0.001f);

        // The whole point of the history: values differ tick-to-tick as the entity moves.
        Assert.NotEqual(x1, x2);
        Assert.NotEqual(x2, x3);

        world.UninstallPlugin("NetworkServer");
        client.Dispose();
        server.Dispose();
    }

    [Fact]
    public async Task Despawn_HistoryEnabled_DropsEntityHistory()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var world = new World();
        var plugin = new NetworkServerPlugin(server, new ServerNetworkConfig
        {
            TickRate = 50,
            Serializer = CreateSerializer(),
            StateHistoryTicks = 120,
        });
        world.InstallPlugin(plugin);

        await server.ListenAsync(7777);
        await client.ConnectAsync("localhost", 7777);
        server.Update();
        client.Update();

        var entity = world.Spawn().With(new HistoryPosition { X = 5f }).Build();
        plugin.RegisterNetworkedEntity(entity);

        world.Update(0.02f); // capture at tick 1

        var history = plugin.StateHistory;
        Assert.NotNull(history);
        Assert.True(history.TryGetState(entity, 1, out _));

        world.Despawn(entity);

        Assert.False(history.TryGetState(entity, 1, out _));

        world.UninstallPlugin("NetworkServer");
        client.Dispose();
        server.Dispose();
    }

    #endregion
}
