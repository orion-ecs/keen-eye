namespace KeenEyes.Tests;

/// <summary>
/// Additional comprehensive tests for ComponentDependencies class to increase coverage.
/// </summary>
public class ComponentDependenciesTests
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

    public struct Armor : IComponent
    {
        public int Value;
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithEmptyCollections_CreatesEmptyDependencies()
    {
        var deps = new ComponentDependencies([], []);

        Assert.Empty(deps.Reads);
        Assert.Empty(deps.Writes);
        Assert.Empty(deps.AllAccessed);
    }

    [Fact]
    public void Constructor_WithMultipleReads_StoresAll()
    {
        var deps = new ComponentDependencies(
            reads: [typeof(Position), typeof(Velocity), typeof(Health)],
            writes: []
        );

        Assert.Equal(3, deps.Reads.Count);
        Assert.Contains(typeof(Position), deps.Reads);
        Assert.Contains(typeof(Velocity), deps.Reads);
        Assert.Contains(typeof(Health), deps.Reads);
    }

    [Fact]
    public void Constructor_WithMultipleWrites_StoresAll()
    {
        var deps = new ComponentDependencies(
            reads: [],
            writes: [typeof(Position), typeof(Velocity), typeof(Health)]
        );

        Assert.Equal(3, deps.Writes.Count);
        Assert.Contains(typeof(Position), deps.Writes);
        Assert.Contains(typeof(Velocity), deps.Writes);
        Assert.Contains(typeof(Health), deps.Writes);
    }

    [Fact]
    public void Constructor_WithDuplicates_DeduplicatesTypes()
    {
        var deps = new ComponentDependencies(
            reads: [typeof(Position), typeof(Position), typeof(Position)],
            writes: [typeof(Velocity), typeof(Velocity)]
        );

        Assert.Single(deps.Reads);
        Assert.Single(deps.Writes);
    }

    #endregion

    #region AllAccessed Tests

    [Fact]
    public void AllAccessed_WithOnlyReads_ReturnsReads()
    {
        var deps = new ComponentDependencies(
            reads: [typeof(Position), typeof(Velocity)],
            writes: []
        );

        Assert.Equal(2, deps.AllAccessed.Count);
        Assert.Contains(typeof(Position), deps.AllAccessed);
        Assert.Contains(typeof(Velocity), deps.AllAccessed);
    }

    [Fact]
    public void AllAccessed_WithOnlyWrites_ReturnsWrites()
    {
        var deps = new ComponentDependencies(
            reads: [],
            writes: [typeof(Position), typeof(Velocity)]
        );

        Assert.Equal(2, deps.AllAccessed.Count);
        Assert.Contains(typeof(Position), deps.AllAccessed);
        Assert.Contains(typeof(Velocity), deps.AllAccessed);
    }

    [Fact]
    public void AllAccessed_WithSameTypeInBoth_ReturnsUnion()
    {
        var deps = new ComponentDependencies(
            reads: [typeof(Position), typeof(Health)],
            writes: [typeof(Position), typeof(Velocity)]
        );

        Assert.Equal(3, deps.AllAccessed.Count);
        Assert.Contains(typeof(Position), deps.AllAccessed);
        Assert.Contains(typeof(Velocity), deps.AllAccessed);
        Assert.Contains(typeof(Health), deps.AllAccessed);
    }

    #endregion

    #region ConflictsWith Tests

    [Fact]
    public void ConflictsWith_BothEmpty_NoConflict()
    {
        var deps1 = ComponentDependencies.Empty;
        var deps2 = ComponentDependencies.Empty;

        Assert.False(deps1.ConflictsWith(deps2));
    }

    [Fact]
    public void ConflictsWith_OnlyOneHasWrites_NoOverlap_NoConflict()
    {
        var deps1 = new ComponentDependencies(
            reads: [typeof(Position)],
            writes: []
        );
        var deps2 = new ComponentDependencies(
            reads: [],
            writes: [typeof(Velocity)]
        );

        Assert.False(deps1.ConflictsWith(deps2));
    }

    [Fact]
    public void ConflictsWith_MultipleWriteWriteConflicts_ReturnsTrue()
    {
        var deps1 = new ComponentDependencies(
            reads: [],
            writes: [typeof(Position), typeof(Velocity), typeof(Health)]
        );
        var deps2 = new ComponentDependencies(
            reads: [],
            writes: [typeof(Position), typeof(Velocity)]
        );

        Assert.True(deps1.ConflictsWith(deps2));
    }

    [Fact]
    public void ConflictsWith_MultipleReadWriteConflicts_ReturnsTrue()
    {
        var deps1 = new ComponentDependencies(
            reads: [typeof(Position), typeof(Velocity)],
            writes: []
        );
        var deps2 = new ComponentDependencies(
            reads: [],
            writes: [typeof(Position), typeof(Velocity), typeof(Health)]
        );

        Assert.True(deps1.ConflictsWith(deps2));
    }

    [Fact]
    public void ConflictsWith_PartialOverlap_StillConflicts()
    {
        var deps1 = new ComponentDependencies(
            reads: [typeof(Position)],
            writes: [typeof(Velocity)]
        );
        var deps2 = new ComponentDependencies(
            reads: [typeof(Health)],
            writes: [typeof(Position)]
        );

        Assert.True(deps1.ConflictsWith(deps2));
    }

    [Fact]
    public void ConflictsWith_IsSymmetric()
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
        Assert.True(deps2.ConflictsWith(deps1));
    }

    #endregion

    #region GetConflictingComponents Tests

    [Fact]
    public void GetConflictingComponents_NoConflicts_ReturnsEmpty()
    {
        var deps1 = new ComponentDependencies(
            reads: [typeof(Position)],
            writes: [typeof(Velocity)]
        );
        var deps2 = new ComponentDependencies(
            reads: [typeof(Health)],
            writes: [typeof(Damage)]
        );

        var conflicts = deps1.GetConflictingComponents(deps2);

        Assert.Empty(conflicts);
    }

    [Fact]
    public void GetConflictingComponents_WriteWriteOnly_ReturnsOverlap()
    {
        var deps1 = new ComponentDependencies(
            reads: [],
            writes: [typeof(Position), typeof(Velocity)]
        );
        var deps2 = new ComponentDependencies(
            reads: [],
            writes: [typeof(Velocity), typeof(Health)]
        );

        var conflicts = deps1.GetConflictingComponents(deps2);

        Assert.Single(conflicts);
        Assert.Contains(typeof(Velocity), conflicts);
    }

    [Fact]
    public void GetConflictingComponents_ReadWriteOnly_ReturnsOverlap()
    {
        var deps1 = new ComponentDependencies(
            reads: [typeof(Position), typeof(Velocity)],
            writes: []
        );
        var deps2 = new ComponentDependencies(
            reads: [],
            writes: [typeof(Velocity), typeof(Health)]
        );

        var conflicts = deps1.GetConflictingComponents(deps2);

        Assert.Single(conflicts);
        Assert.Contains(typeof(Velocity), conflicts);
    }

    [Fact]
    public void GetConflictingComponents_AllConflictTypes_ReturnsAll()
    {
        var deps1 = new ComponentDependencies(
            reads: [typeof(Position), typeof(Health)],
            writes: [typeof(Velocity)]
        );
        var deps2 = new ComponentDependencies(
            reads: [typeof(Velocity)],
            writes: [typeof(Position), typeof(Health)]
        );

        var conflicts = deps1.GetConflictingComponents(deps2);

        // Position: deps1 reads, deps2 writes
        // Health: deps1 reads, deps2 writes
        // Velocity: deps1 writes, deps2 reads
        Assert.Equal(3, conflicts.Count);
        Assert.Contains(typeof(Position), conflicts);
        Assert.Contains(typeof(Health), conflicts);
        Assert.Contains(typeof(Velocity), conflicts);
    }

    [Fact]
    public void GetConflictingComponents_BothEmpty_ReturnsEmpty()
    {
        var conflicts = ComponentDependencies.Empty.GetConflictingComponents(ComponentDependencies.Empty);

        Assert.Empty(conflicts);
    }

    #endregion

    #region Merge Tests

    [Fact]
    public void Merge_WithEmpty_ReturnsOriginal()
    {
        var deps = new ComponentDependencies(
            reads: [typeof(Position)],
            writes: [typeof(Velocity)]
        );

        var merged = deps.Merge(ComponentDependencies.Empty);

        Assert.Single(merged.Reads);
        Assert.Single(merged.Writes);
        Assert.Contains(typeof(Position), merged.Reads);
        Assert.Contains(typeof(Velocity), merged.Writes);
    }

    [Fact]
    public void Merge_EmptyWithNonEmpty_ReturnsOther()
    {
        var deps = new ComponentDependencies(
            reads: [typeof(Position)],
            writes: [typeof(Velocity)]
        );

        var merged = ComponentDependencies.Empty.Merge(deps);

        Assert.Single(merged.Reads);
        Assert.Single(merged.Writes);
        Assert.Contains(typeof(Position), merged.Reads);
        Assert.Contains(typeof(Velocity), merged.Writes);
    }

    [Fact]
    public void Merge_WithOverlap_Deduplicates()
    {
        var deps1 = new ComponentDependencies(
            reads: [typeof(Position), typeof(Health)],
            writes: [typeof(Velocity)]
        );
        var deps2 = new ComponentDependencies(
            reads: [typeof(Position), typeof(Damage)],
            writes: [typeof(Velocity), typeof(Armor)]
        );

        var merged = deps1.Merge(deps2);

        // Reads: Position, Health, Damage (Position deduplicated)
        Assert.Equal(3, merged.Reads.Count);
        Assert.Contains(typeof(Position), merged.Reads);
        Assert.Contains(typeof(Health), merged.Reads);
        Assert.Contains(typeof(Damage), merged.Reads);

        // Writes: Velocity, Armor (Velocity deduplicated)
        Assert.Equal(2, merged.Writes.Count);
        Assert.Contains(typeof(Velocity), merged.Writes);
        Assert.Contains(typeof(Armor), merged.Writes);
    }

    [Fact]
    public void Merge_DoesNotModifyOriginals()
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

        // Original deps1 unchanged
        Assert.Single(deps1.Reads);
        Assert.Single(deps1.Writes);

        // Original deps2 unchanged
        Assert.Single(deps2.Reads);
        Assert.Single(deps2.Writes);

        // Merged has both
        Assert.Equal(2, merged.Reads.Count);
        Assert.Equal(2, merged.Writes.Count);
    }

    #endregion

    #region FromQuery Tests

    [Fact]
    public void FromQuery_WithOnlyReads_ExtractsReads()
    {
        var description = new QueryDescription();
        description.AddRead<Position>();
        description.AddRead<Velocity>();

        var deps = ComponentDependencies.FromQuery(description);

        Assert.Equal(2, deps.Reads.Count);
        Assert.Empty(deps.Writes);
        Assert.Contains(typeof(Position), deps.Reads);
        Assert.Contains(typeof(Velocity), deps.Reads);
    }

    [Fact]
    public void FromQuery_WithOnlyWrites_ExtractsWrites()
    {
        var description = new QueryDescription();
        description.AddWrite<Position>();
        description.AddWrite<Velocity>();

        var deps = ComponentDependencies.FromQuery(description);

        Assert.Empty(deps.Reads);
        Assert.Equal(2, deps.Writes.Count);
        Assert.Contains(typeof(Position), deps.Writes);
        Assert.Contains(typeof(Velocity), deps.Writes);
    }

    [Fact]
    public void FromQuery_WithBothReadsAndWrites_ExtractsBoth()
    {
        var description = new QueryDescription();
        description.AddRead<Position>();
        description.AddRead<Health>();
        description.AddWrite<Velocity>();
        description.AddWrite<Damage>();

        var deps = ComponentDependencies.FromQuery(description);

        Assert.Equal(2, deps.Reads.Count);
        Assert.Equal(2, deps.Writes.Count);
    }

    [Fact]
    public void FromQuery_WithEmptyQuery_ReturnsEmpty()
    {
        var description = new QueryDescription();

        var deps = ComponentDependencies.FromQuery(description);

        Assert.Empty(deps.Reads);
        Assert.Empty(deps.Writes);
        Assert.Empty(deps.AllAccessed);
    }

    #endregion

    #region FromQueries Tests

    [Fact]
    public void FromQueries_WithEmptyCollection_ReturnsEmpty()
    {
        var deps = ComponentDependencies.FromQueries([]);

        Assert.Empty(deps.Reads);
        Assert.Empty(deps.Writes);
    }

    [Fact]
    public void FromQueries_WithSingleQuery_ExtractsCorrectly()
    {
        var desc = new QueryDescription();
        desc.AddRead<Position>();
        desc.AddWrite<Velocity>();

        var deps = ComponentDependencies.FromQueries([desc]);

        Assert.Single(deps.Reads);
        Assert.Single(deps.Writes);
        Assert.Contains(typeof(Position), deps.Reads);
        Assert.Contains(typeof(Velocity), deps.Writes);
    }

    [Fact]
    public void FromQueries_WithMultipleQueries_MergesAll()
    {
        var desc1 = new QueryDescription();
        desc1.AddRead<Position>();
        desc1.AddWrite<Velocity>();

        var desc2 = new QueryDescription();
        desc2.AddRead<Health>();
        desc2.AddWrite<Damage>();

        var desc3 = new QueryDescription();
        desc3.AddRead<Armor>();

        var deps = ComponentDependencies.FromQueries([desc1, desc2, desc3]);

        Assert.Equal(3, deps.Reads.Count);
        Assert.Equal(2, deps.Writes.Count);
        Assert.Contains(typeof(Position), deps.Reads);
        Assert.Contains(typeof(Health), deps.Reads);
        Assert.Contains(typeof(Armor), deps.Reads);
        Assert.Contains(typeof(Velocity), deps.Writes);
        Assert.Contains(typeof(Damage), deps.Writes);
    }

    [Fact]
    public void FromQueries_WithOverlappingQueries_Deduplicates()
    {
        var desc1 = new QueryDescription();
        desc1.AddRead<Position>();
        desc1.AddWrite<Velocity>();

        var desc2 = new QueryDescription();
        desc2.AddRead<Position>();
        desc2.AddWrite<Velocity>();

        var deps = ComponentDependencies.FromQueries([desc1, desc2]);

        Assert.Single(deps.Reads);
        Assert.Single(deps.Writes);
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_WithEmptyDependencies_ReturnsEmptyLists()
    {
        var deps = ComponentDependencies.Empty;

        var str = deps.ToString();

        Assert.Contains("Reads: []", str);
        Assert.Contains("Writes: []", str);
    }

    [Fact]
    public void ToString_WithOnlyReads_ShowsReads()
    {
        var deps = new ComponentDependencies(
            reads: [typeof(Position), typeof(Velocity)],
            writes: []
        );

        var str = deps.ToString();

        Assert.Contains("Position", str);
        Assert.Contains("Velocity", str);
        Assert.Contains("Writes: []", str);
    }

    [Fact]
    public void ToString_WithOnlyWrites_ShowsWrites()
    {
        var deps = new ComponentDependencies(
            reads: [],
            writes: [typeof(Position), typeof(Velocity)]
        );

        var str = deps.ToString();

        Assert.Contains("Reads: []", str);
        Assert.Contains("Position", str);
        Assert.Contains("Velocity", str);
    }

    [Fact]
    public void ToString_WithBoth_ShowsBoth()
    {
        var deps = new ComponentDependencies(
            reads: [typeof(Position)],
            writes: [typeof(Velocity)]
        );

        var str = deps.ToString();

        Assert.Contains("Reads:", str);
        Assert.Contains("Position", str);
        Assert.Contains("Writes:", str);
        Assert.Contains("Velocity", str);
    }

    #endregion

    #region Empty Static Tests

    [Fact]
    public void Empty_HasNoAccess()
    {
        Assert.Empty(ComponentDependencies.Empty.Reads);
        Assert.Empty(ComponentDependencies.Empty.Writes);
        Assert.Empty(ComponentDependencies.Empty.AllAccessed);
    }

    [Fact]
    public void Empty_DoesNotConflict()
    {
        var deps = new ComponentDependencies(
            reads: [typeof(Position)],
            writes: [typeof(Velocity)]
        );

        Assert.False(ComponentDependencies.Empty.ConflictsWith(deps));
        Assert.False(deps.ConflictsWith(ComponentDependencies.Empty));
    }

    #endregion
}
