namespace KeenEyes.Tests;

/// <summary>
/// Tests for the StatisticsManager.
/// </summary>
public class StatisticsManagerTests
{
#pragma warning disable CS0649 // Field is never assigned to
    [Component]
    private partial struct Position : IComponent
    {
        public float X;
        public float Y;
    }

    [Component]
    private partial struct Velocity : IComponent
    {
        public float X;
        public float Y;
    }

    [Component]
    private partial struct Health : IComponent
    {
        public int Current;
        public int Max;
    }
#pragma warning restore CS0649

    #region GetMemoryStats Tests

    [Fact]
    public void GetMemoryStats_WithNoEntities_ReturnsZeroStats()
    {
        using var world = new World();

        var stats = world.GetMemoryStats();

        Assert.Equal(0, stats.EntitiesAllocated);
        Assert.Equal(0, stats.EntitiesActive);
        Assert.Equal(0, stats.EntitiesRecycled);
        Assert.Equal(0, stats.EntityRecycleCount);
        Assert.Equal(0, stats.ArchetypeCount);
        Assert.Equal(0, stats.ComponentTypeCount);
        Assert.Equal(0, stats.EstimatedComponentBytes);
    }

    [Fact]
    public void GetMemoryStats_WithEntities_ReturnsCorrectEntityCount()
    {
        using var world = new World();

        world.Spawn().With(new Position()).Build();
        world.Spawn().With(new Position()).Build();
        world.Spawn().With(new Velocity()).Build();

        var stats = world.GetMemoryStats();

        Assert.Equal(3, stats.EntitiesAllocated);
        Assert.Equal(3, stats.EntitiesActive);
        Assert.Equal(0, stats.EntitiesRecycled);
    }

    [Fact]
    public void GetMemoryStats_WithRecycledEntities_ReturnsCorrectRecycleCount()
    {
        using var world = new World();

        var entity1 = world.Spawn().With(new Position()).Build();
        var entity2 = world.Spawn().With(new Position()).Build();

        world.Despawn(entity1);

        var stats = world.GetMemoryStats();

        Assert.Equal(2, stats.EntitiesAllocated);
        Assert.Equal(1, stats.EntitiesActive);
        Assert.Equal(1, stats.EntitiesRecycled);

        // Recycle the entity
        world.Spawn().With(new Position()).Build();

        stats = world.GetMemoryStats();
        Assert.Equal(1, stats.EntityRecycleCount);
    }

    [Fact]
    public void GetMemoryStats_WithMultipleArchetypes_ReturnsCorrectArchetypeCount()
    {
        using var world = new World();

        world.Spawn().With(new Position()).Build();
        world.Spawn().With(new Position()).With(new Velocity()).Build();
        world.Spawn().With(new Position()).With(new Velocity()).With(new Health()).Build();

        var stats = world.GetMemoryStats();

        Assert.Equal(3, stats.ArchetypeCount);
    }

    [Fact]
    public void GetMemoryStats_WithComponents_ReturnsCorrectComponentTypeCount()
    {
        using var world = new World();

        world.Spawn().With(new Position()).Build();
        world.Spawn().With(new Position()).With(new Velocity()).Build();

        var stats = world.GetMemoryStats();

        Assert.Equal(2, stats.ComponentTypeCount);
    }

    [Fact]
    public void GetMemoryStats_WithSystems_ReturnsCorrectSystemCount()
    {
        using var world = new World();

        world.AddSystem(new TestSystem());
        world.AddSystem(new TestSystem());

        var stats = world.GetMemoryStats();

        Assert.Equal(2, stats.SystemCount);
    }

    [Fact]
    public void GetMemoryStats_WithQueries_ReturnsCorrectQueryCacheStats()
    {
        using var world = new World();

        world.Spawn().With(new Position()).Build();
        world.Spawn().With(new Position()).With(new Velocity()).Build();

        // Execute queries to populate cache
        foreach (var _ in world.Query<Position>()) { }
        foreach (var _ in world.Query<Position, Velocity>()) { }

        var stats = world.GetMemoryStats();

        Assert.Equal(2, stats.CachedQueryCount);
        Assert.True(stats.QueryCacheHits >= 0);
        Assert.True(stats.QueryCacheMisses >= 0);
    }

    [Fact]
    public void GetMemoryStats_EstimatesComponentBytes_Correctly()
    {
        using var world = new World();

        // Create entities with known component sizes
        world.Spawn().With(new Position()).Build();
        world.Spawn().With(new Position()).Build();

        var stats = world.GetMemoryStats();

        // Position struct has 2 floats (8 bytes total)
        // 2 entities * 8 bytes = 16 bytes
        Assert.True(stats.EstimatedComponentBytes > 0);
    }

    [Fact]
    public void GetMemoryStats_MultipleCalls_ReturnsConsistentResults()
    {
        using var world = new World();

        world.Spawn().With(new Position()).Build();
        world.Spawn().With(new Velocity()).Build();

        var stats1 = world.GetMemoryStats();
        var stats2 = world.GetMemoryStats();

        Assert.Equal(stats1.EntitiesActive, stats2.EntitiesActive);
        Assert.Equal(stats1.ArchetypeCount, stats2.ArchetypeCount);
        Assert.Equal(stats1.ComponentTypeCount, stats2.ComponentTypeCount);
        Assert.Equal(stats1.EstimatedComponentBytes, stats2.EstimatedComponentBytes);
    }

    [Fact]
    public void GetMemoryStats_AfterDespawn_UpdatesStats()
    {
        using var world = new World();

        var entity1 = world.Spawn().With(new Position()).Build();
        var entity2 = world.Spawn().With(new Position()).Build();

        var statsBefore = world.GetMemoryStats();
        Assert.Equal(2, statsBefore.EntitiesActive);

        world.Despawn(entity1);

        var statsAfter = world.GetMemoryStats();
        Assert.Equal(1, statsAfter.EntitiesActive);
        Assert.Equal(1, statsAfter.EntitiesRecycled);
    }

    #endregion

    #region Helper System

    private sealed class TestSystem : SystemBase
    {
        public override void Update(float deltaTime)
        {
            // Empty test system
        }
    }

    #endregion
}
