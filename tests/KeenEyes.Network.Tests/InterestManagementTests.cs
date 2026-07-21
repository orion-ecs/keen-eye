using System.Numerics;
using KeenEyes.Network.Components;
using KeenEyes.Network.Protocol;
using KeenEyes.Network.Serialization;
using KeenEyes.Network.Transport;
using KeenEyes.Testing.Network;

namespace KeenEyes.Network.Tests;

// LocalTransport completes synchronously, so Wait() is safe
#pragma warning disable xUnit1031 // Do not use blocking task operations
#pragma warning disable xUnit1051 // Use TestContext.Current.CancellationToken

/// <summary>
/// Replicated position component used to drive spatial interest decisions.
/// </summary>
public struct InterestPosition : IComponent
{
    /// <summary>The X coordinate.</summary>
    public float X;

    /// <summary>The Y coordinate.</summary>
    public float Y;

    /// <summary>The Z coordinate.</summary>
    public float Z;
}

/// <summary>
/// Replicated server-authoritative value component for delta assertions.
/// </summary>
public struct InterestHealth : IComponent
{
    /// <summary>The server-controlled value.</summary>
    public float Value;
}

/// <summary>
/// Owner-authoritative component used to verify echo suppression on the filtered path.
/// </summary>
public struct InterestOwnerInput : IComponent
{
    /// <summary>The owner-controlled value.</summary>
    public float Value;
}

/// <summary>
/// Tests for interest management / area-of-interest filtering (issue #585):
/// the per-client replication path, <see cref="SpatialInterestManager"/> scope
/// enter/exit behavior, owner relevance invariants, relevance-update throttling,
/// and the spatial grid math.
/// </summary>
public sealed class InterestManagementTests
{
    private const int TickRate = 60;
    private const float TickDelta = 0.02f;

    #region Harness

    private sealed class Harness : IDisposable
    {
        public required LocalTransport ServerTransport { get; init; }
        public required LocalTransport ClientTransport { get; init; }
        public required World ServerWorld { get; init; }
        public required World ClientWorld { get; init; }
        public required NetworkServerPlugin ServerPlugin { get; init; }
        public required NetworkClientPlugin ClientPlugin { get; init; }
        public required int ClientId { get; init; }

        public void Dispose()
        {
            ClientWorld.UninstallPlugin("NetworkClient");
            ServerWorld.UninstallPlugin("NetworkServer");
            ClientTransport.Dispose();
            ServerTransport.Dispose();
            ClientWorld.Dispose();
            ServerWorld.Dispose();
        }
    }

    private static MockNetworkSerializer CreateSerializer()
    {
        var serializer = new MockNetworkSerializer();

        serializer.RegisterComponent<InterestPosition>(
            serialize: (ref BitWriter w, InterestPosition c) =>
            {
                w.WriteFloat(c.X);
                w.WriteFloat(c.Y);
                w.WriteFloat(c.Z);
            },
            deserialize: (ref BitReader r) => new InterestPosition
            {
                X = r.ReadFloat(),
                Y = r.ReadFloat(),
                Z = r.ReadFloat(),
            },
            new NetworkComponentInfo
            {
                Type = typeof(InterestPosition),
                NetworkTypeId = 1,
                Strategy = SyncStrategy.Authoritative,
                Frequency = 0,
                Priority = 128,
                SupportsInterpolation = false,
                SupportsPrediction = false,
                SupportsDelta = false,
            });

        serializer.RegisterComponent<InterestHealth>(
            serialize: (ref BitWriter w, InterestHealth c) => w.WriteFloat(c.Value),
            deserialize: (ref BitReader r) => new InterestHealth { Value = r.ReadFloat() },
            new NetworkComponentInfo
            {
                Type = typeof(InterestHealth),
                NetworkTypeId = 2,
                Strategy = SyncStrategy.Authoritative,
                Frequency = 0,
                Priority = 128,
                SupportsInterpolation = false,
                SupportsPrediction = false,
                SupportsDelta = false,
            });

        serializer.RegisterComponent<InterestOwnerInput>(
            serialize: (ref BitWriter w, InterestOwnerInput c) => w.WriteFloat(c.Value),
            deserialize: (ref BitReader r) => new InterestOwnerInput { Value = r.ReadFloat() },
            new NetworkComponentInfo
            {
                Type = typeof(InterestOwnerInput),
                NetworkTypeId = 3,
                Strategy = SyncStrategy.OwnerAuthoritative,
                Frequency = 0,
                Priority = 128,
                SupportsInterpolation = false,
                SupportsPrediction = false,
                SupportsDelta = false,
            });

        return serializer;
    }

    private static Vector3? GetPosition(IWorld world, Entity entity)
    {
        if (!world.Has<InterestPosition>(entity))
        {
            return null;
        }

        ref readonly var position = ref world.Get<InterestPosition>(entity);
        return new Vector3(position.X, position.Y, position.Z);
    }

    private static SpatialInterestManager CreateSpatialManager(
        float updateFrequencyHz = 0f,
        Func<IWorld, int, Vector3?>? viewpointProvider = null)
        => new()
        {
            CellSize = 50f,
            ViewDistance = 100f,
            UpdateFrequencyHz = updateFrequencyHz,
            PositionProvider = GetPosition,
            ViewpointProvider = viewpointProvider,
        };

    private static Harness CreateConnectedHarness(IInterestManager? interestManager)
    {
        var (server, client) = LocalTransport.CreatePair();
        var serverWorld = new World();
        var clientWorld = new World();

        // The client applies incoming components by Type, which requires the
        // types to be registered in the client world up front.
        clientWorld.Components.Register<InterestPosition>();
        clientWorld.Components.Register<InterestHealth>();
        clientWorld.Components.Register<InterestOwnerInput>();

        var serverPlugin = new NetworkServerPlugin(server, new ServerNetworkConfig
        {
            TickRate = TickRate,
            Serializer = CreateSerializer(),
            InterestManager = interestManager,
        });
        serverWorld.InstallPlugin(serverPlugin);

        var clientPlugin = new NetworkClientPlugin(client, new ClientNetworkConfig
        {
            TickRate = TickRate,
            EnablePrediction = false,
            Serializer = CreateSerializer(),
        });
        clientWorld.InstallPlugin(clientPlugin);

        server.ListenAsync(7777).Wait();
        clientPlugin.ConnectAsync().Wait();
        server.Update();
        client.Update();

        return new Harness
        {
            ServerTransport = server,
            ClientTransport = client,
            ServerWorld = serverWorld,
            ClientWorld = clientWorld,
            ServerPlugin = serverPlugin,
            ClientPlugin = clientPlugin,
            ClientId = clientPlugin.LocalClientId,
        };
    }

    /// <summary>
    /// Runs one server network tick and delivers its messages to the client.
    /// </summary>
    private static void Step(Harness harness, float deltaTime = TickDelta)
    {
        harness.ServerWorld.Update(deltaTime);
        harness.ClientTransport.Update();
        harness.ServerTransport.Update();
    }

    private static (Entity Entity, uint NetId) SpawnAt(
        Harness harness,
        float x,
        int ownerId = NetworkOwner.ServerClientId,
        float health = 0f)
    {
        var entity = harness.ServerWorld.Spawn()
            .With(new InterestPosition { X = x })
            .With(new InterestHealth { Value = health })
            .Build();
        var netId = harness.ServerPlugin.RegisterNetworkedEntity(entity, ownerId);
        return (entity, netId.Value);
    }

    private static bool TryGetClientEntity(World clientWorld, uint netId, out Entity found)
    {
        foreach (var entity in clientWorld.Query<NetworkId>())
        {
            if (clientWorld.Get<NetworkId>(entity).Value == netId)
            {
                found = entity;
                return true;
            }
        }

        found = default;
        return false;
    }

    /// <summary>
    /// Counts messages about a single network ID arriving at a transport.
    /// </summary>
    private sealed class MessageCounter
    {
        public int Spawns { get; private set; }
        public int Despawns { get; private set; }
        public int Deltas { get; private set; }

        public MessageCounter(LocalTransport transport, uint netId)
        {
            transport.DataReceived += (_, data) =>
            {
                var reader = new NetworkMessageReader(data);
                reader.ReadHeader(out var type, out uint _);
                switch (type)
                {
                    case MessageType.EntitySpawn:
                        reader.ReadEntitySpawn(out var spawnId, out _);
                        if (spawnId == netId)
                        {
                            Spawns++;
                        }
                        break;

                    case MessageType.EntityDespawn:
                        reader.ReadEntityDespawn(out var despawnId);
                        if (despawnId == netId)
                        {
                            Despawns++;
                        }
                        break;

                    case MessageType.ComponentDelta:
                        if (reader.ReadNetworkId() == netId)
                        {
                            Deltas++;
                        }
                        break;
                }
            };
        }
    }

    #endregion

    #region Null interest manager (broadcast path)

    [Fact]
    public void Send_NullInterestManager_FarEntityStillReplicatedToClient()
    {
        using var harness = CreateConnectedHarness(interestManager: null);

        SpawnAt(harness, x: 0f, ownerId: harness.ClientId);
        var (_, farNetId) = SpawnAt(harness, x: 10_000f, health: 3f);

        Step(harness);

        // Without an interest manager everything is broadcast, distance is ignored.
        Assert.True(TryGetClientEntity(harness.ClientWorld, farNetId, out var clientEntity));
        Assert.Equal(3f, harness.ClientWorld.Get<InterestHealth>(clientEntity).Value, 0.001f);
    }

    #endregion

    #region Spatial relevance

    [Fact]
    public void Send_SpatialInterestManager_NearEntity_ReplicatedToClient()
    {
        using var harness = CreateConnectedHarness(CreateSpatialManager());

        SpawnAt(harness, x: 0f, ownerId: harness.ClientId);
        var (_, nearNetId) = SpawnAt(harness, x: 10f, health: 5f);

        Step(harness);

        Assert.True(TryGetClientEntity(harness.ClientWorld, nearNetId, out var clientEntity));
        Assert.Equal(5f, harness.ClientWorld.Get<InterestHealth>(clientEntity).Value, 0.001f);
    }

    [Fact]
    public void Send_SpatialInterestManager_FarEntity_NeverArrivesAtClient()
    {
        using var harness = CreateConnectedHarness(CreateSpatialManager());

        SpawnAt(harness, x: 0f, ownerId: harness.ClientId);
        var (_, farNetId) = SpawnAt(harness, x: 10_000f);
        var counter = new MessageCounter(harness.ClientTransport, farNetId);

        for (var i = 0; i < 5; i++)
        {
            Step(harness);
        }

        Assert.False(TryGetClientEntity(harness.ClientWorld, farNetId, out _));
        Assert.Equal(0, counter.Spawns);
        Assert.Equal(0, counter.Deltas);
    }

    [Fact]
    public void Send_SpatialInterestManager_EntityWithoutPosition_AlwaysReplicated()
    {
        using var harness = CreateConnectedHarness(CreateSpatialManager());

        // No owned entity and no viewpoint: only globally relevant entities replicate.
        var entity = harness.ServerWorld.Spawn().With(new InterestHealth { Value = 3f }).Build();
        var netId = harness.ServerPlugin.RegisterNetworkedEntity(entity);

        Step(harness);

        Assert.True(TryGetClientEntity(harness.ClientWorld, netId.Value, out var clientEntity));
        Assert.Equal(3f, harness.ClientWorld.Get<InterestHealth>(clientEntity).Value, 0.001f);
    }

    [Fact]
    public void Send_AlwaysRelevantInterestManager_FarEntityReplicatedToClient()
    {
        using var harness = CreateConnectedHarness(new AlwaysRelevantInterestManager());

        var (_, farNetId) = SpawnAt(harness, x: 10_000f, health: 7f);

        Step(harness);

        Assert.True(TryGetClientEntity(harness.ClientWorld, farNetId, out var clientEntity));
        Assert.Equal(7f, harness.ClientWorld.Get<InterestHealth>(clientEntity).Value, 0.001f);
    }

    #endregion

    #region Scope transitions

    [Fact]
    public void Send_SpatialInterestManager_EntityEnteringScope_ClientReceivesSpawnThenDeltas()
    {
        using var harness = CreateConnectedHarness(CreateSpatialManager());

        SpawnAt(harness, x: 0f, ownerId: harness.ClientId);
        var (serverEntity, netId) = SpawnAt(harness, x: 10_000f, health: 1f);
        var counter = new MessageCounter(harness.ClientTransport, netId);

        Step(harness);
        Step(harness);
        Assert.False(TryGetClientEntity(harness.ClientWorld, netId, out _));

        // Entity moves into view distance.
        harness.ServerWorld.Set(serverEntity, new InterestPosition { X = 10f });
        Step(harness);

        Assert.True(TryGetClientEntity(harness.ClientWorld, netId, out var clientEntity));
        Assert.Equal(1f, harness.ClientWorld.Get<InterestHealth>(clientEntity).Value, 0.001f);
        Assert.Equal(1, counter.Spawns);

        // Subsequent changes arrive as deltas against the per-client baseline.
        harness.ServerWorld.Set(serverEntity, new InterestHealth { Value = 2f });
        Step(harness);

        Assert.Equal(2f, harness.ClientWorld.Get<InterestHealth>(clientEntity).Value, 0.001f);
        Assert.Equal(1, counter.Spawns);
        Assert.Equal(1, counter.Deltas);
    }

    [Fact]
    public void Send_SpatialInterestManager_EntityLeavingScope_ClientReceivesDespawnAndNoFurtherDeltas()
    {
        using var harness = CreateConnectedHarness(CreateSpatialManager());

        SpawnAt(harness, x: 0f, ownerId: harness.ClientId);
        var (serverEntity, netId) = SpawnAt(harness, x: 10f, health: 1f);

        Step(harness);
        Assert.True(TryGetClientEntity(harness.ClientWorld, netId, out _));

        var counter = new MessageCounter(harness.ClientTransport, netId);

        // Entity moves out of view distance.
        harness.ServerWorld.Set(serverEntity, new InterestPosition { X = 10_000f });
        Step(harness);

        Assert.False(TryGetClientEntity(harness.ClientWorld, netId, out _));
        Assert.Equal(1, counter.Despawns);

        // Changes while out of scope produce no traffic for this client.
        harness.ServerWorld.Set(serverEntity, new InterestHealth { Value = 2f });
        for (var i = 0; i < 3; i++)
        {
            Step(harness);
        }

        Assert.Equal(0, counter.Deltas);
        Assert.Equal(0, counter.Spawns);
    }

    [Fact]
    public void Send_SpatialInterestManager_ReEntry_ResendsFullStateWithFreshValues()
    {
        using var harness = CreateConnectedHarness(CreateSpatialManager());

        SpawnAt(harness, x: 0f, ownerId: harness.ClientId);
        var (serverEntity, netId) = SpawnAt(harness, x: 10f, health: 1f);
        var counter = new MessageCounter(harness.ClientTransport, netId);

        Step(harness);
        Assert.True(TryGetClientEntity(harness.ClientWorld, netId, out var clientEntity));
        Assert.Equal(1f, harness.ClientWorld.Get<InterestHealth>(clientEntity).Value, 0.001f);

        // Leave scope, then change state while the client cannot see the entity.
        harness.ServerWorld.Set(serverEntity, new InterestPosition { X = 10_000f });
        Step(harness);
        Assert.False(TryGetClientEntity(harness.ClientWorld, netId, out _));

        harness.ServerWorld.Set(serverEntity, new InterestHealth { Value = 99f });
        Step(harness);

        // Re-enter scope: stale per-client baseline was invalidated on exit,
        // so the client receives a fresh full spawn with the new value.
        harness.ServerWorld.Set(serverEntity, new InterestPosition { X = 10f });
        Step(harness);

        Assert.True(TryGetClientEntity(harness.ClientWorld, netId, out clientEntity));
        Assert.Equal(99f, harness.ClientWorld.Get<InterestHealth>(clientEntity).Value, 0.001f);
        Assert.Equal(2, counter.Spawns);
    }

    #endregion

    #region Owner invariants

    [Fact]
    public void Send_SpatialInterestManager_OwnedEntityOutsideViewDistance_StillReplicatedToOwner()
    {
        // Pin the client's viewpoint at the origin so distance is judged from there.
        using var harness = CreateConnectedHarness(
            CreateSpatialManager(viewpointProvider: (_, _) => Vector3.Zero));

        var (_, ownedNetId) = SpawnAt(harness, x: 10_000f, ownerId: harness.ClientId, health: 4f);
        var (_, strangerNetId) = SpawnAt(harness, x: 10_010f);

        Step(harness);

        // The owned entity is far from the viewpoint but must always replicate to
        // its owner; the equally-far non-owned entity proves the manager did
        // consider that region out of range.
        Assert.True(TryGetClientEntity(harness.ClientWorld, ownedNetId, out var clientEntity));
        Assert.Equal(4f, harness.ClientWorld.Get<InterestHealth>(clientEntity).Value, 0.001f);
        Assert.False(TryGetClientEntity(harness.ClientWorld, strangerNetId, out _));
    }

    [Fact]
    public void Send_SpatialInterestManager_OwnerAuthoritativeComponent_NotEchoedToOwner()
    {
        using var harness = CreateConnectedHarness(CreateSpatialManager());

        var serverEntity = harness.ServerWorld.Spawn()
            .With(new InterestPosition { X = 0f })
            .With(new InterestHealth { Value = 1f })
            .With(new InterestOwnerInput { Value = 1f })
            .Build();
        var netId = harness.ServerPlugin.RegisterNetworkedEntity(serverEntity, harness.ClientId);

        Step(harness);
        Assert.True(TryGetClientEntity(harness.ClientWorld, netId.Value, out _));

        var counter = new MessageCounter(harness.ClientTransport, netId.Value);

        // Owner-authoritative change: suppressed toward the owner.
        harness.ServerWorld.Set(serverEntity, new InterestOwnerInput { Value = 5f });
        Step(harness);
        Assert.Equal(0, counter.Deltas);

        // Server-authoritative change on the same entity still reaches the owner.
        harness.ServerWorld.Set(serverEntity, new InterestHealth { Value = 7f });
        Step(harness);
        Assert.Equal(1, counter.Deltas);
    }

    #endregion

    #region Update frequency throttling

    [Fact]
    public void Send_SpatialInterestManager_UpdateFrequency_ThrottlesRelevanceRecomputation()
    {
        using var harness = CreateConnectedHarness(CreateSpatialManager(updateFrequencyHz: 1f));

        SpawnAt(harness, x: 0f, ownerId: harness.ClientId);
        var (serverEntity, netId) = SpawnAt(harness, x: 10f);

        // First tick forces an initial relevance computation for the new client.
        Step(harness);
        Assert.True(TryGetClientEntity(harness.ClientWorld, netId, out _));

        // Move out of range; at 1 Hz the relevance set is not recomputed on the
        // next few ticks, so the entity stays in scope.
        harness.ServerWorld.Set(serverEntity, new InterestPosition { X = 10_000f });
        for (var i = 0; i < 5; i++)
        {
            Step(harness);
        }

        Assert.True(TryGetClientEntity(harness.ClientWorld, netId, out _));

        // Once a full update interval has elapsed, the recompute runs and the
        // client receives the despawn.
        Step(harness, deltaTime: 1.1f);

        Assert.False(TryGetClientEntity(harness.ClientWorld, netId, out _));
    }

    #endregion

    #region Grid math

    [Fact]
    public void GetCell_PositionInsideCell_ReturnsContainingCell()
    {
        var cell = SpatialInterestManager.GetCell(new Vector3(150f, 250f, 50f), 100f);

        Assert.Equal((1, 2, 0), cell);
    }

    [Fact]
    public void GetCell_NegativeCoordinates_FloorsTowardNegativeInfinity()
    {
        var cell = SpatialInterestManager.GetCell(new Vector3(-0.5f, -100.1f, -200f), 100f);

        Assert.Equal((-1, -2, -2), cell);
    }

    [Fact]
    public void GetCell_ExactBoundary_AssignsToUpperCell()
    {
        var cell = SpatialInterestManager.GetCell(new Vector3(100f, 0f, 0f), 100f);

        Assert.Equal((1, 0, 0), cell);
    }

    [Fact]
    public void GetCellRadius_NonDivisibleDistance_RoundsUp()
    {
        Assert.Equal(3, SpatialInterestManager.GetCellRadius(250f, 100f));
        Assert.Equal(2, SpatialInterestManager.GetCellRadius(200f, 100f));
    }

    [Fact]
    public void IsWithinCellRadius_UsesChebyshevDistance()
    {
        Assert.True(SpatialInterestManager.IsWithinCellRadius((2, 2, 2), (0, 0, 0), 2));
        Assert.True(SpatialInterestManager.IsWithinCellRadius((-2, 1, 0), (0, 0, 0), 2));
        Assert.False(SpatialInterestManager.IsWithinCellRadius((3, 0, 0), (0, 0, 0), 2));
        Assert.False(SpatialInterestManager.IsWithinCellRadius((0, 0, -3), (0, 0, 0), 2));
    }

    #endregion
}
