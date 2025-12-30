using KeenEyes.Capabilities;
using KeenEyes.Testing.Capabilities;

namespace KeenEyes.Testing.Tests.Capabilities;

public class MockStatisticsCapabilityTests
{
    [Fact]
    public void Stats_InitiallyDefault()
    {
        var capability = new MockStatisticsCapability();

        var stats = capability.Stats;

        Assert.Equal(0, stats.EntitiesActive);
        Assert.Equal(0, stats.EntitiesAllocated);
        Assert.Equal(0, stats.ArchetypeCount);
    }

    [Fact]
    public void Stats_CanBeSet()
    {
        var customStats = new MemoryStats
        {
            EntitiesActive = 100,
            EntitiesAllocated = 150,
            ArchetypeCount = 5
        };
        var capability = new MockStatisticsCapability { Stats = customStats };

        Assert.Equal(100, capability.Stats.EntitiesActive);
        Assert.Equal(150, capability.Stats.EntitiesAllocated);
        Assert.Equal(5, capability.Stats.ArchetypeCount);
    }

    [Fact]
    public void GetMemoryStats_ReturnsConfiguredStats()
    {
        var capability = new MockStatisticsCapability { Stats = new MemoryStats { EntitiesActive = 50 } };

        var stats = capability.GetMemoryStats();

        Assert.Equal(50, stats.EntitiesActive);
    }

    [Fact]
    public void GetMemoryStats_IncrementsCallCount()
    {
        var capability = new MockStatisticsCapability();

        capability.GetMemoryStats();
        capability.GetMemoryStats();
        capability.GetMemoryStats();

        Assert.Equal(3, capability.GetStatsCallCount);
    }

    [Fact]
    public void WithStats_ConfiguresAllProperties()
    {
        var capability = new MockStatisticsCapability();

        capability.WithStats(
            entitiesActive: 100,
            entitiesAllocated: 200,
            archetypeCount: 10,
            componentTypeCount: 25,
            systemCount: 5);

        var stats = capability.Stats;
        Assert.Equal(100, stats.EntitiesActive);
        Assert.Equal(200, stats.EntitiesAllocated);
        Assert.Equal(10, stats.ArchetypeCount);
        Assert.Equal(25, stats.ComponentTypeCount);
        Assert.Equal(5, stats.SystemCount);
    }

    [Fact]
    public void WithStats_ReturnsCapabilityForChaining()
    {
        var capability = new MockStatisticsCapability();

        var result = capability.WithStats(entitiesActive: 50);

        Assert.Same(capability, result);
    }

    [Fact]
    public void Reset_ClearsCallCount()
    {
        var capability = new MockStatisticsCapability();
        capability.GetMemoryStats();
        capability.GetMemoryStats();

        capability.Reset();

        Assert.Equal(0, capability.GetStatsCallCount);
    }
}
