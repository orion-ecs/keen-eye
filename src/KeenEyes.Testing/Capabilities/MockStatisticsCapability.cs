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
    /// Gets the number of times <see cref="GetMemoryStats"/> was called.
    /// </summary>
    public int GetStatsCallCount { get; private set; }

    /// <inheritdoc />
    public MemoryStats GetMemoryStats()
    {
        GetStatsCallCount++;
        return Stats;
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
    /// Resets the call count.
    /// </summary>
    public void Reset()
    {
        GetStatsCallCount = 0;
    }
}
