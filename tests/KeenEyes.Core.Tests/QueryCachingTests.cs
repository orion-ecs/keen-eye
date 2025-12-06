namespace KeenEyes.Tests;

/// <summary>
/// Tests for the query caching system.
/// </summary>
public class QueryCachingTests
{
    #region Test Components

    private struct Position : IComponent
    {
        public float X;
        public float Y;

        public Position() { }
    }

    private struct Velocity : IComponent
    {
        public float X;
        public float Y;

        public Velocity() { }
    }

    private struct Health : IComponent
    {
        public int Current;
        public int Max;

        public Health() { }
    }

    private struct EnemyTag : ITagComponent;

    #endregion

    #region QueryDescriptor Tests

    [Fact]
    public void QueryDescriptor_SameTypes_Equal()
    {
        var desc1 = new QueryDescriptor([typeof(Position), typeof(Velocity)], []);
        var desc2 = new QueryDescriptor([typeof(Velocity), typeof(Position)], []);

        Assert.Equal(desc1, desc2);
        Assert.Equal(desc1.GetHashCode(), desc2.GetHashCode());
    }

    [Fact]
    public void QueryDescriptor_DifferentTypes_NotEqual()
    {
        var desc1 = new QueryDescriptor([typeof(Position)], []);
        var desc2 = new QueryDescriptor([typeof(Velocity)], []);

        Assert.NotEqual(desc1, desc2);
    }

    [Fact]
    public void QueryDescriptor_WithVsWithout_NotEqual()
    {
        var desc1 = new QueryDescriptor([typeof(Position)], []);
        var desc2 = new QueryDescriptor([], [typeof(Position)]);

        Assert.NotEqual(desc1, desc2);
    }

    [Fact]
    public void QueryDescriptor_SameWithAndWithout_Equal()
    {
        var desc1 = new QueryDescriptor([typeof(Position)], [typeof(Velocity)]);
        var desc2 = new QueryDescriptor([typeof(Position)], [typeof(Velocity)]);

        Assert.Equal(desc1, desc2);
    }

    [Fact]
    public void QueryDescriptor_FromDescription_CreatesCorrectDescriptor()
    {
        var description = new QueryDescription();
        description.AddWrite<Position>();
        description.AddWith<Velocity>();
        description.AddWithout<Health>();

        var descriptor = QueryDescriptor.FromDescription(description);

        Assert.Contains(typeof(Position), descriptor.With.ToList());
        Assert.Contains(typeof(Velocity), descriptor.With.ToList());
        Assert.Contains(typeof(Health), descriptor.Without.ToList());
    }

    [Fact]
    public void QueryDescriptor_ToString_ReturnsReadableFormat()
    {
        var desc = new QueryDescriptor([typeof(Position)], [typeof(Velocity)]);
        var str = desc.ToString();

        Assert.Contains("Position", str);
        Assert.Contains("Without", str);
        Assert.Contains("Velocity", str);
    }

    #endregion

    #region QueryManager Tests

    [Fact]
    public void QueryManager_CachesQueryResults()
    {
        using var world = new World();
        world.Components.Register<Position>();
        world.Components.Register<Velocity>();

        using var manager = new ArchetypeManager(world.Components);
        var queryManager = new QueryManager(manager);

        // Create an archetype
        manager.GetOrCreateArchetype([typeof(Position)]);

        var description = new QueryDescription();
        description.AddWrite<Position>();

        // First query - cache miss
        queryManager.GetMatchingArchetypes(description);
        Assert.Equal(1, queryManager.CacheMisses);
        Assert.Equal(0, queryManager.CacheHits);

        // Second query - cache hit
        queryManager.GetMatchingArchetypes(description);
        Assert.Equal(1, queryManager.CacheMisses);
        Assert.Equal(1, queryManager.CacheHits);
    }

    [Fact]
    public void QueryManager_IncrementallyUpdatesCache()
    {
        using var world = new World();
        world.Components.Register<Position>();
        world.Components.Register<Velocity>();

        using var manager = new ArchetypeManager(world.Components);
        var queryManager = new QueryManager(manager);

        var description = new QueryDescription();
        description.AddWrite<Position>();

        // Create first archetype
        manager.GetOrCreateArchetype([typeof(Position)]);

        // Query - caches result with 1 archetype
        var result1 = queryManager.GetMatchingArchetypes(description);
        Assert.Single(result1);

        // Create second matching archetype
        manager.GetOrCreateArchetype([typeof(Position), typeof(Velocity)]);

        // Same query - should include new archetype (incremental update)
        var result2 = queryManager.GetMatchingArchetypes(description);
        Assert.Equal(2, result2.Count);
    }

    [Fact]
    public void QueryManager_InvalidateCache_ClearsAll()
    {
        using var world = new World();
        world.Components.Register<Position>();

        using var manager = new ArchetypeManager(world.Components);
        var queryManager = new QueryManager(manager);

        manager.GetOrCreateArchetype([typeof(Position)]);

        var description = new QueryDescription();
        description.AddWrite<Position>();

        queryManager.GetMatchingArchetypes(description);
        Assert.Equal(1, queryManager.CachedQueryCount);

        queryManager.InvalidateCache();
        Assert.Equal(0, queryManager.CachedQueryCount);
    }

    [Fact]
    public void QueryManager_ResetStatistics_ClearsCounters()
    {
        using var world = new World();
        world.Components.Register<Position>();

        using var manager = new ArchetypeManager(world.Components);
        var queryManager = new QueryManager(manager);

        manager.GetOrCreateArchetype([typeof(Position)]);

        var description = new QueryDescription();
        description.AddWrite<Position>();

        queryManager.GetMatchingArchetypes(description);
        queryManager.GetMatchingArchetypes(description);

        Assert.True(queryManager.CacheHits > 0 || queryManager.CacheMisses > 0);

        queryManager.ResetStatistics();

        Assert.Equal(0, queryManager.CacheHits);
        Assert.Equal(0, queryManager.CacheMisses);
    }

    [Fact]
    public void QueryManager_HitRate_CalculatesCorrectly()
    {
        using var world = new World();
        world.Components.Register<Position>();

        using var manager = new ArchetypeManager(world.Components);
        var queryManager = new QueryManager(manager);

        manager.GetOrCreateArchetype([typeof(Position)]);

        var description = new QueryDescription();
        description.AddWrite<Position>();

        // 1 miss
        queryManager.GetMatchingArchetypes(description);

        // 3 hits
        queryManager.GetMatchingArchetypes(description);
        queryManager.GetMatchingArchetypes(description);
        queryManager.GetMatchingArchetypes(description);

        // Hit rate = 3 / 4 * 100 = 75%
        Assert.Equal(75.0, queryManager.HitRate);
    }

    #endregion

    #region World Query Caching Integration Tests

    [Fact]
    public void World_QueryCaching_ImprovesCacheHitRate()
    {
        using var world = new World();

        // Create some entities
        for (var i = 0; i < 10; i++)
        {
            world.Spawn().With(new Position { X = i }).Build();
        }

        // Run same query multiple times
        for (var i = 0; i < 10; i++)
        {
            foreach (var entity in world.Query<Position>())
            {
                _ = world.Get<Position>(entity);
            }
        }

        var stats = world.GetMemoryStats();

        // Should have good cache hit rate after multiple queries
        Assert.True(stats.QueryCacheHitRate > 50.0);
    }

    [Fact]
    public void World_DifferentQueries_CachedSeparately()
    {
        using var world = new World();

        world.Spawn().With(new Position()).Build();
        world.Spawn().With(new Velocity()).Build();
        world.Spawn().With(new Position()).With(new Velocity()).Build();

        // Run different queries
        var posQuery = world.Query<Position>().ToList();
        var velQuery = world.Query<Velocity>().ToList();
        var bothQuery = world.Query<Position, Velocity>().ToList();

        var stats = world.GetMemoryStats();

        // Should have at least 3 cached queries
        Assert.True(stats.CachedQueryCount >= 3);

        // Verify query results are correct
        Assert.Equal(2, posQuery.Count);
        Assert.Equal(2, velQuery.Count);
        Assert.Single(bothQuery);
    }

    [Fact]
    public void World_QueryWithFilter_CachedCorrectly()
    {
        using var world = new World();

        // Create entities with different component combinations
        world.Spawn().With(new Position()).Build();
        world.Spawn().With(new Position()).With(new Velocity()).Build();
        world.Spawn().With(new Position()).With(new Health()).Build();

        // Query Position without Velocity
        var filtered = world.Query<Position>().Without<Velocity>().ToList();

        Assert.Equal(2, filtered.Count);

        // Run same query again - should hit cache
        var filtered2 = world.Query<Position>().Without<Velocity>().ToList();
        Assert.Equal(2, filtered2.Count);

        var stats = world.GetMemoryStats();
        Assert.True(stats.QueryCacheHits >= 1);
    }

    [Fact]
    public void World_NewArchetype_UpdatesQueryCache()
    {
        using var world = new World();

        // Create entity with Position
        world.Spawn().With(new Position()).Build();

        // Run query (caches result)
        var result1 = world.Query<Position>().ToList();
        Assert.Single(result1);

        // Add component to entity creates new archetype
        world.Spawn().With(new Position()).With(new Velocity()).Build();

        // Query should now return both entities
        var result2 = world.Query<Position>().ToList();
        Assert.Equal(2, result2.Count);
    }

    #endregion
}
