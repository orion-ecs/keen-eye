namespace KeenEyes.Tests;

public class CommandBufferTests
{
    [Fact]
    public void CreateCommandBuffer_ReturnsNewBuffer()
    {
        using var world = new World();

        var buffer = world.CreateCommandBuffer();

        Assert.NotNull(buffer);
        Assert.Same(world, buffer.World);
        Assert.Equal(0, buffer.Count);
    }

    [Fact]
    public void QueueSpawn_CreatesEntityOnExecute()
    {
        using var world = new World();
        var buffer = world.CreateCommandBuffer();

        buffer.QueueSpawn()
            .With(new Position { X = 10, Y = 20 })
            .Build();

        // Before execute - no entities
        Assert.Empty(world.GetAllEntities());

        buffer.Execute();

        // After execute - entity exists
        var entities = world.GetAllEntities().ToList();
        Assert.Single(entities);

        var entity = entities[0];
        ref var pos = ref world.Get<Position>(entity);
        Assert.Equal(10, pos.X);
        Assert.Equal(20, pos.Y);
    }

    [Fact]
    public void QueueSpawn_WithName_SetsEntityName()
    {
        using var world = new World();
        var buffer = world.CreateCommandBuffer();

        buffer.QueueSpawn("Player")
            .With(new Position { X = 0, Y = 0 })
            .Build();

        buffer.Execute();

        var entity = world.GetAllEntities().First();
        Assert.Equal("Player", world.GetName(entity));
    }

    [Fact]
    public void QueueDespawn_DestroysEntityOnExecute()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new Position { X = 10, Y = 20 })
            .Build();

        var buffer = world.CreateCommandBuffer();
        buffer.QueueDespawn(entity);

        // Before execute - entity still alive
        Assert.True(world.IsAlive(entity));

        buffer.Execute();

        // After execute - entity destroyed
        Assert.False(world.IsAlive(entity));
    }

    [Fact]
    public void QueueAdd_AddsComponentOnExecute()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new Position { X = 10, Y = 20 })
            .Build();

        var buffer = world.CreateCommandBuffer();
        buffer.QueueAdd(entity, new Velocity { X = 1, Y = 2 });

        // Before execute - no velocity
        Assert.False(world.Has<Velocity>(entity));

        buffer.Execute();

        // After execute - velocity added
        Assert.True(world.Has<Velocity>(entity));
        ref var vel = ref world.Get<Velocity>(entity);
        Assert.Equal(1, vel.X);
        Assert.Equal(2, vel.Y);
    }

    [Fact]
    public void QueueAddTag_AddsTagOnExecute()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new Position { X = 10, Y = 20 })
            .Build();

        var buffer = world.CreateCommandBuffer();
        buffer.QueueAddTag<EnemyTag>(entity);

        Assert.False(world.Has<EnemyTag>(entity));

        buffer.Execute();

        Assert.True(world.Has<EnemyTag>(entity));
    }

    [Fact]
    public void QueueSet_UpdatesComponentOnExecute()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new Position { X = 10, Y = 20 })
            .Build();

        var buffer = world.CreateCommandBuffer();
        buffer.QueueSet(entity, new Position { X = 100, Y = 200 });

        // Before execute - original values
        ref var pos = ref world.Get<Position>(entity);
        Assert.Equal(10, pos.X);

        buffer.Execute();

        // After execute - updated values
        ref var pos2 = ref world.Get<Position>(entity);
        Assert.Equal(100, pos2.X);
        Assert.Equal(200, pos2.Y);
    }

    [Fact]
    public void QueueRemove_RemovesComponentOnExecute()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new Position { X = 10, Y = 20 })
            .With(new Velocity { X = 1, Y = 2 })
            .Build();

        var buffer = world.CreateCommandBuffer();
        buffer.QueueRemove<Velocity>(entity);

        Assert.True(world.Has<Velocity>(entity));

        buffer.Execute();

        Assert.False(world.Has<Velocity>(entity));
        Assert.True(world.Has<Position>(entity));
    }

    [Fact]
    public void Execute_ClearsBuffer()
    {
        using var world = new World();
        var buffer = world.CreateCommandBuffer();

        buffer.QueueSpawn().With(new Position { X = 0, Y = 0 }).Build();
        Assert.Equal(1, buffer.Count);

        buffer.Execute();
        Assert.Equal(0, buffer.Count);

        // Executing again does nothing
        buffer.Execute();
        Assert.Single(world.GetAllEntities());
    }

    [Fact]
    public void Clear_RemovesAllCommandsWithoutExecuting()
    {
        using var world = new World();
        var buffer = world.CreateCommandBuffer();

        buffer.QueueSpawn().With(new Position { X = 0, Y = 0 }).Build();
        Assert.Equal(1, buffer.Count);

        buffer.Clear();

        Assert.Equal(0, buffer.Count);
        Assert.Empty(world.GetAllEntities());
    }

    [Fact]
    public void WorldFlush_ExecutesBuffer()
    {
        using var world = new World();
        var buffer = world.CreateCommandBuffer();

        buffer.QueueSpawn().With(new Position { X = 0, Y = 0 }).Build();

        world.Flush(buffer);

        Assert.Single(world.GetAllEntities());
        Assert.Equal(0, buffer.Count);
    }

    [Fact]
    public void QueueDespawn_IgnoresDeadEntity()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new Position { X = 10, Y = 20 })
            .Build();

        world.Despawn(entity);

        var buffer = world.CreateCommandBuffer();
        buffer.QueueDespawn(entity);

        // Should not throw
        buffer.Execute();
    }

    [Fact]
    public void QueueAdd_IgnoresIfEntityAlreadyHasComponent()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new Position { X = 10, Y = 20 })
            .Build();

        var buffer = world.CreateCommandBuffer();
        buffer.QueueAdd(entity, new Position { X = 100, Y = 200 });

        // Should not throw, just ignore
        buffer.Execute();

        // Original value preserved
        ref var pos = ref world.Get<Position>(entity);
        Assert.Equal(10, pos.X);
    }

    [Fact]
    public void QueueSet_IgnoresIfEntityDoesNotHaveComponent()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new Position { X = 10, Y = 20 })
            .Build();

        var buffer = world.CreateCommandBuffer();
        buffer.QueueSet(entity, new Velocity { X = 1, Y = 2 });

        // Should not throw, just ignore
        buffer.Execute();

        Assert.False(world.Has<Velocity>(entity));
    }

    [Fact]
    public void MultipleOperations_ExecuteInOrder()
    {
        using var world = new World();
        var buffer = world.CreateCommandBuffer();

        // Queue spawn
        buffer.QueueSpawn("Test")
            .With(new Position { X = 0, Y = 0 })
            .Build();

        buffer.Execute();

        var entity = world.GetAllEntities().First();

        // Queue add, then set
        buffer.QueueAdd(entity, new Velocity { X = 1, Y = 1 });
        buffer.QueueSet(entity, new Velocity { X = 10, Y = 10 });

        buffer.Execute();

        // Add happens first, then set updates it
        ref var vel = ref world.Get<Velocity>(entity);
        Assert.Equal(10, vel.X);
    }

    [Fact]
    public void DeferredSpawn_WithTag_AddsTag()
    {
        using var world = new World();
        var buffer = world.CreateCommandBuffer();

        buffer.QueueSpawn()
            .With(new Position { X = 0, Y = 0 })
            .WithTag<EnemyTag>()
            .Build();

        buffer.Execute();

        var entity = world.GetAllEntities().First();
        Assert.True(world.Has<EnemyTag>(entity));
    }

    [Fact]
    public void SafeIterationWithCommandBuffer()
    {
        using var world = new World();

        // Create some entities
        for (int i = 0; i < 5; i++)
        {
            world.Spawn()
                .With(new Health { Current = 10 - i * 4, Max = 10 })
                .Build();
        }

        var buffer = world.CreateCommandBuffer();

        // Iterate and queue despawns for "dead" entities
        foreach (var entity in world.Query<Health>())
        {
            ref var health = ref world.Get<Health>(entity);
            if (health.Current <= 0)
            {
                buffer.QueueDespawn(entity);
            }
        }

        buffer.Execute();

        // Only entities with positive health remain
        var remaining = world.GetAllEntities().ToList();
        Assert.Equal(3, remaining.Count);

        foreach (var entity in remaining)
        {
            ref var health = ref world.Get<Health>(entity);
            Assert.True(health.Current > 0);
        }
    }
}
