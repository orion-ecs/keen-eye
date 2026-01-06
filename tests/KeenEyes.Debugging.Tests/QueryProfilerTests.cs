namespace KeenEyes.Debugging.Tests;

/// <summary>
/// Unit tests for the <see cref="QueryProfiler"/> class.
/// </summary>
[Collection("DebuggingTests")]
public partial class QueryProfilerTests
{
#pragma warning disable CS0649 // Field is never assigned to
    [Component]
    private partial struct TestComponent
    {
        public int Value;
    }

    [Component]
    private partial struct OtherComponent
    {
        public float X;
    }
#pragma warning restore CS0649

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullCapability_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new QueryProfiler(null!));
    }

    [Fact]
    public void Constructor_WithValidCapability_Succeeds()
    {
        // Arrange
        using var world = new World();

        // Act & Assert - should not throw
        var profiler = new QueryProfiler(world);
        Assert.NotNull(profiler);
    }

    #endregion

    #region BeginQuery/EndQuery Tests

    [Fact]
    public void BeginQuery_EndQuery_RecordsProfile()
    {
        // Arrange
        using var world = new World();
        var profiler = new QueryProfiler(world);

        // Act
        profiler.BeginQuery("TestQuery");
        Thread.Sleep(1); // Small delay to ensure measurable time
        profiler.EndQuery("TestQuery");

        // Assert
        var profile = profiler.GetQueryProfile("TestQuery");
        Assert.Equal("TestQuery", profile.Name);
        Assert.Equal(1, profile.CallCount);
        Assert.True(profile.TotalTime > TimeSpan.Zero);
        Assert.True(profile.AverageTime > TimeSpan.Zero);
    }

    [Fact]
    public void BeginQuery_EndQuery_WithEntityCount_TracksEntities()
    {
        // Arrange
        using var world = new World();
        var profiler = new QueryProfiler(world);

        // Act
        profiler.BeginQuery("TestQuery");
        profiler.EndQuery("TestQuery", entityCount: 100);

        // Assert
        var profile = profiler.GetQueryProfile("TestQuery");
        Assert.Equal(100, profile.TotalEntities);
        Assert.Equal(100, profile.AverageEntities);
        Assert.Equal(100, profile.MinEntities);
        Assert.Equal(100, profile.MaxEntities);
    }

    [Fact]
    public void BeginQuery_EndQuery_MultipleCalls_AccumulatesStats()
    {
        // Arrange
        using var world = new World();
        var profiler = new QueryProfiler(world);

        // Act
        for (int i = 0; i < 5; i++)
        {
            profiler.BeginQuery("TestQuery");
            Thread.Sleep(1);
            profiler.EndQuery("TestQuery", entityCount: (i + 1) * 10);
        }

        // Assert
        var profile = profiler.GetQueryProfile("TestQuery");
        Assert.Equal("TestQuery", profile.Name);
        Assert.Equal(5, profile.CallCount);
        Assert.True(profile.TotalTime > TimeSpan.Zero);
        Assert.True(profile.AverageTime > TimeSpan.Zero);
        Assert.Equal(10 + 20 + 30 + 40 + 50, profile.TotalEntities);
        Assert.Equal(10, profile.MinEntities);
        Assert.Equal(50, profile.MaxEntities);
    }

    [Fact]
    public void EndQuery_WithoutBeginQuery_DoesNotThrow()
    {
        // Arrange
        using var world = new World();
        var profiler = new QueryProfiler(world);

        // Act & Assert - should not throw
        profiler.EndQuery("NonExistentQuery");

        var profile = profiler.GetQueryProfile("NonExistentQuery");
        Assert.Equal(0, profile.CallCount);
    }

    [Fact]
    public void BeginQuery_Overwrite_ReplacesPreviousSample()
    {
        // Arrange
        using var world = new World();
        var profiler = new QueryProfiler(world);

        // Act
        profiler.BeginQuery("TestQuery");
        Thread.Sleep(1);
        profiler.BeginQuery("TestQuery"); // Overwrite previous BeginQuery
        Thread.Sleep(1);
        profiler.EndQuery("TestQuery");

        // Assert - should only record one sample (the second one)
        var profile = profiler.GetQueryProfile("TestQuery");
        Assert.Equal(1, profile.CallCount);
    }

    #endregion

    #region GetQueryProfile Tests

    [Fact]
    public void GetQueryProfile_NonExistentQuery_ReturnsEmptyProfile()
    {
        // Arrange
        using var world = new World();
        var profiler = new QueryProfiler(world);

        // Act
        var profile = profiler.GetQueryProfile("NonExistent");

        // Assert
        Assert.Equal("NonExistent", profile.Name);
        Assert.Equal(0, profile.CallCount);
        Assert.Equal(TimeSpan.Zero, profile.TotalTime);
        Assert.Equal(TimeSpan.Zero, profile.AverageTime);
        Assert.Equal(0, profile.TotalEntities);
    }

    [Fact]
    public void GetQueryProfile_ExistingQuery_ReturnsCorrectProfile()
    {
        // Arrange
        using var world = new World();
        var profiler = new QueryProfiler(world);
        profiler.BeginQuery("TestQuery");
        Thread.Sleep(1);
        profiler.EndQuery("TestQuery", entityCount: 50);

        // Act
        var profile = profiler.GetQueryProfile("TestQuery");

        // Assert
        Assert.Equal("TestQuery", profile.Name);
        Assert.Equal(1, profile.CallCount);
        Assert.True(profile.TotalTime > TimeSpan.Zero);
        Assert.Equal(50, profile.TotalEntities);
    }

    #endregion

    #region GetAllQueryProfiles Tests

    [Fact]
    public void GetAllQueryProfiles_NoSamples_ReturnsEmptyList()
    {
        // Arrange
        using var world = new World();
        var profiler = new QueryProfiler(world);

        // Act
        var profiles = profiler.GetAllQueryProfiles();

        // Assert
        Assert.Empty(profiles);
    }

    [Fact]
    public void GetAllQueryProfiles_MultipleQueries_ReturnsAllProfiles()
    {
        // Arrange
        using var world = new World();
        var profiler = new QueryProfiler(world);

        profiler.BeginQuery("Query1");
        profiler.EndQuery("Query1");

        profiler.BeginQuery("Query2");
        profiler.EndQuery("Query2");

        profiler.BeginQuery("Query3");
        profiler.EndQuery("Query3");

        // Act
        var profiles = profiler.GetAllQueryProfiles();

        // Assert
        Assert.Equal(3, profiles.Count);
        Assert.Contains(profiles, p => p.Name == "Query1");
        Assert.Contains(profiles, p => p.Name == "Query2");
        Assert.Contains(profiles, p => p.Name == "Query3");
    }

    #endregion

    #region GetSlowestQueries Tests

    [Fact]
    public void GetSlowestQueries_ReturnsSortedByAverageTime()
    {
        // Arrange
        using var world = new World();
        var profiler = new QueryProfiler(world);

        // Create queries with different execution times
        profiler.BeginQuery("FastQuery");
        profiler.EndQuery("FastQuery");

        profiler.BeginQuery("SlowQuery");
        Thread.Sleep(20);
        profiler.EndQuery("SlowQuery");

        profiler.BeginQuery("MediumQuery");
        Thread.Sleep(5);
        profiler.EndQuery("MediumQuery");

        // Act
        var slowest = profiler.GetSlowestQueries(10);

        // Assert
        Assert.Equal(3, slowest.Count);
        Assert.Equal("SlowQuery", slowest[0].Name);
        Assert.Equal("MediumQuery", slowest[1].Name);
        Assert.Equal("FastQuery", slowest[2].Name);
    }

    [Fact]
    public void GetSlowestQueries_RespectsCount()
    {
        // Arrange
        using var world = new World();
        var profiler = new QueryProfiler(world);

        for (int i = 0; i < 10; i++)
        {
            profiler.BeginQuery($"Query{i}");
            profiler.EndQuery($"Query{i}");
        }

        // Act
        var slowest = profiler.GetSlowestQueries(3);

        // Assert
        Assert.Equal(3, slowest.Count);
    }

    #endregion

    #region GetCacheStatistics Tests

    [Fact]
    public void GetCacheStatistics_ReturnsValidStats()
    {
        // Arrange
        using var world = new World();
        var profiler = new QueryProfiler(world);

        // Act
        var stats = profiler.GetCacheStatistics();

        // Assert
        Assert.True(stats.CachedQueryCount >= 0);
        Assert.True(stats.CacheHits >= 0);
        Assert.True(stats.CacheMisses >= 0);
    }

    [Fact]
    public void GetCacheStatistics_AfterQueryExecution_ReflectsCacheUsage()
    {
        // Arrange
        using var world = new World();
        var profiler = new QueryProfiler(world);

        // Create some entities
        for (int i = 0; i < 5; i++)
        {
            world.Spawn().With(new TestComponent { Value = i }).Build();
        }

        // Execute query multiple times to generate cache hits
        for (int i = 0; i < 5; i++)
        {
            foreach (var entity in world.Query<TestComponent>())
            {
                // Process
            }
        }

        // Act
        var stats = profiler.GetCacheStatistics();

        // Assert - at least one cached query should exist
        Assert.True(stats.CachedQueryCount >= 1);
    }

    #endregion

    #region GetQueryReport Tests

    [Fact]
    public void GetQueryReport_ReturnsFormattedString()
    {
        // Arrange
        using var world = new World();
        var profiler = new QueryProfiler(world);

        // Act
        var report = profiler.GetQueryReport();

        // Assert
        Assert.Contains("=== Query Profiling Report ===", report);
        Assert.Contains("Query Cache Statistics:", report);
    }

    [Fact]
    public void GetQueryReport_WithProfiles_IncludesTimingData()
    {
        // Arrange
        using var world = new World();
        var profiler = new QueryProfiler(world);

        profiler.BeginQuery("TestQuery");
        Thread.Sleep(1);
        profiler.EndQuery("TestQuery", entityCount: 100);

        // Act
        var report = profiler.GetQueryReport();

        // Assert
        Assert.Contains("Query Timing Profiles:", report);
        Assert.Contains("TestQuery", report);
    }

    [Fact]
    public void GetQueryReport_NoProfiles_ShowsInstructions()
    {
        // Arrange
        using var world = new World();
        var profiler = new QueryProfiler(world);

        // Act
        var report = profiler.GetQueryReport();

        // Assert
        Assert.Contains("No query timing profiles recorded", report);
        Assert.Contains("BeginQuery/EndQuery", report);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ClearsAllProfiles()
    {
        // Arrange
        using var world = new World();
        var profiler = new QueryProfiler(world);
        profiler.BeginQuery("Query1");
        profiler.EndQuery("Query1");
        profiler.BeginQuery("Query2");
        profiler.EndQuery("Query2");

        // Act
        profiler.Reset();

        // Assert
        var profiles = profiler.GetAllQueryProfiles();
        Assert.Empty(profiles);

        var profile = profiler.GetQueryProfile("Query1");
        Assert.Equal(0, profile.CallCount);
    }

    [Fact]
    public void Reset_ClearsActiveSamples()
    {
        // Arrange
        using var world = new World();
        var profiler = new QueryProfiler(world);
        profiler.BeginQuery("TestQuery");

        // Act
        profiler.Reset();
        profiler.EndQuery("TestQuery"); // Should do nothing since BeginQuery was cleared

        // Assert
        var profile = profiler.GetQueryProfile("TestQuery");
        Assert.Equal(0, profile.CallCount);
    }

    #endregion

    #region Min/Max Entity Tracking Tests

    [Fact]
    public void MinEntities_TracksSmallestCount()
    {
        // Arrange
        using var world = new World();
        var profiler = new QueryProfiler(world);

        // Act
        profiler.BeginQuery("TestQuery");
        profiler.EndQuery("TestQuery", entityCount: 100);

        profiler.BeginQuery("TestQuery");
        profiler.EndQuery("TestQuery", entityCount: 50);

        profiler.BeginQuery("TestQuery");
        profiler.EndQuery("TestQuery", entityCount: 200);

        // Assert
        var profile = profiler.GetQueryProfile("TestQuery");
        Assert.Equal(50, profile.MinEntities);
    }

    [Fact]
    public void MaxEntities_TracksLargestCount()
    {
        // Arrange
        using var world = new World();
        var profiler = new QueryProfiler(world);

        // Act
        profiler.BeginQuery("TestQuery");
        profiler.EndQuery("TestQuery", entityCount: 100);

        profiler.BeginQuery("TestQuery");
        profiler.EndQuery("TestQuery", entityCount: 50);

        profiler.BeginQuery("TestQuery");
        profiler.EndQuery("TestQuery", entityCount: 200);

        // Assert
        var profile = profiler.GetQueryProfile("TestQuery");
        Assert.Equal(200, profile.MaxEntities);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void QueryProfiler_IntegrationWithRealQueries()
    {
        // Arrange
        using var world = new World();
        var profiler = new QueryProfiler(world);

        // Create entities
        for (int i = 0; i < 100; i++)
        {
            world.Spawn().With(new TestComponent { Value = i }).Build();
        }

        for (int i = 0; i < 50; i++)
        {
            world.Spawn().With(new OtherComponent { X = i }).Build();
        }

        // Act - Profile actual queries
        profiler.BeginQuery("AllTestComponents");
        var count1 = 0;
        foreach (var entity in world.Query<TestComponent>())
        {
            count1++;
        }
        profiler.EndQuery("AllTestComponents", entityCount: count1);

        profiler.BeginQuery("AllOtherComponents");
        var count2 = 0;
        foreach (var entity in world.Query<OtherComponent>())
        {
            count2++;
        }
        profiler.EndQuery("AllOtherComponents", entityCount: count2);

        // Assert
        var profile1 = profiler.GetQueryProfile("AllTestComponents");
        var profile2 = profiler.GetQueryProfile("AllOtherComponents");

        Assert.Equal(1, profile1.CallCount);
        Assert.Equal(100, profile1.TotalEntities);
        Assert.True(profile1.TotalTime > TimeSpan.Zero);

        Assert.Equal(1, profile2.CallCount);
        Assert.Equal(50, profile2.TotalEntities);
        Assert.True(profile2.TotalTime > TimeSpan.Zero);
    }

    #endregion
}
