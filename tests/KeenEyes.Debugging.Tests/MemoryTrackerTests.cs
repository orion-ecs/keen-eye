namespace KeenEyes.Debugging.Tests;

/// <summary>
/// Unit tests for the <see cref="MemoryTracker"/> class.
/// </summary>
public partial class MemoryTrackerTests
{
#pragma warning disable CS0649 // Field is never assigned to
    [Component]
    private partial struct TestComponent
    {
        public int Value;
    }

    [Component]
    private partial struct LargeComponent
    {
        public long A, B, C, D, E, F, G, H;
    }
#pragma warning restore CS0649

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullWorld_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MemoryTracker(null!));
    }

    [Fact]
    public void Constructor_WithValidWorld_Succeeds()
    {
        // Arrange
        using var world = new World();

        // Act & Assert - should not throw
        var tracker = new MemoryTracker(world);
        Assert.NotNull(tracker);
    }

    #endregion

    #region GetMemoryStats Tests

    [Fact]
    public void GetMemoryStats_EmptyWorld_ReturnsZeroEntities()
    {
        // Arrange
        using var world = new World();
        var tracker = new MemoryTracker(world);

        // Act
        var stats = tracker.GetMemoryStats();

        // Assert
        Assert.Equal(0, stats.EntitiesActive);
        Assert.Equal(0, stats.EntitiesAllocated);
    }

    [Fact]
    public void GetMemoryStats_WithEntities_ReturnsCorrectCount()
    {
        // Arrange
        using var world = new World();
        var tracker = new MemoryTracker(world);

        var entity1 = world.Spawn().Build();
        var entity2 = world.Spawn().Build();
        var entity3 = world.Spawn().Build();

        // Act
        var stats = tracker.GetMemoryStats();

        // Assert
        Assert.Equal(3, stats.EntitiesActive);
        Assert.True(stats.EntitiesAllocated >= 3);
    }

    [Fact]
    public void GetMemoryStats_AfterDespawn_ReflectsRecycledEntities()
    {
        // Arrange
        using var world = new World();
        var tracker = new MemoryTracker(world);

        var entity1 = world.Spawn().Build();
        var entity2 = world.Spawn().Build();
        var entity3 = world.Spawn().Build();

        world.Despawn(entity2);

        // Act
        var stats = tracker.GetMemoryStats();

        // Assert
        Assert.Equal(2, stats.EntitiesActive);
        Assert.Equal(1, stats.EntitiesRecycled);
    }

    [Fact]
    public void GetMemoryStats_WithComponents_EstimatesMemory()
    {
        // Arrange
        using var world = new World();
        var tracker = new MemoryTracker(world);

        var entity = world.Spawn()
            .With(new TestComponent { Value = 42 })
            .Build();

        // Act
        var stats = tracker.GetMemoryStats();

        // Assert
        Assert.True(stats.EstimatedComponentBytes > 0);
    }

    [Fact]
    public void GetMemoryStats_TracksArchetypes()
    {
        // Arrange
        using var world = new World();
        var tracker = new MemoryTracker(world);

        // Create entities with different component combinations (different archetypes)
        var entity1 = world.Spawn()
            .With(new TestComponent { Value = 1 })
            .Build();

        var entity2 = world.Spawn()
            .With(new LargeComponent())
            .Build();

        var entity3 = world.Spawn()
            .With(new TestComponent { Value = 2 })
            .With(new LargeComponent())
            .Build();

        // Act
        var stats = tracker.GetMemoryStats();

        // Assert
        Assert.True(stats.ArchetypeCount >= 3); // At least 3 different archetypes
    }

    [Fact]
    public void GetMemoryStats_TracksComponentTypes()
    {
        // Arrange
        using var world = new World();
        var tracker = new MemoryTracker(world);

        var entity = world.Spawn()
            .With(new TestComponent { Value = 42 })
            .With(new LargeComponent())
            .Build();

        // Act
        var stats = tracker.GetMemoryStats();

        // Assert
        Assert.True(stats.ComponentTypeCount >= 2);
    }

    #endregion

    #region GetMemoryReport Tests

    [Fact]
    public void GetMemoryReport_ReturnsFormattedString()
    {
        // Arrange
        using var world = new World();
        var tracker = new MemoryTracker(world);

        // Act
        var report = tracker.GetMemoryReport();

        // Assert
        Assert.Contains("=== Memory Statistics ===", report);
        Assert.Contains("Entities:", report);
        Assert.Contains("Archetypes:", report);
        Assert.Contains("Component Types:", report);
    }

    [Fact]
    public void GetMemoryReport_WithEntities_IncludesEntityStats()
    {
        // Arrange
        using var world = new World();
        var tracker = new MemoryTracker(world);

        var entity1 = world.Spawn().Build();
        var entity2 = world.Spawn().Build();

        // Act
        var report = tracker.GetMemoryReport();

        // Assert
        Assert.Contains("2 active", report);
    }

    [Fact]
    public void GetMemoryReport_AfterRecycling_ShowsRecycleStats()
    {
        // Arrange
        using var world = new World();
        var tracker = new MemoryTracker(world);

        var entity1 = world.Spawn().Build();
        var entity2 = world.Spawn().Build();
        world.Despawn(entity1);

        var entity3 = world.Spawn().Build(); // Should reuse entity1

        // Act
        var report = tracker.GetMemoryReport();

        // Assert
        Assert.Contains("Entity Recycling:", report);
        Assert.Contains("reuses", report);
    }

    [Fact]
    public void GetMemoryReport_FormatsBytes_Correctly()
    {
        // Arrange
        using var world = new World();
        var tracker = new MemoryTracker(world);

        // Create some entities with components
        for (int i = 0; i < 100; i++)
        {
            world.Spawn()
                .With(new LargeComponent())
                .Build();
        }

        // Act
        var report = tracker.GetMemoryReport();

        // Assert
        Assert.Contains("Estimated Component Memory:", report);
        // Should format bytes in some unit (bytes, KB, MB, etc.)
        Assert.Matches(@"\d+(\.\d+)?\s+(bytes|KB|MB|GB)", report);
    }

    [Fact]
    public void GetMemoryReport_IncludesQueryCacheStats()
    {
        // Arrange
        using var world = new World();
        var tracker = new MemoryTracker(world);

        var entity = world.Spawn()
            .With(new TestComponent { Value = 42 })
            .Build();

        // Execute a query to populate cache
        var query = world.Query<TestComponent>();
        foreach (var e in query)
        {
            // Process entity
        }

        // Act
        var report = tracker.GetMemoryReport();

        // Assert
        Assert.Contains("Query Cache:", report);
        Assert.Contains("queries", report);
        Assert.Contains("hit rate", report);
    }

    #endregion

    #region GetArchetypeStats Tests

    [Fact]
    public void GetArchetypeStats_EmptyWorld_ReturnsEmptyList()
    {
        // Arrange
        using var world = new World();
        var tracker = new MemoryTracker(world);

        // Act
        var stats = tracker.GetArchetypeStats();

        // Assert
        Assert.Empty(stats);
    }

    [Fact]
    public void GetArchetypeStats_WithEntities_ReturnsArchetypeInfo()
    {
        // Arrange
        using var world = new World();
        var tracker = new MemoryTracker(world);

        world.Spawn()
            .With(new TestComponent { Value = 42 })
            .Build();

        // Act
        var stats = tracker.GetArchetypeStats();

        // Assert
        Assert.Single(stats);
        Assert.Equal(1, stats[0].EntityCount);
        Assert.Contains("TestComponent", stats[0].ComponentTypeNames);
    }

    [Fact]
    public void GetArchetypeStats_MultipleArchetypes_ReturnsSeparateEntries()
    {
        // Arrange
        using var world = new World();
        var tracker = new MemoryTracker(world);

        world.Spawn().With(new TestComponent { Value = 1 }).Build();
        world.Spawn().With(new LargeComponent()).Build();
        world.Spawn().With(new TestComponent { Value = 2 }).With(new LargeComponent()).Build();

        // Act
        var stats = tracker.GetArchetypeStats();

        // Assert
        Assert.Equal(3, stats.Count);
    }

    [Fact]
    public void GetArchetypeStats_SameArchetype_CountsEntitiesTogether()
    {
        // Arrange
        using var world = new World();
        var tracker = new MemoryTracker(world);

        world.Spawn().With(new TestComponent { Value = 1 }).Build();
        world.Spawn().With(new TestComponent { Value = 2 }).Build();
        world.Spawn().With(new TestComponent { Value = 3 }).Build();

        // Act
        var stats = tracker.GetArchetypeStats();

        // Assert
        Assert.Single(stats);
        Assert.Equal(3, stats[0].EntityCount);
    }

    [Fact]
    public void GetArchetypeStats_IncludesMemoryEstimate()
    {
        // Arrange
        using var world = new World();
        var tracker = new MemoryTracker(world);

        world.Spawn().With(new LargeComponent()).Build();

        // Act
        var stats = tracker.GetArchetypeStats();

        // Assert
        Assert.Single(stats);
        Assert.True(stats[0].EstimatedMemoryBytes > 0);
    }

    [Fact]
    public void GetArchetypeStats_IncludesUtilizationPercentage()
    {
        // Arrange
        using var world = new World();
        var tracker = new MemoryTracker(world);

        world.Spawn().With(new TestComponent { Value = 1 }).Build();

        // Act
        var stats = tracker.GetArchetypeStats();

        // Assert
        Assert.Single(stats);
        Assert.True(stats[0].UtilizationPercentage > 0);
        Assert.True(stats[0].UtilizationPercentage <= 100);
    }

    [Fact]
    public void GetArchetypeStats_ReturnsComponentTypeNames()
    {
        // Arrange
        using var world = new World();
        var tracker = new MemoryTracker(world);

        world.Spawn()
            .With(new TestComponent { Value = 1 })
            .With(new LargeComponent())
            .Build();

        // Act
        var stats = tracker.GetArchetypeStats();

        // Assert
        Assert.Single(stats);
        Assert.Equal(2, stats[0].ComponentTypeNames.Count);
        Assert.Contains("TestComponent", stats[0].ComponentTypeNames);
        Assert.Contains("LargeComponent", stats[0].ComponentTypeNames);
    }

    #endregion

    #region GetArchetypeReport Tests

    [Fact]
    public void GetArchetypeReport_EmptyWorld_ReturnsNoArchetypesMessage()
    {
        // Arrange
        using var world = new World();
        var tracker = new MemoryTracker(world);

        // Act
        var report = tracker.GetArchetypeReport();

        // Assert
        Assert.Contains("=== Archetype Statistics ===", report);
        Assert.Contains("Total Archetypes: 0", report);
        Assert.Contains("No archetypes in use.", report);
    }

    [Fact]
    public void GetArchetypeReport_WithEntities_ReturnsFormattedReport()
    {
        // Arrange
        using var world = new World();
        var tracker = new MemoryTracker(world);

        world.Spawn().With(new TestComponent { Value = 42 }).Build();

        // Act
        var report = tracker.GetArchetypeReport();

        // Assert
        Assert.Contains("=== Archetype Statistics ===", report);
        Assert.Contains("Total Archetypes: 1", report);
        Assert.Contains("ID", report);
        Assert.Contains("Entities", report);
        Assert.Contains("Chunks", report);
        Assert.Contains("Utilization", report);
        Assert.Contains("Memory", report);
        Assert.Contains("Components", report);
    }

    [Fact]
    public void GetArchetypeReport_IncludesArchetypeDetails()
    {
        // Arrange
        using var world = new World();
        var tracker = new MemoryTracker(world);

        world.Spawn().With(new TestComponent { Value = 1 }).Build();
        world.Spawn().With(new TestComponent { Value = 2 }).Build();

        // Act
        var report = tracker.GetArchetypeReport();

        // Assert
        // Should show the archetype with 2 entities
        Assert.Contains("TestComponent", report);
    }

    [Fact]
    public void GetArchetypeReport_TruncatesLongComponentLists()
    {
        // Arrange
        using var world = new World();
        var tracker = new MemoryTracker(world);

        // Create entity with multiple components for longer component string
        world.Spawn()
            .With(new TestComponent { Value = 1 })
            .With(new LargeComponent())
            .Build();

        // Act
        var report = tracker.GetArchetypeReport();

        // Assert - should contain component names (combined length < 40 for these)
        Assert.Contains("TestComponent", report);
        Assert.Contains("LargeComponent", report);
    }

    [Fact]
    public void GetArchetypeReport_SortsByEntityCountDescending()
    {
        // Arrange
        using var world = new World();
        var tracker = new MemoryTracker(world);

        // Create archetypes with different entity counts
        world.Spawn().With(new LargeComponent()).Build(); // 1 entity

        world.Spawn().With(new TestComponent { Value = 1 }).Build(); // 3 entities
        world.Spawn().With(new TestComponent { Value = 2 }).Build();
        world.Spawn().With(new TestComponent { Value = 3 }).Build();

        // Act
        var report = tracker.GetArchetypeReport();
        var lines = report.Split('\n');

        // Assert - Find the data lines (after header lines)
        var testCompLine = Array.FindIndex(lines, l => l.Contains("TestComponent"));
        var largeCompLine = Array.FindIndex(lines, l => l.Contains("LargeComponent"));

        // TestComponent archetype (3 entities) should come before LargeComponent (1 entity)
        Assert.True(testCompLine < largeCompLine, "Archetypes should be sorted by entity count descending");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void MemoryTracker_UpdatesWithWorldChanges()
    {
        // Arrange
        using var world = new World();
        var tracker = new MemoryTracker(world);

        var stats1 = tracker.GetMemoryStats();

        // Act - Add entities
        var entity1 = world.Spawn()
            .With(new TestComponent { Value = 1 })
            .Build();

        var stats2 = tracker.GetMemoryStats();

        var entity2 = world.Spawn()
            .With(new TestComponent { Value = 2 })
            .Build();

        var stats3 = tracker.GetMemoryStats();

        // Despawn
        world.Despawn(entity1);

        var stats4 = tracker.GetMemoryStats();

        // Assert
        Assert.Equal(0, stats1.EntitiesActive);
        Assert.Equal(1, stats2.EntitiesActive);
        Assert.Equal(2, stats3.EntitiesActive);
        Assert.Equal(1, stats4.EntitiesActive);
        Assert.Equal(1, stats4.EntitiesRecycled);
    }

    #endregion
}
