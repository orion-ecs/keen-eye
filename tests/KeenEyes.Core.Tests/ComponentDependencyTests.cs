namespace KeenEyes.Tests;

/// <summary>
/// Tests for component dependency tracking and conflict detection.
/// </summary>
public class ComponentDependencyTests
{
    #region Test Components

    public struct Position : IComponent
    {
        public float X, Y;
    }

    public struct Velocity : IComponent
    {
        public float X, Y;
    }

    public struct Health : IComponent
    {
        public int Current, Max;
    }

    public struct Damage : IComponent
    {
        public int Amount;
    }

    #endregion

    #region ComponentDependencies Tests

    [Fact]
    public void Empty_HasNoReadsOrWrites()
    {
        var deps = ComponentDependencies.Empty;

        Assert.Empty(deps.Reads);
        Assert.Empty(deps.Writes);
        Assert.Empty(deps.AllAccessed);
    }

    [Fact]
    public void Constructor_StoresReadsAndWrites()
    {
        var deps = new ComponentDependencies(
            reads: [typeof(Position)],
            writes: [typeof(Velocity)]
        );

        Assert.Single(deps.Reads);
        Assert.Single(deps.Writes);
        Assert.Contains(typeof(Position), deps.Reads);
        Assert.Contains(typeof(Velocity), deps.Writes);
    }

    [Fact]
    public void AllAccessed_CombinesReadsAndWrites()
    {
        var deps = new ComponentDependencies(
            reads: [typeof(Position), typeof(Health)],
            writes: [typeof(Velocity), typeof(Damage)]
        );

        Assert.Equal(4, deps.AllAccessed.Count);
        Assert.Contains(typeof(Position), deps.AllAccessed);
        Assert.Contains(typeof(Health), deps.AllAccessed);
        Assert.Contains(typeof(Velocity), deps.AllAccessed);
        Assert.Contains(typeof(Damage), deps.AllAccessed);
    }

    [Fact]
    public void ConflictsWith_ReadRead_NoConflict()
    {
        var deps1 = new ComponentDependencies(
            reads: [typeof(Position)],
            writes: []
        );
        var deps2 = new ComponentDependencies(
            reads: [typeof(Position)],
            writes: []
        );

        Assert.False(deps1.ConflictsWith(deps2));
    }

    [Fact]
    public void ConflictsWith_WriteWrite_HasConflict()
    {
        var deps1 = new ComponentDependencies(
            reads: [],
            writes: [typeof(Position)]
        );
        var deps2 = new ComponentDependencies(
            reads: [],
            writes: [typeof(Position)]
        );

        Assert.True(deps1.ConflictsWith(deps2));
    }

    [Fact]
    public void ConflictsWith_ReadWrite_HasConflict()
    {
        var deps1 = new ComponentDependencies(
            reads: [typeof(Position)],
            writes: []
        );
        var deps2 = new ComponentDependencies(
            reads: [],
            writes: [typeof(Position)]
        );

        Assert.True(deps1.ConflictsWith(deps2));
    }

    [Fact]
    public void ConflictsWith_WriteRead_HasConflict()
    {
        var deps1 = new ComponentDependencies(
            reads: [],
            writes: [typeof(Position)]
        );
        var deps2 = new ComponentDependencies(
            reads: [typeof(Position)],
            writes: []
        );

        Assert.True(deps1.ConflictsWith(deps2));
    }

    [Fact]
    public void ConflictsWith_DifferentComponents_NoConflict()
    {
        var deps1 = new ComponentDependencies(
            reads: [typeof(Position)],
            writes: [typeof(Velocity)]
        );
        var deps2 = new ComponentDependencies(
            reads: [typeof(Health)],
            writes: [typeof(Damage)]
        );

        Assert.False(deps1.ConflictsWith(deps2));
    }

    [Fact]
    public void ConflictsWith_Empty_NoConflict()
    {
        var deps1 = new ComponentDependencies(
            reads: [typeof(Position)],
            writes: [typeof(Velocity)]
        );

        Assert.False(deps1.ConflictsWith(ComponentDependencies.Empty));
        Assert.False(ComponentDependencies.Empty.ConflictsWith(deps1));
    }

    [Fact]
    public void GetConflictingComponents_ReturnsConflicts()
    {
        var deps1 = new ComponentDependencies(
            reads: [typeof(Position)],
            writes: [typeof(Velocity), typeof(Health)]
        );
        var deps2 = new ComponentDependencies(
            reads: [typeof(Health)],
            writes: [typeof(Position), typeof(Velocity)]
        );

        var conflicts = deps1.GetConflictingComponents(deps2);

        Assert.Equal(3, conflicts.Count);
        Assert.Contains(typeof(Position), conflicts); // deps1 reads, deps2 writes
        Assert.Contains(typeof(Velocity), conflicts); // Both write
        Assert.Contains(typeof(Health), conflicts);   // deps1 writes, deps2 reads
    }

    [Fact]
    public void Merge_CombinesDependencies()
    {
        var deps1 = new ComponentDependencies(
            reads: [typeof(Position)],
            writes: [typeof(Velocity)]
        );
        var deps2 = new ComponentDependencies(
            reads: [typeof(Health)],
            writes: [typeof(Damage)]
        );

        var merged = deps1.Merge(deps2);

        Assert.Equal(2, merged.Reads.Count);
        Assert.Equal(2, merged.Writes.Count);
        Assert.Contains(typeof(Position), merged.Reads);
        Assert.Contains(typeof(Health), merged.Reads);
        Assert.Contains(typeof(Velocity), merged.Writes);
        Assert.Contains(typeof(Damage), merged.Writes);
    }

    [Fact]
    public void FromQuery_ExtractsDependencies()
    {
        var description = new QueryDescription();
        description.AddRead<Position>();
        description.AddWrite<Velocity>();

        var deps = ComponentDependencies.FromQuery(description);

        Assert.Single(deps.Reads);
        Assert.Single(deps.Writes);
        Assert.Contains(typeof(Position), deps.Reads);
        Assert.Contains(typeof(Velocity), deps.Writes);
    }

    [Fact]
    public void FromQueries_MergesAllQueries()
    {
        var desc1 = new QueryDescription();
        desc1.AddRead<Position>();

        var desc2 = new QueryDescription();
        desc2.AddWrite<Velocity>();
        desc2.AddWrite<Health>();

        var deps = ComponentDependencies.FromQueries([desc1, desc2]);

        Assert.Single(deps.Reads);
        Assert.Equal(2, deps.Writes.Count);
    }

    #endregion

    #region SystemDependencyBuilder Tests

    [Fact]
    public void Builder_Reads_AddsReadDependency()
    {
        var builder = new SystemDependencyBuilder();

        builder.Reads<Position>();
        var deps = builder.Build();

        Assert.Single(deps.Reads);
        Assert.Contains(typeof(Position), deps.Reads);
        Assert.Empty(deps.Writes);
    }

    [Fact]
    public void Builder_Writes_AddsWriteDependency()
    {
        var builder = new SystemDependencyBuilder();

        builder.Writes<Velocity>();
        var deps = builder.Build();

        Assert.Empty(deps.Reads);
        Assert.Single(deps.Writes);
        Assert.Contains(typeof(Velocity), deps.Writes);
    }

    [Fact]
    public void Builder_ReadWrites_AddsBothDependencies()
    {
        var builder = new SystemDependencyBuilder();

        builder.ReadWrites<Position>();
        var deps = builder.Build();

        Assert.Single(deps.Reads);
        Assert.Single(deps.Writes);
        Assert.Contains(typeof(Position), deps.Reads);
        Assert.Contains(typeof(Position), deps.Writes);
    }

    [Fact]
    public void Builder_UsesQuery_ExtractsFromQueryDescription()
    {
        var description = new QueryDescription();
        description.AddRead<Position>();
        description.AddWrite<Velocity>();

        var builder = new SystemDependencyBuilder();
        builder.UsesQuery(description);
        var deps = builder.Build();

        Assert.Contains(typeof(Position), deps.Reads);
        Assert.Contains(typeof(Velocity), deps.Writes);
    }

    [Fact]
    public void Builder_Chaining_AccumulatesDependencies()
    {
        var builder = new SystemDependencyBuilder();

        builder
            .Reads<Position>()
            .Reads<Health>()
            .Writes<Velocity>()
            .Writes<Damage>();

        var deps = builder.Build();

        Assert.Equal(2, deps.Reads.Count);
        Assert.Equal(2, deps.Writes.Count);
    }

    [Fact]
    public void Builder_Reset_ClearsDependencies()
    {
        var builder = new SystemDependencyBuilder();

        builder.Reads<Position>().Writes<Velocity>();
        builder.Reset();
        var deps = builder.Build();

        Assert.Empty(deps.Reads);
        Assert.Empty(deps.Writes);
    }

    #endregion

    #region SystemDependencyTracker Tests

    [Fact]
    public void Tracker_RegisterSystem_ExtractsDependencies()
    {
        var tracker = new SystemDependencyTracker();
        var system = new TestDependencyProviderSystem();

        tracker.RegisterSystem(system);

        var deps = tracker.GetDependencies<TestDependencyProviderSystem>();
        Assert.Contains(typeof(Position), deps.Reads);
        Assert.Contains(typeof(Velocity), deps.Writes);
    }

    [Fact]
    public void Tracker_RegisterNonProvider_UsesEmptyDependencies()
    {
        var tracker = new SystemDependencyTracker();
        var system = new TestNonProviderSystem();

        tracker.RegisterSystem(system);

        var deps = tracker.GetDependencies<TestNonProviderSystem>();
        Assert.Empty(deps.Reads);
        Assert.Empty(deps.Writes);
    }

    [Fact]
    public void Tracker_RegisterDependenciesExplicit_OverridesProvider()
    {
        var tracker = new SystemDependencyTracker();

        var customDeps = new ComponentDependencies(
            reads: [typeof(Health)],
            writes: [typeof(Damage)]
        );
        tracker.RegisterDependencies(typeof(TestDependencyProviderSystem), customDeps);

        var deps = tracker.GetDependencies<TestDependencyProviderSystem>();
        Assert.Contains(typeof(Health), deps.Reads);
        Assert.Contains(typeof(Damage), deps.Writes);
        Assert.DoesNotContain(typeof(Position), deps.Reads);
    }

    [Fact]
    public void Tracker_HasConflict_DetectsWriteWriteConflict()
    {
        var tracker = new SystemDependencyTracker();

        tracker.RegisterDependencies(typeof(TestNonProviderSystem), new ComponentDependencies(
            reads: [],
            writes: [typeof(Position)]
        ));
        tracker.RegisterDependencies(typeof(TestDependencyProviderSystem), new ComponentDependencies(
            reads: [],
            writes: [typeof(Position)]
        ));

        Assert.True(tracker.HasConflict(
            typeof(TestNonProviderSystem),
            typeof(TestDependencyProviderSystem)));
    }

    [Fact]
    public void Tracker_HasConflict_AllowsReadRead()
    {
        var tracker = new SystemDependencyTracker();

        tracker.RegisterDependencies(typeof(TestNonProviderSystem), new ComponentDependencies(
            reads: [typeof(Position)],
            writes: []
        ));
        tracker.RegisterDependencies(typeof(TestDependencyProviderSystem), new ComponentDependencies(
            reads: [typeof(Position)],
            writes: []
        ));

        Assert.False(tracker.HasConflict(
            typeof(TestNonProviderSystem),
            typeof(TestDependencyProviderSystem)));
    }

    [Fact]
    public void Tracker_CanRunInParallelWith_ChecksAllSystems()
    {
        var tracker = new SystemDependencyTracker();

        // System A writes Position
        tracker.RegisterDependencies(typeof(TestNonProviderSystem), new ComponentDependencies(
            reads: [],
            writes: [typeof(Position)]
        ));

        // System B reads Position - conflicts with A
        tracker.RegisterDependencies(typeof(TestDependencyProviderSystem), new ComponentDependencies(
            reads: [typeof(Position)],
            writes: []
        ));

        // System C writes Velocity - no conflict with A
        tracker.RegisterDependencies(typeof(TestSystem3), new ComponentDependencies(
            reads: [],
            writes: [typeof(Velocity)]
        ));

        Assert.False(tracker.CanRunInParallelWith(
            typeof(TestNonProviderSystem),
            [typeof(TestDependencyProviderSystem)]
        ));

        Assert.True(tracker.CanRunInParallelWith(
            typeof(TestNonProviderSystem),
            [typeof(TestSystem3)]
        ));
    }

    [Fact]
    public void Tracker_GetRegisteredSystems_ReturnsAllTypes()
    {
        var tracker = new SystemDependencyTracker();

        tracker.RegisterDependencies(typeof(TestNonProviderSystem), ComponentDependencies.Empty);
        tracker.RegisterDependencies(typeof(TestDependencyProviderSystem), ComponentDependencies.Empty);

        var systems = tracker.GetRegisteredSystems().ToList();

        Assert.Equal(2, systems.Count);
        Assert.Contains(typeof(TestNonProviderSystem), systems);
        Assert.Contains(typeof(TestDependencyProviderSystem), systems);
    }

    [Fact]
    public void Tracker_Unregister_RemovesSystem()
    {
        var tracker = new SystemDependencyTracker();

        tracker.RegisterDependencies(typeof(TestNonProviderSystem), ComponentDependencies.Empty);
        Assert.Equal(1, tracker.Count);

        tracker.Unregister(typeof(TestNonProviderSystem));
        Assert.Equal(0, tracker.Count);
    }

    [Fact]
    public void Tracker_Clear_RemovesAllSystems()
    {
        var tracker = new SystemDependencyTracker();

        tracker.RegisterDependencies(typeof(TestNonProviderSystem), ComponentDependencies.Empty);
        tracker.RegisterDependencies(typeof(TestDependencyProviderSystem), ComponentDependencies.Empty);

        tracker.Clear();

        Assert.Equal(0, tracker.Count);
    }

    #endregion

    #region Test Systems

    private sealed class TestDependencyProviderSystem : SystemBase, ISystemDependencyProvider
    {
        public void GetDependencies(ISystemDependencyBuilder builder)
        {
            builder
                .Reads<Position>()
                .Writes<Velocity>();
        }

        public override void Update(float deltaTime) { }
    }

    private sealed class TestNonProviderSystem : SystemBase
    {
        public override void Update(float deltaTime) { }
    }

    private sealed class TestSystem3 : SystemBase
    {
        public override void Update(float deltaTime) { }
    }

    #endregion
}
