using KeenEyes.Network.Replication;
using KeenEyes.Network.Serialization;
using KeenEyes.Network.Transport;
using KeenEyes.Testing.Network;

namespace KeenEyes.Network.Tests;

// LocalTransport completes synchronously, so awaiting its tasks never blocks a real socket.
#pragma warning disable xUnit1031 // Do not use blocking task operations
#pragma warning disable xUnit1051 // Use TestContext.Current.CancellationToken

/// <summary>
/// One-dimensional interpolatable position used to exercise lag-compensated rewinding.
/// </summary>
public struct LagPosition : IComponent
{
    /// <summary>The position along the X axis.</summary>
    public float X;
}

/// <summary>
/// A non-interpolatable networked value used to verify snap-to-at-or-before behavior.
/// </summary>
public struct LagTeam : IComponent
{
    /// <summary>The team identifier.</summary>
    public int Value;
}

/// <summary>
/// Test interpolator that linearly blends <see cref="LagPosition"/> and refuses everything else.
/// </summary>
public sealed class LagPositionInterpolator : INetworkInterpolator
{
    /// <inheritdoc/>
    public bool IsInterpolatable(Type type) => type == typeof(LagPosition);

    /// <inheritdoc/>
    public object? Interpolate(Type type, object from, object to, float factor)
    {
        if (type != typeof(LagPosition))
        {
            return null;
        }

        var a = (LagPosition)from;
        var b = (LagPosition)to;
        return new LagPosition { X = a.X + ((b.X - a.X) * factor) };
    }
}

/// <summary>
/// Tests for <see cref="LagCompensation"/>: perceived-tick estimation, interpolated historical
/// state, rewind/restore exactness, and end-to-end lag-compensated hit detection.
/// </summary>
public sealed class LagCompensationTests
{
    private static MockNetworkSerializer CreateSerializer()
    {
        var serializer = new MockNetworkSerializer();
        serializer.RegisterComponent<LagPosition>(
            serialize: (ref BitWriter w, LagPosition c) => w.WriteFloat(c.X),
            deserialize: (ref BitReader r) => new LagPosition { X = r.ReadFloat() });
        serializer.RegisterComponent<LagTeam>(
            serialize: (ref BitWriter w, LagTeam c) => w.WriteUInt32((uint)c.Value),
            deserialize: (ref BitReader r) => new LagTeam { Value = (int)r.ReadUInt32() });
        return serializer;
    }

    private static float XOf(IReadOnlyDictionary<Type, object> state) =>
        ((LagPosition)state[typeof(LagPosition)]).X;

    private static int TeamOf(IReadOnlyDictionary<Type, object> state) =>
        ((LagTeam)state[typeof(LagTeam)]).Value;

    /// <summary>
    /// Installs a server plugin (no client connection) and returns it with its transport.
    /// </summary>
    private static (NetworkServerPlugin Plugin, LocalTransport Server) InstallServer(World world, ServerNetworkConfig config)
    {
        var (server, _) = LocalTransport.CreatePair();
        var plugin = new NetworkServerPlugin(server, config);
        world.InstallPlugin(plugin);
        return (plugin, server);
    }

    /// <summary>
    /// Installs a server plugin and connects a single client, returning the client id.
    /// </summary>
    private static async Task<(NetworkServerPlugin Plugin, LocalTransport Server, LocalTransport Client, int ClientId)>
        InstallServerWithClientAsync(World world, ServerNetworkConfig config)
    {
        var (server, client) = LocalTransport.CreatePair();
        var plugin = new NetworkServerPlugin(server, config);
        world.InstallPlugin(plugin);

        await server.ListenAsync(7777);
        await client.ConnectAsync("localhost", 7777);
        server.Update();
        client.Update();

        var clientId = plugin.GetConnectedClients().First().ClientId;
        return (plugin, server, client, clientId);
    }

    private static void AdvanceTo(NetworkServerPlugin plugin, uint targetTick)
    {
        // Each Tick(1f) advances exactly one network tick regardless of tick rate.
        while (plugin.CurrentTick < targetTick)
        {
            plugin.Tick(1f);
        }
    }

    #region Availability

    [Fact]
    public void LagCompensation_HistoryDisabled_IsNull()
    {
        using var world = new World();
        var (plugin, server) = InstallServer(world, new ServerNetworkConfig { Serializer = CreateSerializer() });

        Assert.Null(plugin.StateHistory);
        Assert.Null(plugin.LagCompensation);

        world.UninstallPlugin("NetworkServer");
        server.Dispose();
    }

    [Fact]
    public void LagCompensation_HistoryEnabledAndInstalled_IsAvailable()
    {
        using var world = new World();
        var (plugin, server) = InstallServer(world, new ServerNetworkConfig
        {
            Serializer = CreateSerializer(),
            StateHistoryTicks = 64,
        });

        Assert.NotNull(plugin.StateHistory);
        Assert.NotNull(plugin.LagCompensation);

        world.UninstallPlugin("NetworkServer");
        server.Dispose();
    }

    [Fact]
    public void LagCompensation_AfterUninstall_IsNullAgain()
    {
        using var world = new World();
        var (plugin, server) = InstallServer(world, new ServerNetworkConfig
        {
            Serializer = CreateSerializer(),
            StateHistoryTicks = 64,
        });
        Assert.NotNull(plugin.LagCompensation);

        world.UninstallPlugin("NetworkServer");

        Assert.Null(plugin.LagCompensation);
        server.Dispose();
    }

    #endregion

    #region EstimateClientPerceivedTick

    [Fact]
    public async Task EstimateClientPerceivedTick_WithRttAndInterpolationDelay_SubtractsBothTerms()
    {
        using var world = new World();
        var (plugin, server, client, clientId) = await InstallServerWithClientAsync(world, new ServerNetworkConfig
        {
            TickRate = 50,           // 20 ms per tick
            InterpolationDelayMs = 100f, // 5 ticks
            Serializer = CreateSerializer(),
            StateHistoryTicks = 128,
        });

        // RTT 200 ms -> one-way 100 ms -> 5 ticks. Plus 5 ticks interpolation delay = 10 ticks back.
        plugin.GetConnectedClients().First().RoundTripTimeMs = 200f;
        AdvanceTo(plugin, 20);

        var perceived = plugin.LagCompensation!.EstimateClientPerceivedTick(clientId);

        Assert.Equal(10u, perceived);

        world.UninstallPlugin("NetworkServer");
        client.Dispose();
        server.Dispose();
    }

    [Fact]
    public async Task EstimateClientPerceivedTick_ZeroLatencyAndNoDelay_ReturnsCurrentTick()
    {
        using var world = new World();
        var (plugin, server, client, clientId) = await InstallServerWithClientAsync(world, new ServerNetworkConfig
        {
            TickRate = 50,
            InterpolationDelayMs = 0f,
            Serializer = CreateSerializer(),
            StateHistoryTicks = 128,
        });

        plugin.GetConnectedClients().First().RoundTripTimeMs = 0f;
        AdvanceTo(plugin, 20);

        var perceived = plugin.LagCompensation!.EstimateClientPerceivedTick(clientId);

        Assert.Equal(20u, perceived);

        world.UninstallPlugin("NetworkServer");
        client.Dispose();
        server.Dispose();
    }

    [Fact]
    public async Task EstimateClientPerceivedTick_ExcessiveLatency_ClampsToOldestRetainedTick()
    {
        using var world = new World();
        var (plugin, server, client, clientId) = await InstallServerWithClientAsync(world, new ServerNetworkConfig
        {
            TickRate = 50,
            InterpolationDelayMs = 0f,
            Serializer = CreateSerializer(),
            StateHistoryTicks = 8, // window keeps ticks (currentTick - 7 .. currentTick)
        });

        // Absurd RTT would rewind far past the retained window; must clamp to the oldest tick.
        plugin.GetConnectedClients().First().RoundTripTimeMs = 100_000f;
        AdvanceTo(plugin, 20);

        var perceived = plugin.LagCompensation!.EstimateClientPerceivedTick(clientId);

        // Oldest retained = currentTick + 1 - capacity = 20 + 1 - 8 = 13.
        Assert.Equal(13u, perceived);

        world.UninstallPlugin("NetworkServer");
        client.Dispose();
        server.Dispose();
    }

    [Fact]
    public void EstimateClientPerceivedTick_UnknownClient_UsesZeroRttGracefully()
    {
        using var world = new World();
        var (plugin, server) = InstallServer(world, new ServerNetworkConfig
        {
            TickRate = 50,
            InterpolationDelayMs = 100f, // 5 ticks, no RTT term for an unknown client
            Serializer = CreateSerializer(),
            StateHistoryTicks = 128,
        });
        AdvanceTo(plugin, 20);

        // Client 999 was never connected: RTT resolves to 0, only interpolation delay applies.
        var perceived = plugin.LagCompensation!.EstimateClientPerceivedTick(999);

        Assert.Equal(15u, perceived);

        world.UninstallPlugin("NetworkServer");
        server.Dispose();
    }

    #endregion

    #region TryGetStateAt

    [Fact]
    public void TryGetStateAt_ExactRecordedTick_ReturnsRecordedValue()
    {
        using var world = new World();
        var serializer = CreateSerializer();
        var (plugin, server) = InstallServer(world, new ServerNetworkConfig
        {
            Serializer = serializer,
            Interpolator = new LagPositionInterpolator(),
            StateHistoryTicks = 64,
        });
        var entity = world.Spawn().With(new LagPosition { X = 7f }).Build();
        plugin.StateHistory!.Capture(entity, 10, world, serializer);

        Assert.True(plugin.LagCompensation!.TryGetStateAt(entity, 10, out var state));
        Assert.Equal(7f, XOf(state), 0.001f);

        world.UninstallPlugin("NetworkServer");
        server.Dispose();
    }

    [Fact]
    public void TryGetStateAt_BetweenTicks_InterpolatableComponent_Blends()
    {
        using var world = new World();
        var serializer = CreateSerializer();
        var (plugin, server) = InstallServer(world, new ServerNetworkConfig
        {
            Serializer = serializer,
            Interpolator = new LagPositionInterpolator(),
            StateHistoryTicks = 64,
        });
        var entity = world.Spawn().With(new LagPosition { X = 0f }).Build();

        ref var pos = ref world.Get<LagPosition>(entity);
        pos.X = 0f;
        plugin.StateHistory!.Capture(entity, 10, world, serializer);
        pos = ref world.Get<LagPosition>(entity);
        pos.X = 100f;
        plugin.StateHistory.Capture(entity, 20, world, serializer);

        // Tick 15 is halfway between 10 and 20, so X blends to 50.
        Assert.True(plugin.LagCompensation!.TryGetStateAt(entity, 15, out var state));
        Assert.Equal(50f, XOf(state), 0.001f);

        world.UninstallPlugin("NetworkServer");
        server.Dispose();
    }

    [Fact]
    public void TryGetStateAt_BetweenTicks_NonInterpolatableComponent_SnapsToAtOrBefore()
    {
        using var world = new World();
        var serializer = CreateSerializer();
        var (plugin, server) = InstallServer(world, new ServerNetworkConfig
        {
            Serializer = serializer,
            Interpolator = new LagPositionInterpolator(),
            StateHistoryTicks = 64,
        });
        var entity = world.Spawn().With(new LagTeam { Value = 1 }).Build();

        ref var team = ref world.Get<LagTeam>(entity);
        team.Value = 1;
        plugin.StateHistory!.Capture(entity, 10, world, serializer);
        team = ref world.Get<LagTeam>(entity);
        team.Value = 2;
        plugin.StateHistory.Capture(entity, 20, world, serializer);

        // LagTeam is not interpolatable, so tick 15 snaps to the tick-10 value.
        Assert.True(plugin.LagCompensation!.TryGetStateAt(entity, 15, out var state));
        Assert.Equal(1, TeamOf(state));

        world.UninstallPlugin("NetworkServer");
        server.Dispose();
    }

    [Fact]
    public void TryGetStateAt_BetweenTicks_NoInterpolatorConfigured_SnapsToAtOrBefore()
    {
        using var world = new World();
        var serializer = CreateSerializer();
        var (plugin, server) = InstallServer(world, new ServerNetworkConfig
        {
            Serializer = serializer,
            Interpolator = null, // interpolation disabled
            StateHistoryTicks = 64,
        });
        var entity = world.Spawn().With(new LagPosition { X = 0f }).Build();

        ref var pos = ref world.Get<LagPosition>(entity);
        pos.X = 0f;
        plugin.StateHistory!.Capture(entity, 10, world, serializer);
        pos = ref world.Get<LagPosition>(entity);
        pos.X = 100f;
        plugin.StateHistory.Capture(entity, 20, world, serializer);

        Assert.True(plugin.LagCompensation!.TryGetStateAt(entity, 15, out var state));
        Assert.Equal(0f, XOf(state), 0.001f);

        world.UninstallPlugin("NetworkServer");
        server.Dispose();
    }

    [Fact]
    public void TryGetStateAt_NoLaterTick_ReturnsAtOrBeforeUnchanged()
    {
        using var world = new World();
        var serializer = CreateSerializer();
        var (plugin, server) = InstallServer(world, new ServerNetworkConfig
        {
            Serializer = serializer,
            Interpolator = new LagPositionInterpolator(),
            StateHistoryTicks = 64,
        });
        var entity = world.Spawn().With(new LagPosition { X = 42f }).Build();
        plugin.StateHistory!.Capture(entity, 10, world, serializer);

        // Tick 15 is after the newest recorded tick 10; nothing to interpolate toward.
        Assert.True(plugin.LagCompensation!.TryGetStateAt(entity, 15, out var state));
        Assert.Equal(42f, XOf(state), 0.001f);

        world.UninstallPlugin("NetworkServer");
        server.Dispose();
    }

    [Fact]
    public void TryGetStateAt_UnknownEntity_ReturnsFalse()
    {
        using var world = new World();
        var (plugin, server) = InstallServer(world, new ServerNetworkConfig
        {
            Serializer = CreateSerializer(),
            Interpolator = new LagPositionInterpolator(),
            StateHistoryTicks = 64,
        });
        var entity = world.Spawn().With(new LagPosition { X = 1f }).Build();

        Assert.False(plugin.LagCompensation!.TryGetStateAt(entity, 10, out var state));
        Assert.Empty(state);

        world.UninstallPlugin("NetworkServer");
        server.Dispose();
    }

    #endregion

    #region Rewind

    [Fact]
    public void Rewind_SwapsLiveComponentsToHistoricalValues_WithinScope()
    {
        using var world = new World();
        var serializer = CreateSerializer();
        var (plugin, server) = InstallServer(world, new ServerNetworkConfig
        {
            Serializer = serializer,
            Interpolator = new LagPositionInterpolator(),
            StateHistoryTicks = 64,
        });

        var entity = world.Spawn().With(new LagPosition { X = 10f }).Build();
        plugin.StateHistory!.Capture(entity, 10, world, serializer);

        // Move the live entity away from the recorded position.
        world.Get<LagPosition>(entity).X = 99f;
        Assert.Equal(99f, world.Get<LagPosition>(entity).X, 0.001f);

        using (plugin.LagCompensation!.Rewind([entity], 10))
        {
            Assert.Equal(10f, world.Get<LagPosition>(entity).X, 0.001f);
        }

        world.UninstallPlugin("NetworkServer");
        server.Dispose();
    }

    [Fact]
    public void Rewind_OnDispose_RestoresOriginalLiveValuesExactly()
    {
        using var world = new World();
        var serializer = CreateSerializer();
        var (plugin, server) = InstallServer(world, new ServerNetworkConfig
        {
            Serializer = serializer,
            Interpolator = new LagPositionInterpolator(),
            StateHistoryTicks = 64,
        });

        var entity = world.Spawn().With(new LagPosition { X = 10f }).Build();
        plugin.StateHistory!.Capture(entity, 10, world, serializer);
        world.Get<LagPosition>(entity).X = 99f;

        using (plugin.LagCompensation!.Rewind([entity], 10))
        {
            Assert.Equal(10f, world.Get<LagPosition>(entity).X, 0.001f);
        }

        Assert.Equal(99f, world.Get<LagPosition>(entity).X, 0.001f);

        world.UninstallPlugin("NetworkServer");
        server.Dispose();
    }

    [Fact]
    public void Rewind_ExceptionThrownInsideScope_StillRestoresLiveValues()
    {
        using var world = new World();
        var serializer = CreateSerializer();
        var (plugin, server) = InstallServer(world, new ServerNetworkConfig
        {
            Serializer = serializer,
            Interpolator = new LagPositionInterpolator(),
            StateHistoryTicks = 64,
        });

        var entity = world.Spawn().With(new LagPosition { X = 10f }).Build();
        plugin.StateHistory!.Capture(entity, 10, world, serializer);
        world.Get<LagPosition>(entity).X = 99f;

        var threw = false;
        try
        {
            using (plugin.LagCompensation!.Rewind([entity], 10))
            {
                Assert.Equal(10f, world.Get<LagPosition>(entity).X, 0.001f);
                throw new InvalidOperationException("hit resolution failed");
            }
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        Assert.True(threw);
        Assert.Equal(99f, world.Get<LagPosition>(entity).X, 0.001f);

        world.UninstallPlugin("NetworkServer");
        server.Dispose();
    }

    [Fact]
    public void Rewind_EntityWithoutHistory_IsSkippedWithoutAffectingOthers()
    {
        using var world = new World();
        var serializer = CreateSerializer();
        var (plugin, server) = InstallServer(world, new ServerNetworkConfig
        {
            Serializer = serializer,
            Interpolator = new LagPositionInterpolator(),
            StateHistoryTicks = 64,
        });

        var withHistory = world.Spawn().With(new LagPosition { X = 10f }).Build();
        plugin.StateHistory!.Capture(withHistory, 10, world, serializer);
        world.Get<LagPosition>(withHistory).X = 99f;

        // Never captured, so it has no retained state at tick 10.
        var withoutHistory = world.Spawn().With(new LagPosition { X = 77f }).Build();

        using (plugin.LagCompensation!.Rewind([withHistory, withoutHistory], 10))
        {
            Assert.Equal(10f, world.Get<LagPosition>(withHistory).X, 0.001f);   // rewound
            Assert.Equal(77f, world.Get<LagPosition>(withoutHistory).X, 0.001f); // untouched
        }

        Assert.Equal(99f, world.Get<LagPosition>(withHistory).X, 0.001f);
        Assert.Equal(77f, world.Get<LagPosition>(withoutHistory).X, 0.001f);

        world.UninstallPlugin("NetworkServer");
        server.Dispose();
    }

    #endregion

    #region End-to-end hit detection

    [Fact]
    public async Task LagCompensatedHitTest_RewoundPositionHits_WhereLivePositionMisses()
    {
        using var world = new World();
        var serializer = CreateSerializer();
        var (plugin, server, client, clientId) = await InstallServerWithClientAsync(world, new ServerNetworkConfig
        {
            TickRate = 50,               // 20 ms per tick
            InterpolationDelayMs = 100f, // 5 ticks behind
            Serializer = serializer,
            Interpolator = new LagPositionInterpolator(),
            StateHistoryTicks = 128,
        });

        // RTT 200 ms -> one-way 5 ticks; plus 5 ticks interpolation delay = 10 ticks back.
        plugin.GetConnectedClients().First().RoundTripTimeMs = 200f;

        // Target moves one unit per tick along X. Record its authoritative position each tick.
        var target = world.Spawn().With(new LagPosition { X = 0f }).Build();
        for (uint tick = 1; tick <= 20; tick++)
        {
            world.Get<LagPosition>(target).X = tick;
            plugin.StateHistory!.Capture(target, tick, world, serializer);
            AdvanceTo(plugin, tick);
        }

        // Live target now sits at X = 20; the attacker fired at where they saw it: X = 10.
        const float aimX = 10f;
        const float hitRadius = 0.5f;
        Assert.Equal(20f, world.Get<LagPosition>(target).X, 0.001f);

        static bool IsHit(World w, Entity e, float aim, float radius) =>
            MathF.Abs(w.Get<LagPosition>(e).X - aim) <= radius;

        // Testing against the live position misses (|20 - 10| = 10).
        Assert.False(IsHit(world, target, aimX, hitRadius));

        var perceivedTick = plugin.LagCompensation!.EstimateClientPerceivedTick(clientId);
        Assert.Equal(10u, perceivedTick);

        bool hit;
        using (plugin.LagCompensation.Rewind([target], perceivedTick))
        {
            // Rewound to tick 10 where X = 10, so the shot connects.
            hit = IsHit(world, target, aimX, hitRadius);
        }

        Assert.True(hit);

        // Live state is exactly restored after the scope ends.
        Assert.Equal(20f, world.Get<LagPosition>(target).X, 0.001f);

        world.UninstallPlugin("NetworkServer");
        client.Dispose();
        server.Dispose();
    }

    #endregion
}
