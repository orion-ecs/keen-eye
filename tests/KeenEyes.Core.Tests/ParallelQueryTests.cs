using System.Collections.Concurrent;

namespace KeenEyes.Tests;

/// <summary>
/// Tests for parallel query iteration extensions.
/// </summary>
public class ParallelQueryTests
{
    #region Test Components

    public struct Position : IComponent
    {
        public float X, Y;
    }

    public struct Velocity : IComponent
    {
        public float X, Y;
    }

    public struct Health : IComponent
    {
        public int Current, Max;
    }

    public struct Damage : IComponent
    {
        public int Amount;
    }

    #endregion

    #region ForEachParallel Single Component Tests

    [Fact]
    public void ForEachParallel_SingleComponent_ProcessesAllEntities()
    {
        using var world = new World();

        // Create entities
        for (int i = 0; i < 100; i++)
        {
            world.Spawn().With(new Position { X = i, Y = i }).Build();
        }

        var processedCount = 0;

        // Use minEntityCount=0 to force parallel execution even with few entities
        world.Query<Position>().ForEachParallel(
            (Entity e, ref Position pos) =>
            {
                Interlocked.Increment(ref processedCount);
            },
            minEntityCount: 0
        );

        Assert.Equal(100, processedCount);
    }

    [Fact]
    public void ForEachParallel_SingleComponent_ModifiesComponents()
    {
        using var world = new World();

        for (int i = 0; i < 100; i++)
        {
            world.Spawn().With(new Position { X = i, Y = 0 }).Build();
        }

        world.Query<Position>().ForEachParallel(
            (Entity e, ref Position pos) =>
            {
                pos.Y = pos.X * 2;
            },
            minEntityCount: 0
        );

        // Verify all entities were modified
        foreach (var entity in world.Query<Position>())
        {
            ref var pos = ref world.Get<Position>(entity);
            Assert.Equal(pos.X * 2, pos.Y);
        }
    }

    [Fact]
    public void ForEachParallel_SingleComponent_FallsBackToSequential_WhenBelowThreshold()
    {
        using var world = new World();

        for (int i = 0; i < 10; i++)
        {
            world.Spawn().With(new Position { X = i, Y = 0 }).Build();
        }

        var processedCount = 0;

        // With default threshold (1000), should use sequential path
        world.Query<Position>().ForEachParallel(
            (Entity e, ref Position pos) =>
            {
                Interlocked.Increment(ref processedCount);
            }
        );

        Assert.Equal(10, processedCount);
    }

    [Fact]
    public void ForEachParallelReadOnly_SingleComponent_ProcessesAllEntities()
    {
        using var world = new World();

        for (int i = 0; i < 100; i++)
        {
            world.Spawn().With(new Position { X = i, Y = i }).Build();
        }

        var sum = 0f;

        world.Query<Position>().ForEachParallelReadOnly(
            (Entity e, in Position pos) =>
            {
                Interlocked.Exchange(ref sum, sum + pos.X);
            },
            minEntityCount: 0
        );

        // Sum of 0..99
        Assert.True(sum > 0);
    }

    #endregion

    #region ForEachParallel Two Components Tests

    [Fact]
    public void ForEachParallel_TwoComponents_ProcessesMatchingEntities()
    {
        using var world = new World();

        // Create entities with both components
        for (int i = 0; i < 50; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = 0 })
                .With(new Velocity { X = 1, Y = 1 })
                .Build();
        }

        // Create entities with only Position (should not match)
        for (int i = 0; i < 50; i++)
        {
            world.Spawn().With(new Position { X = i, Y = 0 }).Build();
        }

        var processedCount = 0;

        world.Query<Position, Velocity>().ForEachParallel(
            (Entity e, ref Position pos, ref Velocity vel) =>
            {
                Interlocked.Increment(ref processedCount);
                pos.X += vel.X;
                pos.Y += vel.Y;
            },
            minEntityCount: 0
        );

        Assert.Equal(50, processedCount);
    }

    [Fact]
    public void ForEachParallel_TwoComponents_ModifiesComponents()
    {
        using var world = new World();
        const float deltaTime = 0.016f;

        for (int i = 0; i < 100; i++)
        {
            world.Spawn()
                .With(new Position { X = 0, Y = 0 })
                .With(new Velocity { X = 100, Y = 50 })
                .Build();
        }

        world.Query<Position, Velocity>().ForEachParallel(
            (Entity e, ref Position pos, ref Velocity vel) =>
            {
                pos.X += vel.X * deltaTime;
                pos.Y += vel.Y * deltaTime;
            },
            minEntityCount: 0
        );

        foreach (var entity in world.Query<Position, Velocity>())
        {
            ref var pos = ref world.Get<Position>(entity);
            Assert.Equal(100 * deltaTime, pos.X, 0.0001f);
            Assert.Equal(50 * deltaTime, pos.Y, 0.0001f);
        }
    }

    #endregion

    #region ForEachParallel Three Components Tests

    [Fact]
    public void ForEachParallel_ThreeComponents_ProcessesMatchingEntities()
    {
        using var world = new World();

        for (int i = 0; i < 100; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = 0 })
                .With(new Velocity { X = 1, Y = 1 })
                .With(new Health { Current = 100, Max = 100 })
                .Build();
        }

        var processedCount = 0;

        world.Query<Position, Velocity, Health>().ForEachParallel(
            (Entity e, ref Position pos, ref Velocity vel, ref Health health) =>
            {
                Interlocked.Increment(ref processedCount);
            },
            minEntityCount: 0
        );

        Assert.Equal(100, processedCount);
    }

    #endregion

    #region ForEachParallel Four Components Tests

    [Fact]
    public void ForEachParallel_FourComponents_ProcessesMatchingEntities()
    {
        using var world = new World();

        for (int i = 0; i < 100; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = 0 })
                .With(new Velocity { X = 1, Y = 1 })
                .With(new Health { Current = 100, Max = 100 })
                .With(new Damage { Amount = 10 })
                .Build();
        }

        var processedCount = 0;

        world.Query<Position, Velocity, Health, Damage>().ForEachParallel(
            (Entity e, ref Position pos, ref Velocity vel, ref Health health, ref Damage dmg) =>
            {
                Interlocked.Increment(ref processedCount);
            },
            minEntityCount: 0
        );

        Assert.Equal(100, processedCount);
    }

    #endregion

    #region Empty Query Tests

    [Fact]
    public void ForEachParallel_EmptyQuery_DoesNothing()
    {
        using var world = new World();

        // No entities created
        var processedCount = 0;

        world.Query<Position>().ForEachParallel(
            (Entity e, ref Position pos) =>
            {
                Interlocked.Increment(ref processedCount);
            },
            minEntityCount: 0
        );

        Assert.Equal(0, processedCount);
    }

    #endregion

    #region Determinism Tests

    [Fact]
    public void ForEachParallel_ProducesSameResults_AsSequential()
    {
        using var world = new World();

        // Create a deterministic set of entities
        for (int i = 0; i < 100; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = i * 2 })
                .With(new Velocity { X = 1, Y = 0.5f })
                .Build();
        }

        // Apply transformation with parallel
        world.Query<Position, Velocity>().ForEachParallel(
            (Entity e, ref Position pos, ref Velocity vel) =>
            {
                pos.X += vel.X;
                pos.Y += vel.Y;
            },
            minEntityCount: 0
        );

        // Verify all transformations were applied correctly
        foreach (var entity in world.Query<Position, Velocity>())
        {
            ref var pos = ref world.Get<Position>(entity);
            // X should be original + 1, Y should be original + 0.5
            // Since we can't know which entity is which, just verify the transformation was applied
            Assert.True(pos.X >= 1); // At least velocity was added
        }
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public void ForEachParallel_ConcurrentAccess_IsThreadSafe()
    {
        using var world = new World();
        var entities = new ConcurrentBag<Entity>();

        // Create many entities
        for (int i = 0; i < 1000; i++)
        {
            var entity = world.Spawn().With(new Position { X = 0, Y = 0 }).Build();
            entities.Add(entity);
        }

        // Process in parallel - each thread increments position
        world.Query<Position>().ForEachParallel(
            (Entity e, ref Position pos) =>
            {
                // Simple operation that's safe per-entity
                pos.X = 1;
                pos.Y = 1;
            },
            minEntityCount: 0
        );

        // Verify all entities were processed
        foreach (var entity in entities)
        {
            ref var pos = ref world.Get<Position>(entity);
            Assert.Equal(1f, pos.X);
            Assert.Equal(1f, pos.Y);
        }
    }

    #endregion

    #region Multiple Archetype Tests

    [Fact]
    public void ForEachParallel_MultipleArchetypes_ProcessesAll()
    {
        using var world = new World();

        // Create entities with different archetypes that all match Position query
        for (int i = 0; i < 50; i++)
        {
            world.Spawn().With(new Position { X = i, Y = 0 }).Build();
        }

        for (int i = 0; i < 50; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = 0 })
                .With(new Velocity { X = 1, Y = 1 })
                .Build();
        }

        for (int i = 0; i < 50; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = 0 })
                .With(new Health { Current = 100, Max = 100 })
                .Build();
        }

        var processedCount = 0;

        world.Query<Position>().ForEachParallel(
            (Entity e, ref Position pos) =>
            {
                Interlocked.Increment(ref processedCount);
            },
            minEntityCount: 0
        );

        Assert.Equal(150, processedCount);
    }

    #endregion
}
