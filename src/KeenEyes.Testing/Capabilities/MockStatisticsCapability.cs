using KeenEyes.Capabilities;

namespace KeenEyes.Testing.Capabilities;

/// <summary>
/// Mock implementation of <see cref="IStatisticsCapability"/> for testing.
/// </summary>
/// <remarks>
/// <para>
/// This mock allows configuring custom memory statistics for testing
/// without requiring a real World.
/// </para>
/// </remarks>
public sealed class MockStatisticsCapability : IStatisticsCapability
{
    /// <summary>
    /// Gets or sets the memory stats to return from <see cref="GetMemoryStats"/>.
    /// </summary>
    public MemoryStats Stats { get; set; } = new MemoryStats();

    /// <summary>
    /// Gets or sets the archetype statistics to return from <see cref="GetArchetypeStatistics"/>.
    /// </summary>
    public List<ArchetypeStatistics> ArchetypeStats { get; set; } = [];

    /// <summary>
    /// Gets the number of times <see cref="GetMemoryStats"/> was called.
    /// </summary>
    public int GetStatsCallCount { get; private set; }

    /// <summary>
    /// Gets the number of times <see cref="GetArchetypeStatistics"/> was called.
    /// </summary>
    public int GetArchetypeStatsCallCount { get; private set; }

    /// <inheritdoc />
    public MemoryStats GetMemoryStats()
    {
        GetStatsCallCount++;
        return Stats;
    }

    /// <inheritdoc />
    public IReadOnlyList<ArchetypeStatistics> GetArchetypeStatistics()
    {
        GetArchetypeStatsCallCount++;
        return ArchetypeStats;
    }

    /// <summary>
    /// Configures the mock with specific statistics.
    /// </summary>
    public MockStatisticsCapability WithStats(
        int entitiesActive = 0,
        int entitiesAllocated = 0,
        int archetypeCount = 0,
        int componentTypeCount = 0,
        int systemCount = 0)
    {
        Stats = new MemoryStats
        {
            EntitiesActive = entitiesActive,
            EntitiesAllocated = entitiesAllocated,
            ArchetypeCount = archetypeCount,
            ComponentTypeCount = componentTypeCount,
            SystemCount = systemCount
        };

        return this;
    }

    /// <summary>
    /// Adds an archetype statistic entry to the mock.
    /// </summary>
    public MockStatisticsCapability WithArchetype(
        int id,
        int entityCount,
        int chunkCount,
        int totalCapacity,
        IReadOnlyList<string> componentTypeNames,
        long estimatedMemoryBytes = 0)
    {
        ArchetypeStats.Add(new ArchetypeStatistics
        {
            Id = id,
            EntityCount = entityCount,
            ChunkCount = chunkCount,
            TotalCapacity = totalCapacity,
            ComponentTypeNames = componentTypeNames,
            EstimatedMemoryBytes = estimatedMemoryBytes
        });

        return this;
    }

    /// <summary>
    /// Resets the call counts.
    /// </summary>
    public void Reset()
    {
        GetStatsCallCount = 0;
        GetArchetypeStatsCallCount = 0;
    }
}
