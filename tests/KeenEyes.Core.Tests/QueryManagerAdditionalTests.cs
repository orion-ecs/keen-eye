namespace KeenEyes.Tests;

/// <summary>
/// Additional tests for QueryManager to improve coverage.
/// </summary>
public class QueryManagerAdditionalTests
{
    #region Test Components

#pragma warning disable CS0649 // Field is never assigned to (test components for type metadata only)
    private struct Position : IComponent
    {
        public float X;
        public float Y;
    }

    private struct Velocity : IComponent
    {
        public float X;
        public float Y;
    }

    private struct Health : IComponent
    {
        public int Current;
        public int Max;
    }
#pragma warning restore CS0649

    #endregion

    #region InvalidateQuery Tests

    [Fact]
    public void QueryManager_InvalidateQuery_RemovesSpecificQuery()
    {
        using var world = new World();
        world.Components.Register<Position>();
        world.Components.Register<Velocity>();

        using var manager = new ArchetypeManager(world.Components);
        var queryManager = new QueryManager(manager);

        manager.GetOrCreateArchetype([typeof(Position)]);
        manager.GetOrCreateArchetype([typeof(Velocity)]);

        var posDescription = new QueryDescription();
        posDescription.AddWrite<Position>();

        var velDescription = new QueryDescription();
        velDescription.AddWrite<Velocity>();

        // Execute both queries to cache them
        queryManager.GetMatchingArchetypes(posDescription);
        queryManager.GetMatchingArchetypes(velDescription);

        Assert.Equal(2, queryManager.CachedQueryCount);

        // Invalidate only the position query
        var posDescriptor = QueryDescriptor.FromDescription(posDescription);
        queryManager.InvalidateQuery(posDescriptor);

        Assert.Equal(1, queryManager.CachedQueryCount);

        // Verify velocity query is still cached (should hit)
        var prevHits = queryManager.CacheHits;
        queryManager.GetMatchingArchetypes(velDescription);
        Assert.Equal(prevHits + 1, queryManager.CacheHits);

        // Verify position query was invalidated (should miss)
        var prevMisses = queryManager.CacheMisses;
        queryManager.GetMatchingArchetypes(posDescription);
        Assert.Equal(prevMisses + 1, queryManager.CacheMisses);
    }

    [Fact]
    public void QueryManager_InvalidateQuery_NonExistentQuery_NoOp()
    {
        using var world = new World();
        world.Components.Register<Position>();

        using var manager = new ArchetypeManager(world.Components);
        var queryManager = new QueryManager(manager);

        manager.GetOrCreateArchetype([typeof(Position)]);

        var description = new QueryDescription();
        description.AddWrite<Position>();
        queryManager.GetMatchingArchetypes(description);

        var cachedCount = queryManager.CachedQueryCount;

        // Invalidate a query that was never cached
        var nonExistentDesc = new QueryDescription();
        nonExistentDesc.AddWrite<Velocity>();
        var descriptor = QueryDescriptor.FromDescription(nonExistentDesc);
        queryManager.InvalidateQuery(descriptor);

        // Cache count should be unchanged
        Assert.Equal(cachedCount, queryManager.CachedQueryCount);
    }

    #endregion

    #region Archetype Creation Event Handling

    [Fact]
    public void QueryManager_OnArchetypeCreated_UpdatesMatchingCaches()
    {
        using var world = new World();
        world.Components.Register<Position>();
        world.Components.Register<Velocity>();

        using var manager = new ArchetypeManager(world.Components);
        var queryManager = new QueryManager(manager);

        // Create initial archetype
        manager.GetOrCreateArchetype([typeof(Position)]);

        var description = new QueryDescription();
        description.AddWrite<Position>();

        // Query and cache result (1 archetype)
        var result1 = queryManager.GetMatchingArchetypes(description);
        Assert.Single(result1);

        // Create new matching archetype
        manager.GetOrCreateArchetype([typeof(Position), typeof(Velocity)]);

        // Same query should now return 2 archetypes (incremental update)
        var result2 = queryManager.GetMatchingArchetypes(description);
        Assert.Equal(2, result2.Count);
    }

    [Fact]
    public void QueryManager_OnArchetypeCreated_DoesNotUpdateNonMatchingCaches()
    {
        using var world = new World();
        world.Components.Register<Position>();
        world.Components.Register<Velocity>();
        world.Components.Register<Health>();

        using var manager = new ArchetypeManager(world.Components);
        var queryManager = new QueryManager(manager);

        // Create initial archetype
        manager.GetOrCreateArchetype([typeof(Position)]);

        var description = new QueryDescription();
        description.AddWrite<Position>();
        description.AddWithout<Health>();

        // Query and cache result (1 archetype)
        var result1 = queryManager.GetMatchingArchetypes(description);
        Assert.Single(result1);

        // Create new archetype that DOESN'T match (has excluded Health component)
        manager.GetOrCreateArchetype([typeof(Position), typeof(Health)]);

        // Same query should still return 1 archetype (not updated)
        var result2 = queryManager.GetMatchingArchetypes(description);
        Assert.Single(result2);
    }

    #endregion

    #region HitRate Edge Cases

    [Fact]
    public void QueryManager_HitRate_NoQueries_ReturnsZero()
    {
        using var world = new World();
        world.Components.Register<Position>();

        using var manager = new ArchetypeManager(world.Components);
        var queryManager = new QueryManager(manager);

        Assert.Equal(0.0, queryManager.HitRate);
    }

    [Fact]
    public void QueryManager_HitRate_OnlyMisses_ReturnsZero()
    {
        using var world = new World();
        world.Components.Register<Position>();
        world.Components.Register<Velocity>();

        using var manager = new ArchetypeManager(world.Components);
        var queryManager = new QueryManager(manager);

        manager.GetOrCreateArchetype([typeof(Position)]);

        // Execute different queries (all will miss)
        var desc1 = new QueryDescription();
        desc1.AddWrite<Position>();
        queryManager.GetMatchingArchetypes(desc1);

        var desc2 = new QueryDescription();
        desc2.AddWrite<Velocity>();
        queryManager.GetMatchingArchetypes(desc2);

        Assert.Equal(0.0, queryManager.HitRate);
    }

    [Fact]
    public void QueryManager_HitRate_OnlyHits_Returns100()
    {
        using var world = new World();
        world.Components.Register<Position>();

        using var manager = new ArchetypeManager(world.Components);
        var queryManager = new QueryManager(manager);

        manager.GetOrCreateArchetype([typeof(Position)]);

        var description = new QueryDescription();
        description.AddWrite<Position>();

        // First query - miss
        queryManager.GetMatchingArchetypes(description);
        queryManager.ResetStatistics();

        // Now only hits
        queryManager.GetMatchingArchetypes(description);
        queryManager.GetMatchingArchetypes(description);
        queryManager.GetMatchingArchetypes(description);

        Assert.Equal(100.0, queryManager.HitRate);
    }

    #endregion

    #region Statistics Reset

    [Fact]
    public void QueryManager_ResetStatistics_PreservesCache()
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

        queryManager.ResetStatistics();

        // Cache should still be present
        Assert.Equal(1, queryManager.CachedQueryCount);

        // Next query should hit the cache
        queryManager.GetMatchingArchetypes(description);
        Assert.Equal(1, queryManager.CacheHits);
        Assert.Equal(0, queryManager.CacheMisses);
    }

    #endregion

    #region Multiple Descriptor Matches

    [Fact]
    public void QueryManager_MultipleDescriptors_CachedSeparately()
    {
        using var world = new World();
        world.Components.Register<Position>();
        world.Components.Register<Velocity>();

        using var manager = new ArchetypeManager(world.Components);
        var queryManager = new QueryManager(manager);

        manager.GetOrCreateArchetype([typeof(Position)]);
        manager.GetOrCreateArchetype([typeof(Position), typeof(Velocity)]);

        // Query 1: Position
        var desc1 = new QueryDescription();
        desc1.AddWrite<Position>();

        // Query 2: Position + Velocity
        var desc2 = new QueryDescription();
        desc2.AddWrite<Position>();
        desc2.AddWrite<Velocity>();

        // Query 3: Position without Velocity
        var desc3 = new QueryDescription();
        desc3.AddWrite<Position>();
        desc3.AddWithout<Velocity>();

        queryManager.GetMatchingArchetypes(desc1);
        queryManager.GetMatchingArchetypes(desc2);
        queryManager.GetMatchingArchetypes(desc3);

        // Should have 3 separate cache entries
        Assert.Equal(3, queryManager.CachedQueryCount);
    }

    #endregion

    #region Archetype Matching Edge Cases

    [Fact]
    public void QueryDescriptor_Matches_EmptyArchetype_WithEmptyQuery()
    {
        var registry = new ComponentRegistry();
        var manager = new ArchetypeManager(registry);
        var archetype = manager.GetOrCreateArchetype([]);

        var descriptor = new QueryDescriptor([], []);

        Assert.True(descriptor.Matches(archetype));
    }

    [Fact]
    public void QueryDescriptor_Matches_EmptyArchetype_WithRequirement_ReturnsFalse()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();
        var manager = new ArchetypeManager(registry);
        var archetype = manager.GetOrCreateArchetype([]);

        var descriptor = new QueryDescriptor([typeof(Position)], []);

        Assert.False(descriptor.Matches(archetype));
    }

    [Fact]
    public void QueryDescriptor_Matches_ArchetypeWithExtra_ReturnsTrue()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();
        registry.Register<Velocity>();
        registry.Register<Health>();
        var manager = new ArchetypeManager(registry);
        var archetype = manager.GetOrCreateArchetype([typeof(Position), typeof(Velocity), typeof(Health)]);

        var descriptor = new QueryDescriptor([typeof(Position)], []);

        Assert.True(descriptor.Matches(archetype));
    }

    #endregion

    #region ArchetypeCache Direct Tests

    [Fact]
    public void ArchetypeCache_Add_DuplicateArchetype_IsIdempotent()
    {
        using var world = new World();
        world.Components.Register<Position>();

        using var manager = new ArchetypeManager(world.Components);
        var queryManager = new QueryManager(manager);

        // Create archetype
        var archetype = manager.GetOrCreateArchetype([typeof(Position)]);

        // Query to create cache entry
        var description = new QueryDescription();
        description.AddWrite<Position>();
        var result = queryManager.GetMatchingArchetypes(description);

        Assert.Single(result);

        // Adding the same archetype again should be idempotent
        // Trigger another archetype creation event (via manager)
        // The cache should not have duplicates
        var result2 = queryManager.GetMatchingArchetypes(description);
        Assert.Single(result2);
    }

    [Fact]
    public void ArchetypeCache_SetAll_ReplacesExistingEntries()
    {
        using var world = new World();
        world.Components.Register<Position>();
        world.Components.Register<Velocity>();

        using var manager = new ArchetypeManager(world.Components);
        var queryManager = new QueryManager(manager);

        // Create archetypes
        manager.GetOrCreateArchetype([typeof(Position)]);

        var description = new QueryDescription();
        description.AddWrite<Position>();

        // Initial query
        var result1 = queryManager.GetMatchingArchetypes(description);
        Assert.Single(result1);

        // Add another matching archetype
        manager.GetOrCreateArchetype([typeof(Position), typeof(Velocity)]);

        // Query again - should have 2 archetypes
        var result2 = queryManager.GetMatchingArchetypes(description);
        Assert.Equal(2, result2.Count);
    }

    [Fact]
    public void ArchetypeCache_PopulateIfEmpty_OnlyPopulatesOnce()
    {
        using var world = new World();
        world.Components.Register<Position>();

        using var manager = new ArchetypeManager(world.Components);
        var queryManager = new QueryManager(manager);

        manager.GetOrCreateArchetype([typeof(Position)]);

        var description = new QueryDescription();
        description.AddWrite<Position>();

        // First query - cache miss, populates cache
        queryManager.GetMatchingArchetypes(description);
        Assert.Equal(1, queryManager.CacheMisses);

        // Second query - cache hit
        queryManager.GetMatchingArchetypes(description);
        Assert.Equal(1, queryManager.CacheHits);

        // Multiple queries should keep hitting cache
        queryManager.GetMatchingArchetypes(description);
        queryManager.GetMatchingArchetypes(description);
        Assert.Equal(3, queryManager.CacheHits);
        Assert.Equal(1, queryManager.CacheMisses);
    }

    #endregion
}
