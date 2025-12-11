namespace KeenEyes.Tests;

/// <summary>
/// Integration tests for client-server multi-world scenarios.
/// Demonstrates using WorldEntityRef for cross-world entity references.
/// </summary>
public class ClientServerIntegrationTests
{
    #region Test Components

    [Component]
    public partial struct Position : IComponent
    {
        public float X;
        public float Y;
    }

    [Component]
    public partial struct Velocity : IComponent
    {
        public float X;
        public float Y;
    }

    [Component]
    public partial struct NetworkedEntity : IComponent
    {
        public WorldEntityRef ServerEntity;
    }

    [Component]
    public partial struct Health : IComponent
    {
        public int Current;
        public int Max;
    }

    #endregion

    [Fact]
    public void ClientServer_SeparateWorlds_WorkIndependently()
    {
        using var serverWorld = new World { Name = "Server" };
        using var clientWorld = new World { Name = "Client" };

        // Server creates entities
        var serverPlayer = serverWorld.Spawn()
            .With(new Position { X = 100, Y = 200 })
            .With(new Health { Current = 100, Max = 100 })
            .Build();

        // Client creates entities
        var clientPlayer = clientWorld.Spawn()
            .With(new Position { X = 95, Y = 195 })
            .With(new Health { Current = 100, Max = 100 })
            .Build();

        // Verify independent worlds
        Assert.Equal(1, serverWorld.EntityCount);
        Assert.Equal(1, clientWorld.EntityCount);
        Assert.NotEqual(serverWorld.Id, clientWorld.Id);
    }

    [Fact]
    public void WorldEntityRef_CanReferenceEntityInAnotherWorld()
    {
        using var serverWorld = new World { Name = "Server" };
        using var clientWorld = new World { Name = "Client" };

        // Server creates an entity
        var serverEntity = serverWorld.Spawn()
            .With(new Position { X = 100, Y = 200 })
            .Build();

        // Client entity tracks its server counterpart
        var clientEntity = clientWorld.Spawn()
            .With(new NetworkedEntity
            {
                ServerEntity = new WorldEntityRef
                {
                    WorldId = serverWorld.Id,
                    Entity = serverEntity
                }
            })
            .Build();

        // Verify the reference
        ref var networked = ref clientWorld.Get<NetworkedEntity>(clientEntity);
        Assert.Equal(serverWorld.Id, networked.ServerEntity.WorldId);
        Assert.Equal(serverEntity, networked.ServerEntity.Entity);
    }

    [Fact]
    public void WorldEntityRef_TryResolve_SucceedsWithValidReference()
    {
        using var serverWorld = new World { Name = "Server" };
        using var clientWorld = new World { Name = "Client" };

        // Server entity
        var serverEntity = serverWorld.Spawn()
            .With(new Position { X = 100, Y = 200 })
            .Build();

        // Client entity with reference to server entity
        var clientEntity = clientWorld.Spawn()
            .With(new NetworkedEntity
            {
                ServerEntity = new WorldEntityRef
                {
                    WorldId = serverWorld.Id,
                    Entity = serverEntity
                }
            })
            .Build();

        // Resolve the reference
        ref var networked = ref clientWorld.Get<NetworkedEntity>(clientEntity);
        var resolved = networked.ServerEntity.TryResolve(
            new IWorld[] { serverWorld, clientWorld },
            out var world,
            out var entity);

        // Verify resolution succeeded
        Assert.True(resolved);
        Assert.NotNull(world);
        Assert.Equal(serverWorld.Id, world!.Id);
        Assert.Equal(serverEntity, entity);

        // Verify we can access the server entity
        ref var serverPos = ref world.Get<Position>(entity);
        Assert.Equal(100, serverPos.X);
        Assert.Equal(200, serverPos.Y);
    }

    [Fact]
    public void WorldEntityRef_TryResolve_FailsWithInvalidWorldId()
    {
        using var serverWorld = new World { Name = "Server" };
        using var clientWorld = new World { Name = "Client" };

        // Create a reference to a non-existent world
        var invalidRef = new WorldEntityRef
        {
            WorldId = Guid.NewGuid(), // Random world ID that doesn't exist
            Entity = new Entity(1, 0)
        };

        // Try to resolve
        var resolved = invalidRef.TryResolve(
            new IWorld[] { serverWorld, clientWorld },
            out var world,
            out var entity);

        // Verify resolution failed
        Assert.False(resolved);
        Assert.Null(world);
        Assert.Equal(default, entity);
    }

    [Fact]
    public void WorldEntityRef_TryResolve_FailsWithDeadEntity()
    {
        using var serverWorld = new World { Name = "Server" };
        using var clientWorld = new World { Name = "Client" };

        // Server creates and then despawns an entity
        var serverEntity = serverWorld.Spawn()
            .With(new Position { X = 100, Y = 200 })
            .Build();

        var entityRef = new WorldEntityRef
        {
            WorldId = serverWorld.Id,
            Entity = serverEntity
        };

        // Despawn the server entity
        serverWorld.Despawn(serverEntity);

        // Try to resolve the now-dead entity
        var resolved = entityRef.TryResolve(
            new IWorld[] { serverWorld, clientWorld },
            out var world,
            out var entity);

        // Verify resolution failed (entity is dead)
        Assert.False(resolved);
        Assert.Null(world);
        Assert.Equal(default, entity);
    }

    [Fact]
    public void ClientServer_UpdatesWorkIndependently()
    {
        using var serverWorld = new World { Name = "Server" };
        using var clientWorld = new World { Name = "Client" };

        // Server entity
        var serverEntity = serverWorld.Spawn()
            .With(new Position { X = 0, Y = 0 })
            .With(new Velocity { X = 10, Y = 0 })
            .Build();

        // Client entity with prediction
        var clientEntity = clientWorld.Spawn()
            .With(new Position { X = 0, Y = 0 })
            .With(new Velocity { X = 10, Y = 0 })
            .Build();

        // Simulate server update
        foreach (var entity in serverWorld.Query<Position, Velocity>())
        {
            ref var pos = ref serverWorld.Get<Position>(entity);
            ref readonly var vel = ref serverWorld.Get<Velocity>(entity);
            pos.X += vel.X;
        }

        // Simulate client update
        foreach (var entity in clientWorld.Query<Position, Velocity>())
        {
            ref var pos = ref clientWorld.Get<Position>(entity);
            ref readonly var vel = ref clientWorld.Get<Velocity>(entity);
            pos.X += vel.X;
        }

        // Both should have updated independently
        ref var serverPos = ref serverWorld.Get<Position>(serverEntity);
        ref var clientPos = ref clientWorld.Get<Position>(clientEntity);

        Assert.Equal(10, serverPos.X);
        Assert.Equal(10, clientPos.X);

        // Modify server position (server authoritative correction)
        serverPos.X = 15;

        // Client position should be unchanged
        Assert.Equal(10, clientPos.X);
    }

    [Fact]
    public void MultipleClients_CanReferenceServerEntities()
    {
        using var serverWorld = new World { Name = "Server" };
        using var client1World = new World { Name = "Client1" };
        using var client2World = new World { Name = "Client2" };

        // Server entity
        var serverPlayer = serverWorld.Spawn()
            .With(new Position { X = 100, Y = 200 })
            .Build();

        // Client 1 entity referencing server
        var client1Entity = client1World.Spawn()
            .With(new NetworkedEntity
            {
                ServerEntity = new WorldEntityRef
                {
                    WorldId = serverWorld.Id,
                    Entity = serverPlayer
                }
            })
            .Build();

        // Client 2 entity referencing server
        var client2Entity = client2World.Spawn()
            .With(new NetworkedEntity
            {
                ServerEntity = new WorldEntityRef
                {
                    WorldId = serverWorld.Id,
                    Entity = serverPlayer
                }
            })
            .Build();

        // Both clients can resolve the same server entity
        var worlds = new IWorld[] { serverWorld, client1World, client2World };

        ref var networked1 = ref client1World.Get<NetworkedEntity>(client1Entity);
        var resolved1 = networked1.ServerEntity.TryResolve(worlds, out var world1, out var entity1);

        ref var networked2 = ref client2World.Get<NetworkedEntity>(client2Entity);
        var resolved2 = networked2.ServerEntity.TryResolve(worlds, out var world2, out var entity2);

        Assert.True(resolved1);
        Assert.True(resolved2);
        Assert.Equal(serverPlayer, entity1);
        Assert.Equal(serverPlayer, entity2);
        Assert.Same(world1, world2);
    }
}
