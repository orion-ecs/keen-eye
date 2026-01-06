namespace KeenEyes.Parallelism.Tests;

/// <summary>
/// Test components for parallel query extension tests.
/// </summary>
public struct ParallelTestPosition : IComponent
{
    public float X;
    public float Y;
}

public struct ParallelTestVelocity : IComponent
{
    public float X;
    public float Y;
}

public struct ParallelTestHealth : IComponent
{
    public int Current;
    public int Max;
}

public struct ParallelTestDamage : IComponent
{
    public int Amount;
}

/// <summary>
/// Tests for ParallelQueryExtensions.
/// </summary>
[Collection("ParallelismTests")]
public class ParallelQueryExtensionsTests
{
    #region Single Component - ForEachParallel

    [Fact]
    public void ForEachParallel_SingleComponent_ProcessesAllEntities_Sequential()
    {
        using var world = new World();
        var processedCount = 0;

        // Create entities below the threshold to trigger sequential path
        for (int i = 0; i < 10; i++)
        {
            world.Spawn().With(new ParallelTestPosition { X = i, Y = i }).Build();
        }

        world.Query<ParallelTestPosition>().ForEachParallel<ParallelTestPosition>(
            (Entity e, ref ParallelTestPosition pos) =>
            {
                Interlocked.Increment(ref processedCount);
            },
            minEntityCount: 100); // Force sequential

        Assert.Equal(10, processedCount);
    }

    [Fact]
    public void ForEachParallel_SingleComponent_ProcessesAllEntities_Parallel()
    {
        using var world = new World();
        var processedCount = 0;

        // Create entities above the threshold
        for (int i = 0; i < 100; i++)
        {
            world.Spawn().With(new ParallelTestPosition { X = i, Y = i }).Build();
        }

        world.Query<ParallelTestPosition>().ForEachParallel<ParallelTestPosition>(
            (Entity e, ref ParallelTestPosition pos) =>
            {
                Interlocked.Increment(ref processedCount);
            },
            minEntityCount: 10); // Force parallel

        Assert.Equal(100, processedCount);
    }

    [Fact]
    public void ForEachParallel_SingleComponent_ModifiesComponents()
    {
        using var world = new World();
        var entities = new List<Entity>();

        for (int i = 0; i < 50; i++)
        {
            entities.Add(world.Spawn().With(new ParallelTestPosition { X = i, Y = i }).Build());
        }

        world.Query<ParallelTestPosition>().ForEachParallel<ParallelTestPosition>(
            (Entity e, ref ParallelTestPosition pos) =>
            {
                pos.X *= 2;
                pos.Y *= 2;
            },
            minEntityCount: 10);

        // Verify modifications
        for (int i = 0; i < 50; i++)
        {
            ref var pos = ref world.Get<ParallelTestPosition>(entities[i]);
            Assert.Equal(i * 2, pos.X);
            Assert.Equal(i * 2, pos.Y);
        }
    }

    #endregion

    #region Single Component - ForEachParallelReadOnly

    [Fact]
    public void ForEachParallelReadOnly_SingleComponent_ProcessesAllEntities_Sequential()
    {
        using var world = new World();
        var processedCount = 0;

        for (int i = 0; i < 10; i++)
        {
            world.Spawn().With(new ParallelTestPosition { X = i, Y = 0 }).Build();
        }

        world.Query<ParallelTestPosition>().ForEachParallelReadOnly<ParallelTestPosition>(
            (Entity e, in ParallelTestPosition pos) =>
            {
                Interlocked.Increment(ref processedCount);
            },
            minEntityCount: 100);

        Assert.Equal(10, processedCount);
    }

    [Fact]
    public void ForEachParallelReadOnly_SingleComponent_ProcessesAllEntities_Parallel()
    {
        using var world = new World();
        var processedCount = 0;

        for (int i = 0; i < 100; i++)
        {
            world.Spawn().With(new ParallelTestPosition { X = i, Y = i }).Build();
        }

        world.Query<ParallelTestPosition>().ForEachParallelReadOnly<ParallelTestPosition>(
            (Entity e, in ParallelTestPosition pos) =>
            {
                Interlocked.Increment(ref processedCount);
            },
            minEntityCount: 10);

        Assert.Equal(100, processedCount);
    }

    #endregion

    #region Two Components - ForEachParallel

    [Fact]
    public void ForEachParallel_TwoComponents_ProcessesAllEntities_Sequential()
    {
        using var world = new World();
        var processedCount = 0;

        for (int i = 0; i < 10; i++)
        {
            world.Spawn()
                .With(new ParallelTestPosition { X = i, Y = i })
                .With(new ParallelTestVelocity { X = 1, Y = 1 })
                .Build();
        }

        world.Query<ParallelTestPosition, ParallelTestVelocity>().ForEachParallel<ParallelTestPosition, ParallelTestVelocity>(
            (Entity e, ref ParallelTestPosition pos, ref ParallelTestVelocity vel) =>
            {
                Interlocked.Increment(ref processedCount);
            },
            minEntityCount: 100);

        Assert.Equal(10, processedCount);
    }

    [Fact]
    public void ForEachParallel_TwoComponents_ProcessesAllEntities_Parallel()
    {
        using var world = new World();
        var processedCount = 0;

        for (int i = 0; i < 100; i++)
        {
            world.Spawn()
                .With(new ParallelTestPosition { X = i, Y = i })
                .With(new ParallelTestVelocity { X = 1, Y = 1 })
                .Build();
        }

        world.Query<ParallelTestPosition, ParallelTestVelocity>().ForEachParallel<ParallelTestPosition, ParallelTestVelocity>(
            (Entity e, ref ParallelTestPosition pos, ref ParallelTestVelocity vel) =>
            {
                Interlocked.Increment(ref processedCount);
            },
            minEntityCount: 10);

        Assert.Equal(100, processedCount);
    }

    [Fact]
    public void ForEachParallel_TwoComponents_ModifiesComponents()
    {
        using var world = new World();
        var entities = new List<Entity>();

        for (int i = 0; i < 50; i++)
        {
            entities.Add(world.Spawn()
                .With(new ParallelTestPosition { X = 0, Y = 0 })
                .With(new ParallelTestVelocity { X = i, Y = i * 2 })
                .Build());
        }

        world.Query<ParallelTestPosition, ParallelTestVelocity>().ForEachParallel<ParallelTestPosition, ParallelTestVelocity>(
            (Entity e, ref ParallelTestPosition pos, ref ParallelTestVelocity vel) =>
            {
                pos.X += vel.X;
                pos.Y += vel.Y;
            },
            minEntityCount: 10);

        // Verify modifications
        for (int i = 0; i < 50; i++)
        {
            ref var pos = ref world.Get<ParallelTestPosition>(entities[i]);
            Assert.Equal(i, pos.X);
            Assert.Equal(i * 2, pos.Y);
        }
    }

    #endregion

    #region Two Components - ForEachParallelReadOnly

    [Fact]
    public void ForEachParallelReadOnly_TwoComponents_ProcessesAllEntities_Sequential()
    {
        using var world = new World();
        var processedCount = 0;

        for (int i = 0; i < 10; i++)
        {
            world.Spawn()
                .With(new ParallelTestPosition { X = i, Y = i })
                .With(new ParallelTestVelocity { X = 1, Y = 1 })
                .Build();
        }

        world.Query<ParallelTestPosition, ParallelTestVelocity>().ForEachParallelReadOnly<ParallelTestPosition, ParallelTestVelocity>(
            (Entity e, in ParallelTestPosition pos, in ParallelTestVelocity vel) =>
            {
                Interlocked.Increment(ref processedCount);
            },
            minEntityCount: 100);

        Assert.Equal(10, processedCount);
    }

    [Fact]
    public void ForEachParallelReadOnly_TwoComponents_ProcessesAllEntities_Parallel()
    {
        using var world = new World();
        var processedCount = 0;

        for (int i = 0; i < 100; i++)
        {
            world.Spawn()
                .With(new ParallelTestPosition { X = i, Y = i })
                .With(new ParallelTestVelocity { X = 1, Y = 1 })
                .Build();
        }

        world.Query<ParallelTestPosition, ParallelTestVelocity>().ForEachParallelReadOnly<ParallelTestPosition, ParallelTestVelocity>(
            (Entity e, in ParallelTestPosition pos, in ParallelTestVelocity vel) =>
            {
                Interlocked.Increment(ref processedCount);
            },
            minEntityCount: 10);

        Assert.Equal(100, processedCount);
    }

    #endregion

    #region Three Components - ForEachParallel

    [Fact]
    public void ForEachParallel_ThreeComponents_ProcessesAllEntities_Sequential()
    {
        using var world = new World();
        var processedCount = 0;

        for (int i = 0; i < 10; i++)
        {
            world.Spawn()
                .With(new ParallelTestPosition { X = i, Y = i })
                .With(new ParallelTestVelocity { X = 1, Y = 1 })
                .With(new ParallelTestHealth { Current = 100, Max = 100 })
                .Build();
        }

        world.Query<ParallelTestPosition, ParallelTestVelocity, ParallelTestHealth>()
            .ForEachParallel<ParallelTestPosition, ParallelTestVelocity, ParallelTestHealth>(
                (Entity e, ref ParallelTestPosition pos, ref ParallelTestVelocity vel, ref ParallelTestHealth health) =>
                {
                    Interlocked.Increment(ref processedCount);
                },
                minEntityCount: 100);

        Assert.Equal(10, processedCount);
    }

    [Fact]
    public void ForEachParallel_ThreeComponents_ProcessesAllEntities_Parallel()
    {
        using var world = new World();
        var processedCount = 0;

        for (int i = 0; i < 100; i++)
        {
            world.Spawn()
                .With(new ParallelTestPosition { X = i, Y = i })
                .With(new ParallelTestVelocity { X = 1, Y = 1 })
                .With(new ParallelTestHealth { Current = 100, Max = 100 })
                .Build();
        }

        world.Query<ParallelTestPosition, ParallelTestVelocity, ParallelTestHealth>()
            .ForEachParallel<ParallelTestPosition, ParallelTestVelocity, ParallelTestHealth>(
                (Entity e, ref ParallelTestPosition pos, ref ParallelTestVelocity vel, ref ParallelTestHealth health) =>
                {
                    Interlocked.Increment(ref processedCount);
                },
                minEntityCount: 10);

        Assert.Equal(100, processedCount);
    }

    [Fact]
    public void ForEachParallel_ThreeComponents_ModifiesComponents()
    {
        using var world = new World();
        var entities = new List<Entity>();

        for (int i = 0; i < 50; i++)
        {
            entities.Add(world.Spawn()
                .With(new ParallelTestPosition { X = 0, Y = 0 })
                .With(new ParallelTestVelocity { X = 1, Y = 2 })
                .With(new ParallelTestHealth { Current = 100, Max = 100 })
                .Build());
        }

        world.Query<ParallelTestPosition, ParallelTestVelocity, ParallelTestHealth>()
            .ForEachParallel<ParallelTestPosition, ParallelTestVelocity, ParallelTestHealth>(
                (Entity e, ref ParallelTestPosition pos, ref ParallelTestVelocity vel, ref ParallelTestHealth health) =>
                {
                    pos.X += vel.X;
                    pos.Y += vel.Y;
                    health.Current -= 10;
                },
                minEntityCount: 10);

        foreach (var entity in entities)
        {
            ref var pos = ref world.Get<ParallelTestPosition>(entity);
            ref var health = ref world.Get<ParallelTestHealth>(entity);
            Assert.Equal(1, pos.X);
            Assert.Equal(2, pos.Y);
            Assert.Equal(90, health.Current);
        }
    }

    #endregion

    #region Three Components - ForEachParallelReadOnly

    [Fact]
    public void ForEachParallelReadOnly_ThreeComponents_ProcessesAllEntities_Sequential()
    {
        using var world = new World();
        var processedCount = 0;

        for (int i = 0; i < 10; i++)
        {
            world.Spawn()
                .With(new ParallelTestPosition { X = i, Y = i })
                .With(new ParallelTestVelocity { X = 1, Y = 1 })
                .With(new ParallelTestHealth { Current = 100, Max = 100 })
                .Build();
        }

        world.Query<ParallelTestPosition, ParallelTestVelocity, ParallelTestHealth>()
            .ForEachParallelReadOnly<ParallelTestPosition, ParallelTestVelocity, ParallelTestHealth>(
                (Entity e, in ParallelTestPosition pos, in ParallelTestVelocity vel, in ParallelTestHealth health) =>
                {
                    Interlocked.Increment(ref processedCount);
                },
                minEntityCount: 100);

        Assert.Equal(10, processedCount);
    }

    [Fact]
    public void ForEachParallelReadOnly_ThreeComponents_ProcessesAllEntities_Parallel()
    {
        using var world = new World();
        var processedCount = 0;

        for (int i = 0; i < 100; i++)
        {
            world.Spawn()
                .With(new ParallelTestPosition { X = i, Y = i })
                .With(new ParallelTestVelocity { X = 1, Y = 1 })
                .With(new ParallelTestHealth { Current = 100, Max = 100 })
                .Build();
        }

        world.Query<ParallelTestPosition, ParallelTestVelocity, ParallelTestHealth>()
            .ForEachParallelReadOnly<ParallelTestPosition, ParallelTestVelocity, ParallelTestHealth>(
                (Entity e, in ParallelTestPosition pos, in ParallelTestVelocity vel, in ParallelTestHealth health) =>
                {
                    Interlocked.Increment(ref processedCount);
                },
                minEntityCount: 10);

        Assert.Equal(100, processedCount);
    }

    #endregion

    #region Four Components - ForEachParallel

    [Fact]
    public void ForEachParallel_FourComponents_ProcessesAllEntities_Sequential()
    {
        using var world = new World();
        var processedCount = 0;

        for (int i = 0; i < 10; i++)
        {
            world.Spawn()
                .With(new ParallelTestPosition { X = i, Y = i })
                .With(new ParallelTestVelocity { X = 1, Y = 1 })
                .With(new ParallelTestHealth { Current = 100, Max = 100 })
                .With(new ParallelTestDamage { Amount = 10 })
                .Build();
        }

        world.Query<ParallelTestPosition, ParallelTestVelocity, ParallelTestHealth, ParallelTestDamage>()
            .ForEachParallel<ParallelTestPosition, ParallelTestVelocity, ParallelTestHealth, ParallelTestDamage>(
                (Entity e, ref ParallelTestPosition pos, ref ParallelTestVelocity vel, ref ParallelTestHealth health, ref ParallelTestDamage damage) =>
                {
                    Interlocked.Increment(ref processedCount);
                },
                minEntityCount: 100);

        Assert.Equal(10, processedCount);
    }

    [Fact]
    public void ForEachParallel_FourComponents_ProcessesAllEntities_Parallel()
    {
        using var world = new World();
        var processedCount = 0;

        for (int i = 0; i < 100; i++)
        {
            world.Spawn()
                .With(new ParallelTestPosition { X = i, Y = i })
                .With(new ParallelTestVelocity { X = 1, Y = 1 })
                .With(new ParallelTestHealth { Current = 100, Max = 100 })
                .With(new ParallelTestDamage { Amount = 10 })
                .Build();
        }

        world.Query<ParallelTestPosition, ParallelTestVelocity, ParallelTestHealth, ParallelTestDamage>()
            .ForEachParallel<ParallelTestPosition, ParallelTestVelocity, ParallelTestHealth, ParallelTestDamage>(
                (Entity e, ref ParallelTestPosition pos, ref ParallelTestVelocity vel, ref ParallelTestHealth health, ref ParallelTestDamage damage) =>
                {
                    Interlocked.Increment(ref processedCount);
                },
                minEntityCount: 10);

        Assert.Equal(100, processedCount);
    }

    [Fact]
    public void ForEachParallel_FourComponents_ModifiesComponents()
    {
        using var world = new World();
        var entities = new List<Entity>();

        for (int i = 0; i < 50; i++)
        {
            entities.Add(world.Spawn()
                .With(new ParallelTestPosition { X = 0, Y = 0 })
                .With(new ParallelTestVelocity { X = 1, Y = 2 })
                .With(new ParallelTestHealth { Current = 100, Max = 100 })
                .With(new ParallelTestDamage { Amount = 5 })
                .Build());
        }

        world.Query<ParallelTestPosition, ParallelTestVelocity, ParallelTestHealth, ParallelTestDamage>()
            .ForEachParallel<ParallelTestPosition, ParallelTestVelocity, ParallelTestHealth, ParallelTestDamage>(
                (Entity e, ref ParallelTestPosition pos, ref ParallelTestVelocity vel, ref ParallelTestHealth health, ref ParallelTestDamage damage) =>
                {
                    pos.X += vel.X;
                    pos.Y += vel.Y;
                    health.Current -= damage.Amount;
                },
                minEntityCount: 10);

        foreach (var entity in entities)
        {
            ref var pos = ref world.Get<ParallelTestPosition>(entity);
            ref var health = ref world.Get<ParallelTestHealth>(entity);
            Assert.Equal(1, pos.X);
            Assert.Equal(2, pos.Y);
            Assert.Equal(95, health.Current);
        }
    }

    #endregion

    #region Four Components - ForEachParallelReadOnly

    [Fact]
    public void ForEachParallelReadOnly_FourComponents_ProcessesAllEntities_Sequential()
    {
        using var world = new World();
        var processedCount = 0;

        for (int i = 0; i < 10; i++)
        {
            world.Spawn()
                .With(new ParallelTestPosition { X = i, Y = i })
                .With(new ParallelTestVelocity { X = 1, Y = 1 })
                .With(new ParallelTestHealth { Current = 100, Max = 100 })
                .With(new ParallelTestDamage { Amount = 10 })
                .Build();
        }

        world.Query<ParallelTestPosition, ParallelTestVelocity, ParallelTestHealth, ParallelTestDamage>()
            .ForEachParallelReadOnly<ParallelTestPosition, ParallelTestVelocity, ParallelTestHealth, ParallelTestDamage>(
                (Entity e, in ParallelTestPosition pos, in ParallelTestVelocity vel, in ParallelTestHealth health, in ParallelTestDamage damage) =>
                {
                    Interlocked.Increment(ref processedCount);
                },
                minEntityCount: 100);

        Assert.Equal(10, processedCount);
    }

    [Fact]
    public void ForEachParallelReadOnly_FourComponents_ProcessesAllEntities_Parallel()
    {
        using var world = new World();
        var processedCount = 0;

        for (int i = 0; i < 100; i++)
        {
            world.Spawn()
                .With(new ParallelTestPosition { X = i, Y = i })
                .With(new ParallelTestVelocity { X = 1, Y = 1 })
                .With(new ParallelTestHealth { Current = 100, Max = 100 })
                .With(new ParallelTestDamage { Amount = 10 })
                .Build();
        }

        world.Query<ParallelTestPosition, ParallelTestVelocity, ParallelTestHealth, ParallelTestDamage>()
            .ForEachParallelReadOnly<ParallelTestPosition, ParallelTestVelocity, ParallelTestHealth, ParallelTestDamage>(
                (Entity e, in ParallelTestPosition pos, in ParallelTestVelocity vel, in ParallelTestHealth health, in ParallelTestDamage damage) =>
                {
                    Interlocked.Increment(ref processedCount);
                },
                minEntityCount: 10);

        Assert.Equal(100, processedCount);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ForEachParallel_EmptyQuery_ProcessesNothing()
    {
        using var world = new World();
        var processedCount = 0;

        world.Query<ParallelTestPosition>().ForEachParallel<ParallelTestPosition>(
            (Entity e, ref ParallelTestPosition pos) =>
            {
                Interlocked.Increment(ref processedCount);
            },
            minEntityCount: 10);

        Assert.Equal(0, processedCount);
    }

    [Fact]
    public void ForEachParallel_ExactlyAtThreshold_UsesParallel()
    {
        using var world = new World();
        var processedCount = 0;

        // Create exactly at threshold
        for (int i = 0; i < 10; i++)
        {
            world.Spawn().With(new ParallelTestPosition { X = i, Y = i }).Build();
        }

        world.Query<ParallelTestPosition>().ForEachParallel<ParallelTestPosition>(
            (Entity e, ref ParallelTestPosition pos) =>
            {
                Interlocked.Increment(ref processedCount);
            },
            minEntityCount: 10); // Exactly at threshold

        Assert.Equal(10, processedCount);
    }

    [Fact]
    public void ForEachParallel_JustBelowThreshold_UsesSequential()
    {
        using var world = new World();
        var processedCount = 0;

        // Create just below threshold
        for (int i = 0; i < 9; i++)
        {
            world.Spawn().With(new ParallelTestPosition { X = i, Y = i }).Build();
        }

        world.Query<ParallelTestPosition>().ForEachParallel<ParallelTestPosition>(
            (Entity e, ref ParallelTestPosition pos) =>
            {
                Interlocked.Increment(ref processedCount);
            },
            minEntityCount: 10);

        Assert.Equal(9, processedCount);
    }

    [Fact]
    public void DefaultMinEntityCount_HasReasonableValue()
    {
        Assert.Equal(1000, ParallelQueryExtensions.DefaultMinEntityCount);
    }

    #endregion

    #region Query With Filters

    [Fact]
    public void ForEachParallel_WithQueryFilters_RespectsFilters()
    {
        using var world = new World();
        var processedCount = 0;

        // Create entities with Position
        for (int i = 0; i < 50; i++)
        {
            world.Spawn().With(new ParallelTestPosition { X = i, Y = i }).Build();
        }

        // Create entities with Position and Velocity
        for (int i = 0; i < 50; i++)
        {
            world.Spawn()
                .With(new ParallelTestPosition { X = i, Y = i })
                .With(new ParallelTestVelocity { X = 1, Y = 1 })
                .Build();
        }

        // Query only entities with both Position and Velocity
        world.Query<ParallelTestPosition, ParallelTestVelocity>()
            .ForEachParallel<ParallelTestPosition, ParallelTestVelocity>(
                (Entity e, ref ParallelTestPosition pos, ref ParallelTestVelocity vel) =>
                {
                    Interlocked.Increment(ref processedCount);
                },
                minEntityCount: 10);

        Assert.Equal(50, processedCount);
    }

    #endregion

    #region Thread Safety

    [Fact]
    public void ForEachParallel_ConcurrentModification_IsThreadSafe()
    {
        using var world = new World();
        var entities = new List<Entity>();

        for (int i = 0; i < 1000; i++)
        {
            entities.Add(world.Spawn()
                .With(new ParallelTestPosition { X = 0, Y = 0 })
                .Build());
        }

        // Each entity modifies its own component - should be thread safe
        world.Query<ParallelTestPosition>().ForEachParallel<ParallelTestPosition>(
            (Entity e, ref ParallelTestPosition pos) =>
            {
                pos.X += 1;
                pos.Y += 1;
            },
            minEntityCount: 10);

        // Verify all entities were modified exactly once
        foreach (var entity in entities)
        {
            ref var pos = ref world.Get<ParallelTestPosition>(entity);
            Assert.Equal(1, pos.X);
            Assert.Equal(1, pos.Y);
        }
    }

    #endregion
}
