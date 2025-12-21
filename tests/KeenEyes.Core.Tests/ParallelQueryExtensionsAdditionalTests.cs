using System.Collections.Concurrent;

namespace KeenEyes.Tests;

/// <summary>
/// Additional tests for ParallelQueryExtensions to improve coverage on readonly variants and edge cases.
/// </summary>
public class ParallelQueryExtensionsAdditionalTests
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

    #region Three Component ReadOnly Tests

    [Fact]
    public void ForEachParallelReadOnly_ThreeComponents_ProcessesAllEntities()
    {
        using var world = new World();

        for (int i = 0; i < 100; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = i })
                .With(new Velocity { X = 1, Y = 2 })
                .With(new Health { Current = 100, Max = 100 })
                .Build();
        }

        var processedCount = 0;

        world.Query<Position, Velocity, Health>().ForEachParallelReadOnly(
            (Entity e, in Position pos, in Velocity vel, in Health health) =>
            {
                Interlocked.Increment(ref processedCount);
            },
            minEntityCount: 0
        );

        Assert.Equal(100, processedCount);
    }

    [Fact]
    public void ForEachParallelReadOnly_ThreeComponents_SequentialFallback()
    {
        using var world = new World();

        // Create fewer entities than threshold
        for (int i = 0; i < 10; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = 0 })
                .With(new Velocity { X = 1, Y = 1 })
                .With(new Health { Current = 100, Max = 100 })
                .Build();
        }

        var processedCount = 0;

        // Use default threshold to trigger sequential path
        world.Query<Position, Velocity, Health>().ForEachParallelReadOnly(
            (Entity e, in Position pos, in Velocity vel, in Health health) =>
            {
                processedCount++;
            }
        );

        Assert.Equal(10, processedCount);
    }

    [Fact]
    public void ForEachParallel_ThreeComponents_SequentialFallback()
    {
        using var world = new World();

        for (int i = 0; i < 10; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = 0 })
                .With(new Velocity { X = 1, Y = 1 })
                .With(new Health { Current = 100, Max = 100 })
                .Build();
        }

        var sum = 0;

        world.Query<Position, Velocity, Health>().ForEachParallel(
            (Entity e, ref Position pos, ref Velocity vel, ref Health health) =>
            {
                Interlocked.Add(ref sum, (int)pos.X);
            }
        );

        Assert.Equal(45, sum); // 0+1+2+...+9 = 45
    }

    #endregion

    #region Four Component ReadOnly Tests

    [Fact]
    public void ForEachParallelReadOnly_FourComponents_ProcessesAllEntities()
    {
        using var world = new World();

        for (int i = 0; i < 100; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = i })
                .With(new Velocity { X = 1, Y = 2 })
                .With(new Health { Current = 100, Max = 100 })
                .With(new Damage { Amount = 10 })
                .Build();
        }

        var processedCount = 0;

        world.Query<Position, Velocity, Health, Damage>().ForEachParallelReadOnly(
            (Entity e, in Position pos, in Velocity vel, in Health health, in Damage dmg) =>
            {
                Interlocked.Increment(ref processedCount);
            },
            minEntityCount: 0
        );

        Assert.Equal(100, processedCount);
    }

    [Fact]
    public void ForEachParallelReadOnly_FourComponents_SequentialFallback()
    {
        using var world = new World();

        for (int i = 0; i < 10; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = 0 })
                .With(new Velocity { X = 1, Y = 1 })
                .With(new Health { Current = 100, Max = 100 })
                .With(new Damage { Amount = 5 })
                .Build();
        }

        var processedCount = 0;

        world.Query<Position, Velocity, Health, Damage>().ForEachParallelReadOnly(
            (Entity e, in Position pos, in Velocity vel, in Health health, in Damage dmg) =>
            {
                processedCount++;
            }
        );

        Assert.Equal(10, processedCount);
    }

    [Fact]
    public void ForEachParallel_FourComponents_SequentialFallback()
    {
        using var world = new World();

        for (int i = 0; i < 10; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = 0 })
                .With(new Velocity { X = 1, Y = 1 })
                .With(new Health { Current = 100, Max = 100 })
                .With(new Damage { Amount = 5 })
                .Build();
        }

        var sum = 0;

        world.Query<Position, Velocity, Health, Damage>().ForEachParallel(
            (Entity e, ref Position pos, ref Velocity vel, ref Health health, ref Damage dmg) =>
            {
                Interlocked.Add(ref sum, (int)pos.X);
            }
        );

        Assert.Equal(45, sum);
    }

    #endregion

    #region Three Component Modification Tests

    [Fact]
    public void ForEachParallel_ThreeComponents_ModifiesComponents()
    {
        using var world = new World();

        for (int i = 0; i < 100; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = 0 })
                .With(new Velocity { X = 1, Y = 1 })
                .With(new Health { Current = 50, Max = 100 })
                .Build();
        }

        world.Query<Position, Velocity, Health>().ForEachParallel(
            (Entity e, ref Position pos, ref Velocity vel, ref Health health) =>
            {
                pos.Y = pos.X * 2;
                health.Current = health.Max;
            },
            minEntityCount: 0
        );

        foreach (var entity in world.Query<Position, Health>())
        {
            ref var pos = ref world.Get<Position>(entity);
            ref var health = ref world.Get<Health>(entity);
            Assert.Equal(pos.X * 2, pos.Y);
            Assert.Equal(100, health.Current);
        }
    }

    #endregion

    #region Four Component Modification Tests

    [Fact]
    public void ForEachParallel_FourComponents_ModifiesComponents()
    {
        using var world = new World();

        for (int i = 0; i < 100; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = 0 })
                .With(new Velocity { X = 1, Y = 1 })
                .With(new Health { Current = 50, Max = 100 })
                .With(new Damage { Amount = 0 })
                .Build();
        }

        world.Query<Position, Velocity, Health, Damage>().ForEachParallel(
            (Entity e, ref Position pos, ref Velocity vel, ref Health health, ref Damage dmg) =>
            {
                pos.Y = pos.X * 2;
                dmg.Amount = 10;
            },
            minEntityCount: 0
        );

        foreach (var entity in world.Query<Position, Damage>())
        {
            ref var pos = ref world.Get<Position>(entity);
            ref var dmg = ref world.Get<Damage>(entity);
            Assert.Equal(pos.X * 2, pos.Y);
            Assert.Equal(10, dmg.Amount);
        }
    }

    #endregion

    #region Custom Threshold Tests

    [Fact]
    public void ForEachParallel_CustomThreshold_RespectsMinEntityCount()
    {
        using var world = new World();

        for (int i = 0; i < 50; i++)
        {
            world.Spawn().With(new Position { X = i, Y = 0 }).Build();
        }

        var processedCount = 0;

        // With threshold of 100, should use sequential path
        world.Query<Position>().ForEachParallel(
            (Entity e, ref Position pos) =>
            {
                processedCount++;
            },
            minEntityCount: 100
        );

        Assert.Equal(50, processedCount);
    }

    [Fact]
    public void ForEachParallelReadOnly_CustomThreshold_RespectsMinEntityCount()
    {
        using var world = new World();

        for (int i = 0; i < 50; i++)
        {
            world.Spawn().With(new Position { X = i, Y = 0 }).Build();
        }

        var processedCount = 0;

        world.Query<Position>().ForEachParallelReadOnly(
            (Entity e, in Position pos) =>
            {
                processedCount++;
            },
            minEntityCount: 100
        );

        Assert.Equal(50, processedCount);
    }

    #endregion

    #region Empty Query Tests

    [Fact]
    public void ForEachParallelReadOnly_ThreeComponents_EmptyQuery()
    {
        using var world = new World();

        var processedCount = 0;

        world.Query<Position, Velocity, Health>().ForEachParallelReadOnly(
            (Entity e, in Position pos, in Velocity vel, in Health health) =>
            {
                processedCount++;
            },
            minEntityCount: 0
        );

        Assert.Equal(0, processedCount);
    }

    [Fact]
    public void ForEachParallelReadOnly_FourComponents_EmptyQuery()
    {
        using var world = new World();

        var processedCount = 0;

        world.Query<Position, Velocity, Health, Damage>().ForEachParallelReadOnly(
            (Entity e, in Position pos, in Velocity vel, in Health health, in Damage dmg) =>
            {
                processedCount++;
            },
            minEntityCount: 0
        );

        Assert.Equal(0, processedCount);
    }

    #endregion
}
