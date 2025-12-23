namespace KeenEyes.Core.Tests;

/// <summary>
/// Tests for IWorld.Query interface methods to ensure interface implementations are covered.
/// </summary>
public sealed class IWorldQueryTests
{
    private readonly struct Position : IComponent
    {
        public float X { get; init; }
        public float Y { get; init; }
    }

    private readonly struct Velocity : IComponent
    {
        public float X { get; init; }
        public float Y { get; init; }
    }

    private readonly struct Health : IComponent
    {
        public int Value { get; init; }
    }

    private readonly struct Damage : IComponent
    {
        public int Amount { get; init; }
    }

    [Fact]
    public void IWorld_Query_WithOneComponent_ReturnsQueryBuilder()
    {
        using var world = new World();
        IWorld iworld = world;

        var entity = world.Spawn()
            .With(new Position { X = 1, Y = 2 })
            .Build();

        // Use IWorld.Query<T>()
        var queryBuilder = iworld.Query<Position>();
        Assert.NotNull(queryBuilder);

        // Verify it works
        var found = false;
        foreach (var e in queryBuilder)
        {
            if (e.Id == entity.Id)
            {
                found = true;
            }
        }
        Assert.True(found);
    }

    [Fact]
    public void IWorld_Query_WithTwoComponents_ReturnsQueryBuilder()
    {
        using var world = new World();
        IWorld iworld = world;

        var entity = world.Spawn()
            .With(new Position { X = 1, Y = 2 })
            .With(new Velocity { X = 0.5f, Y = 0.5f })
            .Build();

        // Use IWorld.Query<T1, T2>()
        var queryBuilder = iworld.Query<Position, Velocity>();
        Assert.NotNull(queryBuilder);

        var found = false;
        foreach (var e in queryBuilder)
        {
            if (e.Id == entity.Id)
            {
                found = true;
            }
        }
        Assert.True(found);
    }

    [Fact]
    public void IWorld_Query_WithThreeComponents_ReturnsQueryBuilder()
    {
        using var world = new World();
        IWorld iworld = world;

        var entity = world.Spawn()
            .With(new Position { X = 1, Y = 2 })
            .With(new Velocity { X = 0.5f, Y = 0.5f })
            .With(new Health { Value = 100 })
            .Build();

        // Use IWorld.Query<T1, T2, T3>()
        var queryBuilder = iworld.Query<Position, Velocity, Health>();
        Assert.NotNull(queryBuilder);

        var found = false;
        foreach (var e in queryBuilder)
        {
            if (e.Id == entity.Id)
            {
                found = true;
            }
        }
        Assert.True(found);
    }

    [Fact]
    public void IWorld_Query_WithFourComponents_ReturnsQueryBuilder()
    {
        using var world = new World();
        IWorld iworld = world;

        var entity = world.Spawn()
            .With(new Position { X = 1, Y = 2 })
            .With(new Velocity { X = 0.5f, Y = 0.5f })
            .With(new Health { Value = 100 })
            .With(new Damage { Amount = 10 })
            .Build();

        // Use IWorld.Query<T1, T2, T3, T4>()
        var queryBuilder = iworld.Query<Position, Velocity, Health, Damage>();
        Assert.NotNull(queryBuilder);

        var found = false;
        foreach (var e in queryBuilder)
        {
            if (e.Id == entity.Id)
            {
                found = true;
            }
        }
        Assert.True(found);
    }

    [Fact]
    public void IWorld_Query_CanBeUsedInGenericContext()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new Position { X = 5, Y = 10 })
            .Build();

        // Test using IWorld in a generic method
        var result = QueryThroughInterface<Position>(world, entity);
        Assert.True(result);
    }

    private static bool QueryThroughInterface<T>(IWorld world, Entity entity)
        where T : struct, IComponent
    {
        foreach (var e in world.Query<T>())
        {
            if (e.Id == entity.Id)
            {
                return true;
            }
        }
        return false;
    }

    [Fact]
    public void IWorld_Query_MultipleComponents_WorksInGenericContext()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new Position { X = 5, Y = 10 })
            .With(new Velocity { X = 1, Y = 1 })
            .Build();

        var result = QueryTwoComponentsThroughInterface<Position, Velocity>(world, entity);
        Assert.True(result);
    }

    private static bool QueryTwoComponentsThroughInterface<T1, T2>(IWorld world, Entity entity)
        where T1 : struct, IComponent
        where T2 : struct, IComponent
    {
        foreach (var e in world.Query<T1, T2>())
        {
            if (e.Id == entity.Id)
            {
                return true;
            }
        }
        return false;
    }
}
