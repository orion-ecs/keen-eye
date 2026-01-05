using KeenEyes.Serialization;

namespace KeenEyes.Tests;

/// <summary>
/// Tests for the MigrationGraph class.
/// </summary>
public class MigrationGraphTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_SetsComponentTypeName()
    {
        var graph = new MigrationGraph("TestComponent");

        Assert.Equal("TestComponent", graph.ComponentTypeName);
    }

    [Fact]
    public void Constructor_DefaultsVersionToOne()
    {
        var graph = new MigrationGraph("TestComponent");

        Assert.Equal(1, graph.CurrentVersion);
    }

    [Fact]
    public void Constructor_WithVersion_SetsCurrentVersion()
    {
        var graph = new MigrationGraph("TestComponent", 5);

        Assert.Equal(5, graph.CurrentVersion);
    }

    #endregion

    #region AddEdge Tests

    [Fact]
    public void AddEdge_IncreasesEdgeCount()
    {
        var graph = new MigrationGraph("TestComponent", 3);

        graph.AddEdge(1, 2);

        Assert.Equal(1, graph.EdgeCount);
    }

    [Fact]
    public void AddEdge_MultipleEdges_TracksAllEdges()
    {
        var graph = new MigrationGraph("TestComponent", 4);

        graph.AddEdge(1, 2);
        graph.AddEdge(2, 3);
        graph.AddEdge(3, 4);

        Assert.Equal(3, graph.EdgeCount);
    }

    [Fact]
    public void AddEdge_DuplicateEdge_DoesNotIncrementCount()
    {
        var graph = new MigrationGraph("TestComponent", 3);

        graph.AddEdge(1, 2);
        graph.AddEdge(1, 2); // Duplicate

        Assert.Equal(1, graph.EdgeCount);
    }

    [Fact]
    public void AddEdge_WithInvalidDirection_ThrowsArgumentException()
    {
        var graph = new MigrationGraph("TestComponent", 3);

        Assert.Throws<ArgumentException>(() => graph.AddEdge(2, 1));
        Assert.Throws<ArgumentException>(() => graph.AddEdge(2, 2));
    }

    #endregion

    #region HasPath Tests

    [Fact]
    public void HasPath_WithNoEdges_ReturnsFalse()
    {
        var graph = new MigrationGraph("TestComponent", 3);

        Assert.False(graph.HasPath(1, 3));
    }

    [Fact]
    public void HasPath_WithSingleEdge_ReturnsTrue()
    {
        var graph = new MigrationGraph("TestComponent", 2);

        graph.AddEdge(1, 2);

        Assert.True(graph.HasPath(1, 2));
    }

    [Fact]
    public void HasPath_WithCompleteChain_ReturnsTrue()
    {
        var graph = new MigrationGraph("TestComponent", 4);

        graph.AddEdge(1, 2);
        graph.AddEdge(2, 3);
        graph.AddEdge(3, 4);

        Assert.True(graph.HasPath(1, 4));
    }

    [Fact]
    public void HasPath_WithGap_ReturnsFalse()
    {
        var graph = new MigrationGraph("TestComponent", 4);

        graph.AddEdge(1, 2);
        // Missing 2 -> 3
        graph.AddEdge(3, 4);

        Assert.False(graph.HasPath(1, 4));
    }

    [Fact]
    public void HasPath_FromMiddleOfChain_ReturnsTrue()
    {
        var graph = new MigrationGraph("TestComponent", 5);

        graph.AddEdge(1, 2);
        graph.AddEdge(2, 3);
        graph.AddEdge(3, 4);
        graph.AddEdge(4, 5);

        Assert.True(graph.HasPath(2, 4));
    }

    [Fact]
    public void HasPath_EqualVersions_ReturnsFalse()
    {
        var graph = new MigrationGraph("TestComponent", 3);

        graph.AddEdge(1, 2);
        graph.AddEdge(2, 3);

        Assert.False(graph.HasPath(2, 2));
    }

    [Fact]
    public void HasPath_ReverseDirection_ReturnsFalse()
    {
        var graph = new MigrationGraph("TestComponent", 3);

        graph.AddEdge(1, 2);
        graph.AddEdge(2, 3);

        Assert.False(graph.HasPath(3, 1));
    }

    [Fact]
    public void HasPath_ResultIsCached()
    {
        var graph = new MigrationGraph("TestComponent", 3);

        graph.AddEdge(1, 2);
        graph.AddEdge(2, 3);

        // First call
        Assert.True(graph.HasPath(1, 3));

        // Second call should return cached result
        Assert.True(graph.HasPath(1, 3));
    }

    #endregion

    #region GetMigrationChain Tests

    [Fact]
    public void GetMigrationChain_WithNoEdges_ReturnsEmpty()
    {
        var graph = new MigrationGraph("TestComponent", 3);

        var chain = graph.GetMigrationChain(1, 3);

        Assert.Empty(chain);
    }

    [Fact]
    public void GetMigrationChain_WithSingleStep_ReturnsSingleStep()
    {
        var graph = new MigrationGraph("TestComponent", 2);

        graph.AddEdge(1, 2);

        var chain = graph.GetMigrationChain(1, 2);

        Assert.Single(chain);
        Assert.Equal(new MigrationStep(1, 2), chain[0]);
    }

    [Fact]
    public void GetMigrationChain_WithMultipleSteps_ReturnsAllSteps()
    {
        var graph = new MigrationGraph("TestComponent", 4);

        graph.AddEdge(1, 2);
        graph.AddEdge(2, 3);
        graph.AddEdge(3, 4);

        var chain = graph.GetMigrationChain(1, 4);

        Assert.Equal(3, chain.Count);
        Assert.Equal(new MigrationStep(1, 2), chain[0]);
        Assert.Equal(new MigrationStep(2, 3), chain[1]);
        Assert.Equal(new MigrationStep(3, 4), chain[2]);
    }

    [Fact]
    public void GetMigrationChain_WithGap_ReturnsEmpty()
    {
        var graph = new MigrationGraph("TestComponent", 4);

        graph.AddEdge(1, 2);
        // Missing 2 -> 3
        graph.AddEdge(3, 4);

        var chain = graph.GetMigrationChain(1, 4);

        Assert.Empty(chain);
    }

    [Fact]
    public void GetMigrationChain_EqualVersions_ReturnsEmpty()
    {
        var graph = new MigrationGraph("TestComponent", 3);

        graph.AddEdge(1, 2);

        var chain = graph.GetMigrationChain(2, 2);

        Assert.Empty(chain);
    }

    #endregion

    #region FindGaps Tests

    [Fact]
    public void FindGaps_WithCompleteChain_ReturnsEmpty()
    {
        var graph = new MigrationGraph("TestComponent", 4);

        graph.AddEdge(1, 2);
        graph.AddEdge(2, 3);
        graph.AddEdge(3, 4);

        var gaps = graph.FindGaps();

        Assert.Empty(gaps);
    }

    [Fact]
    public void FindGaps_WithMissingMigration_ReturnsGapVersion()
    {
        var graph = new MigrationGraph("TestComponent", 4);

        graph.AddEdge(1, 2);
        // Missing 2 -> 3
        graph.AddEdge(3, 4);

        var gaps = graph.FindGaps();

        Assert.Single(gaps);
        Assert.Equal(2, gaps[0]);
    }

    [Fact]
    public void FindGaps_WithMultipleMissingMigrations_ReturnsAllGaps()
    {
        var graph = new MigrationGraph("TestComponent", 5);

        graph.AddEdge(1, 2);
        // Missing 2 -> 3
        // Missing 3 -> 4
        graph.AddEdge(4, 5);

        var gaps = graph.FindGaps();

        Assert.Equal(2, gaps.Count);
        Assert.Contains(2, gaps);
        Assert.Contains(3, gaps);
    }

    [Fact]
    public void FindGaps_WithNoEdges_ReturnsAllVersions()
    {
        var graph = new MigrationGraph("TestComponent", 4);

        var gaps = graph.FindGaps();

        Assert.Equal(3, gaps.Count);
        Assert.Contains(1, gaps);
        Assert.Contains(2, gaps);
        Assert.Contains(3, gaps);
    }

    #endregion

    #region HasCycle Tests

    [Fact]
    public void HasCycle_WithNoEdges_ReturnsFalse()
    {
        var graph = new MigrationGraph("TestComponent", 3);

        Assert.False(graph.HasCycle());
    }

    [Fact]
    public void HasCycle_WithLinearChain_ReturnsFalse()
    {
        var graph = new MigrationGraph("TestComponent", 4);

        graph.AddEdge(1, 2);
        graph.AddEdge(2, 3);
        graph.AddEdge(3, 4);

        Assert.False(graph.HasCycle());
    }

    // Note: Cycles are structurally impossible with version-incrementing migrations.
    // The AddEdge method enforces fromVersion < toVersion, preventing cycles.

    #endregion

    #region SetCurrentVersion Tests

    [Fact]
    public void SetCurrentVersion_UpdatesVersion()
    {
        var graph = new MigrationGraph("TestComponent", 3);

        graph.SetCurrentVersion(5);

        Assert.Equal(5, graph.CurrentVersion);
    }

    [Fact]
    public void SetCurrentVersion_ClearsCaches()
    {
        var graph = new MigrationGraph("TestComponent", 3);

        graph.AddEdge(1, 2);
        graph.AddEdge(2, 3);

        // Cache the path check
        Assert.True(graph.HasPath(1, 3));

        // Change version - should clear cache
        graph.SetCurrentVersion(4);

        // Now gaps should include 3
        var gaps = graph.FindGaps();
        Assert.Contains(3, gaps);
    }

    #endregion

    #region ClearCache Tests

    [Fact]
    public void ClearCache_ClearsPathCache()
    {
        var graph = new MigrationGraph("TestComponent", 3);

        graph.AddEdge(1, 2);
        graph.AddEdge(2, 3);

        // Cache the result
        Assert.True(graph.HasPath(1, 3));

        // Clear cache
        graph.ClearCache();

        // Should still work (recalculates)
        Assert.True(graph.HasPath(1, 3));
    }

    #endregion

    #region SourceVersions Tests

    [Fact]
    public void SourceVersions_WithNoEdges_ReturnsEmpty()
    {
        var graph = new MigrationGraph("TestComponent", 3);

        Assert.Empty(graph.SourceVersions);
    }

    [Fact]
    public void SourceVersions_ReturnsOrderedVersions()
    {
        var graph = new MigrationGraph("TestComponent", 4);

        graph.AddEdge(3, 4);
        graph.AddEdge(1, 2);
        graph.AddEdge(2, 3);

        var versions = graph.SourceVersions.ToList();

        Assert.Equal([1, 2, 3], versions);
    }

    #endregion

    #region ToDiagnosticString Tests

    [Fact]
    public void ToDiagnosticString_IncludesComponentName()
    {
        var graph = new MigrationGraph("TestComponent", 3);

        var result = graph.ToDiagnosticString();

        Assert.Contains("TestComponent", result);
    }

    [Fact]
    public void ToDiagnosticString_IncludesCurrentVersion()
    {
        var graph = new MigrationGraph("TestComponent", 5);

        var result = graph.ToDiagnosticString();

        Assert.Contains("Current Version: 5", result);
    }

    [Fact]
    public void ToDiagnosticString_IncludesEdgeCount()
    {
        var graph = new MigrationGraph("TestComponent", 3);
        graph.AddEdge(1, 2);
        graph.AddEdge(2, 3);

        var result = graph.ToDiagnosticString();

        Assert.Contains("Edges: 2", result);
    }

    [Fact]
    public void ToDiagnosticString_WithGaps_ListsGaps()
    {
        var graph = new MigrationGraph("TestComponent", 4);
        graph.AddEdge(1, 2);
        // Missing 2 -> 3
        graph.AddEdge(3, 4);

        var result = graph.ToDiagnosticString();

        Assert.Contains("Gaps", result);
        Assert.Contains("v2", result);
    }

    #endregion
}

/// <summary>
/// Tests for the MigrationStep record struct.
/// </summary>
public class MigrationStepTests
{
    [Fact]
    public void MigrationStep_ToString_FormatsCorrectly()
    {
        var step = new MigrationStep(1, 2);

        Assert.Equal("v1 → v2", step.ToString());
    }

    [Fact]
    public void MigrationStep_Equality_ComparesVersions()
    {
        var step1 = new MigrationStep(1, 2);
        var step2 = new MigrationStep(1, 2);
        var step3 = new MigrationStep(2, 3);

        Assert.Equal(step1, step2);
        Assert.NotEqual(step1, step3);
    }
}
