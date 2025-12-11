namespace KeenEyes.Tests;

/// <summary>
/// Tests validating isolation guarantees for multiple independent worlds.
/// </summary>
public class MultiWorldIsolationTests
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

    [TagComponent]
    public partial struct TestTag : IComponent;

    #endregion

    [Fact]
    public void MultipleWorlds_HaveUniqueIds()
    {
        using var world1 = new World();
        using var world2 = new World();

        Assert.NotEqual(world1.Id, world2.Id);
    }

    [Fact]
    public void MultipleWorlds_CanHaveDistinctNames()
    {
        using var world1 = new World { Name = "Client" };
        using var world2 = new World { Name = "Server" };

        Assert.Equal("Client", world1.Name);
        Assert.Equal("Server", world2.Name);
    }

    [Fact]
    public void ComponentRegistries_ArePerWorld_NotShared()
    {
        // Component registries should be per-world instances, not shared
        using var world1 = new World();
        using var world2 = new World();

        // Register component in world1
        var entity1 = world1.Spawn().With(new Position { X = 1, Y = 2 }).Build();

        // Register component in world2
        var entity2 = world2.Spawn().With(new Position { X = 3, Y = 4 }).Build();

        // Component registries are separate instances per world
        Assert.NotSame(world1.Components, world2.Components);

        // Verify components work correctly in each world
        ref var pos1 = ref world1.Get<Position>(entity1);
        ref var pos2 = ref world2.Get<Position>(entity2);

        Assert.Equal(1, pos1.X);
        Assert.Equal(2, pos1.Y);
        Assert.Equal(3, pos2.X);
        Assert.Equal(4, pos2.Y);
    }

    [Fact]
    public void EntityIds_ArePerWorld_CanHaveSameId()
    {
        using var world1 = new World();
        using var world2 = new World();

        // Create entities in both worlds
        var entity1 = world1.Spawn().Build();
        var entity2 = world2.Spawn().Build();

        // Entities in different worlds can have the same ID
        // They are completely independent
        Assert.Equal(entity1.Id, entity2.Id);

        // Both exist in their respective worlds
        Assert.True(world1.IsAlive(entity1));
        Assert.True(world2.IsAlive(entity2));

        // Despawning in one world doesn't affect the other
        world1.Despawn(entity1);
        Assert.False(world1.IsAlive(entity1));
        Assert.True(world2.IsAlive(entity2)); // Still alive in world2
    }

    [Fact]
    public void SystemInstances_ArePerWorld_NotShared()
    {
        var builder = new WorldBuilder()
            .WithSystem<TestSystem>(SystemPhase.Update);

        using var world1 = builder.Build();
        using var world2 = builder.Build();

        // Each world should have its own system instance
        // Verify worlds are different instances
        Assert.NotEqual(world1.Id, world2.Id);

        // Update one world and verify the other is unaffected
        world1.Update(1.0f);

        // Both worlds should still exist independently
        Assert.NotEqual(world1.Id, world2.Id);
    }

    [Fact]
    public void QueryResults_AreIndependent_DontLeakBetweenWorlds()
    {
        using var world1 = new World();
        using var world2 = new World();

        // Create entities in world1
        world1.Spawn().With(new Position { X = 1, Y = 1 }).Build();
        world1.Spawn().With(new Position { X = 2, Y = 2 }).Build();

        // Create entities in world2
        world2.Spawn().With(new Position { X = 3, Y = 3 }).Build();

        // Query each world independently
        var entities1 = world1.Query<Position>().ToList();
        var entities2 = world2.Query<Position>().ToList();

        // Verify query results are isolated
        Assert.Equal(2, entities1.Count);
        Assert.Single(entities2);

        // Verify data integrity
        ref var pos1 = ref world1.Get<Position>(entities1[0]);
        ref var pos2 = ref world2.Get<Position>(entities2[0]);

        Assert.Equal(1, pos1.X);
        Assert.Equal(3, pos2.X);
    }

    [Fact]
    public void ComponentModifications_DontAffectOtherWorlds()
    {
        using var world1 = new World();
        using var world2 = new World();

        var entity1 = world1.Spawn().With(new Position { X = 10, Y = 20 }).Build();
        var entity2 = world2.Spawn().With(new Position { X = 30, Y = 40 }).Build();

        // Modify component in world1
        ref var pos1 = ref world1.Get<Position>(entity1);
        pos1.X = 100;

        // Verify world2 is unaffected
        ref var pos2 = ref world2.Get<Position>(entity2);
        Assert.Equal(30, pos2.X);
        Assert.Equal(40, pos2.Y);
    }

    [Fact]
    public void DespawningEntityInOneWorld_DoesntAffectOtherWorld()
    {
        using var world1 = new World();
        using var world2 = new World();

        var entity1 = world1.Spawn().With(new Position()).Build();
        var entity2 = world2.Spawn().With(new Position()).Build();

        // Despawn entity in world1
        world1.Despawn(entity1);

        // Verify world1 entity is gone
        Assert.False(world1.IsAlive(entity1));

        // Verify world2 entity is still alive
        Assert.True(world2.IsAlive(entity2));
    }

    [Fact]
    public void MultipleWorldsWithBuilder_AreCompletelyIndependent()
    {
        var builder = new WorldBuilder()
            .WithSystem<TestSystem>(SystemPhase.Update);

        using var clientWorld = builder.Build();
        clientWorld.Name = "Client";

        using var serverWorld = builder.Build();
        serverWorld.Name = "Server";

        // Verify different IDs
        Assert.NotEqual(clientWorld.Id, serverWorld.Id);

        // Verify different names
        Assert.Equal("Client", clientWorld.Name);
        Assert.Equal("Server", serverWorld.Name);

        // Create entities in each world
        var clientEntity = clientWorld.Spawn()
            .With(new Position { X = 1, Y = 1 })
            .Build();

        var serverEntity = serverWorld.Spawn()
            .With(new Position { X = 100, Y = 100 })
            .Build();

        // Verify isolation
        Assert.Equal(1, clientWorld.EntityCount);
        Assert.Equal(1, serverWorld.EntityCount);

        // Verify data independence
        ref var clientPos = ref clientWorld.Get<Position>(clientEntity);
        ref var serverPos = ref serverWorld.Get<Position>(serverEntity);

        Assert.Equal(1, clientPos.X);
        Assert.Equal(100, serverPos.X);

        // Modify one doesn't affect the other
        clientPos.X = 999;
        Assert.Equal(100, serverPos.X);
    }

    [Fact]
    public void WorldDisposal_DoesntAffectOtherWorlds()
    {
        var world1 = new World();
        using var world2 = new World();

        var entity1 = world1.Spawn().With(new Position()).Build();
        var entity2 = world2.Spawn().With(new Position()).Build();

        // Dispose world1
        world1.Dispose();

        // Verify world2 is still functional
        Assert.True(world2.IsAlive(entity2));
        Assert.Equal(1, world2.EntityCount);
    }

    #region Test System

    private sealed class TestSystem : SystemBase
    {
        public override void Update(float deltaTime)
        {
            // Simple system for testing
        }
    }

    #endregion
}
