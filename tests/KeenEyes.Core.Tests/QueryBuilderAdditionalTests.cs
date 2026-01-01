using Health = KeenEyes.Tests.TestHealth;
using Position = KeenEyes.Tests.TestPosition;
using Velocity = KeenEyes.Tests.TestVelocity;

namespace KeenEyes.Tests;

/// <summary>
/// Additional tests for QueryBuilder to improve coverage.
/// </summary>
public class QueryBuilderAdditionalTests
{
    #region Count Method Tests

    [Fact]
    public void QueryBuilder_Count_NoStringTags_FastPath()
    {
        using var world = new World();

        // Create entities with Position
        for (int i = 0; i < 10; i++)
        {
            world.Spawn().With(new Position { X = i, Y = i }).Build();
        }

        // Create entities with Position + Velocity
        for (int i = 0; i < 5; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = i })
                .With(new Velocity { X = 1, Y = 1 })
                .Build();
        }

        // Query Position only (no string tags - fast path)
        var count = world.Query<Position>().Count();

        Assert.Equal(15, count);
    }

    [Fact]
    public void QueryBuilder_Count_WithStringTags_SlowPath()
    {
        using var world = new World();

        // Create entities with Position
        for (int i = 0; i < 10; i++)
        {
            var entity = world.Spawn().With(new Position { X = i, Y = i }).Build();
            if (i % 2 == 0)
            {
                world.AddTag(entity, "even");
            }
        }

        // Query with string tag filter (slow path)
        var count = world.Query<Position>().WithTag("even").Count();

        Assert.Equal(5, count);
    }

    [Fact]
    public void QueryBuilder_Count_WithoutStringTags_SlowPath()
    {
        using var world = new World();

        // Create entities with Position
        for (int i = 0; i < 10; i++)
        {
            var entity = world.Spawn().With(new Position { X = i, Y = i }).Build();
            if (i < 3)
            {
                world.AddTag(entity, "frozen");
            }
        }

        // Query without string tag filter (slow path)
        var count = world.Query<Position>().WithoutTag("frozen").Count();

        Assert.Equal(7, count);
    }

    [Fact]
    public void QueryBuilder_Count_EmptyResult_FastPath()
    {
        using var world = new World();

        // No entities created
        var count = world.Query<Position>().Count();

        Assert.Equal(0, count);
    }

    [Fact]
    public void QueryBuilder_Count_EmptyResult_SlowPath()
    {
        using var world = new World();

        world.Spawn().With(new Position()).Build();

        // Query with non-existent tag (slow path)
        var count = world.Query<Position>().WithTag("nonexistent").Count();

        Assert.Equal(0, count);
    }

    [Fact]
    public void QueryBuilder_Count_MultipleArchetypes_FastPath()
    {
        using var world = new World();

        // Create entities in different archetypes
        world.Spawn().With(new Position()).Build();
        world.Spawn().With(new Position()).With(new Velocity()).Build();
        world.Spawn().With(new Position()).With(new Health()).Build();

        var count = world.Query<Position>().Count();

        Assert.Equal(3, count);
    }

    [Fact]
    public void QueryBuilder_Count_WithFilter_FastPath()
    {
        using var world = new World();

        // Create entities with different component combinations
        for (int i = 0; i < 5; i++)
        {
            world.Spawn().With(new Position()).Build();
        }
        for (int i = 0; i < 3; i++)
        {
            world.Spawn().With(new Position()).With(new Velocity()).Build();
        }

        // Query Position without Velocity (no string tags - fast path)
        var count = world.Query<Position>().Without<Velocity>().Count();

        Assert.Equal(5, count);
    }

    [Fact]
    public void QueryBuilder_Count_CombinedFilters_SlowPath()
    {
        using var world = new World();

        // Create entities
        for (int i = 0; i < 10; i++)
        {
            var entity = world.Spawn()
                .With(new Position { X = i, Y = i })
                .With(new Velocity { X = 1, Y = 1 })
                .Build();

            if (i % 2 == 0)
            {
                world.AddTag(entity, "active");
            }
        }

        // Query with component filter AND string tag (slow path due to string tag)
        var count = world.Query<Position, Velocity>().WithTag("active").Count();

        Assert.Equal(5, count);
    }

    #endregion

    #region Tag Validation Tests

    [Fact]
    public void QueryBuilder_WithTag_NullTag_ThrowsArgumentNullException()
    {
        using var world = new World();

        var query = world.Query<Position>();

        Assert.Throws<ArgumentNullException>(() => query.WithTag(null!));
    }

    [Fact]
    public void QueryBuilder_WithTag_EmptyTag_ThrowsArgumentException()
    {
        using var world = new World();

        var query = world.Query<Position>();

        Assert.Throws<ArgumentException>(() => query.WithTag(""));
    }

    [Fact]
    public void QueryBuilder_WithTag_WhitespaceTag_ThrowsArgumentException()
    {
        using var world = new World();

        var query = world.Query<Position>();

        Assert.Throws<ArgumentException>(() => query.WithTag("   "));
    }

    [Fact]
    public void QueryBuilder_WithTag_TabOnlyTag_ThrowsArgumentException()
    {
        using var world = new World();

        var query = world.Query<Position>();

        Assert.Throws<ArgumentException>(() => query.WithTag("\t"));
    }

    [Fact]
    public void QueryBuilder_WithTag_NewlineOnlyTag_ThrowsArgumentException()
    {
        using var world = new World();

        var query = world.Query<Position>();

        Assert.Throws<ArgumentException>(() => query.WithTag("\n"));
    }

    [Fact]
    public void QueryBuilder_WithTag_ValidTag_NoException()
    {
        using var world = new World();

        var query = world.Query<Position>();

        var result = query.WithTag("valid"); // Should not throw

        // QueryBuilder is a value type - verify it's usable
        _ = result.ToList();
    }

    [Fact]
    public void QueryBuilder_WithoutTag_NullTag_ThrowsArgumentNullException()
    {
        using var world = new World();

        var query = world.Query<Position>();

        Assert.Throws<ArgumentNullException>(() => query.WithoutTag(null!));
    }

    [Fact]
    public void QueryBuilder_WithoutTag_EmptyTag_ThrowsArgumentException()
    {
        using var world = new World();

        var query = world.Query<Position>();

        Assert.Throws<ArgumentException>(() => query.WithoutTag(""));
    }

    [Fact]
    public void QueryBuilder_WithoutTag_WhitespaceTag_ThrowsArgumentException()
    {
        using var world = new World();

        var query = world.Query<Position>();

        Assert.Throws<ArgumentException>(() => query.WithoutTag("   "));
    }

    [Fact]
    public void QueryBuilder_WithoutTag_ValidTag_NoException()
    {
        using var world = new World();

        var query = world.Query<Position>();

        var result = query.WithoutTag("valid"); // Should not throw

        // QueryBuilder is a value type - verify it's usable
        _ = result.ToList();
    }

    #endregion

    #region IQueryBuilder Interface Tests

    [Fact]
    public void QueryBuilder_IQueryBuilder_With_ReturnsIQueryBuilder()
    {
        using var world = new World();

        IQueryBuilder query = world.Query<Position>();
        IQueryBuilder result = query.With<Velocity>();

        Assert.NotNull(result);
    }

    [Fact]
    public void QueryBuilder_IQueryBuilder_Without_ReturnsIQueryBuilder()
    {
        using var world = new World();

        IQueryBuilder query = world.Query<Position>();
        IQueryBuilder result = query.Without<Velocity>();

        Assert.NotNull(result);
    }

    [Fact]
    public void QueryBuilder_IQueryBuilder_WithTag_ReturnsIQueryBuilder()
    {
        using var world = new World();

        IQueryBuilder query = world.Query<Position>();
        IQueryBuilder result = query.WithTag("test");

        Assert.NotNull(result);
    }

    [Fact]
    public void QueryBuilder_IQueryBuilder_WithoutTag_ReturnsIQueryBuilder()
    {
        using var world = new World();

        IQueryBuilder query = world.Query<Position>();
        IQueryBuilder result = query.WithoutTag("test");

        Assert.NotNull(result);
    }

    [Fact]
    public void QueryBuilder_IQueryBuilder_Count_Works()
    {
        using var world = new World();

        world.Spawn().With(new Position()).Build();
        world.Spawn().With(new Position()).Build();

        IQueryBuilder query = world.Query<Position>();
        int count = query.Count();

        Assert.Equal(2, count);
    }

    #endregion

    #region QueryDescription Coverage

    [Fact]
    public void QueryDescription_HasStringTagFilters_False_WhenNoTags()
    {
        var description = new QueryDescription();
        description.AddWrite<Position>();

        Assert.False(description.HasStringTagFilters);
    }

    [Fact]
    public void QueryDescription_HasStringTagFilters_True_WhenWithTag()
    {
        var description = new QueryDescription();
        description.AddWrite<Position>();
        description.AddWithStringTag("test");

        Assert.True(description.HasStringTagFilters);
    }

    [Fact]
    public void QueryDescription_HasStringTagFilters_True_WhenWithoutTag()
    {
        var description = new QueryDescription();
        description.AddWrite<Position>();
        description.AddWithoutStringTag("test");

        Assert.True(description.HasStringTagFilters);
    }

    [Fact]
    public void QueryDescription_HasStringTagFilters_True_WhenBothTags()
    {
        var description = new QueryDescription();
        description.AddWrite<Position>();
        description.AddWithStringTag("include");
        description.AddWithoutStringTag("exclude");

        Assert.True(description.HasStringTagFilters);
    }

    #endregion
}
