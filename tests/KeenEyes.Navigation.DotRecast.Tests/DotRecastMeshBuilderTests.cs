using System.Numerics;
using KeenEyes.Navigation.Abstractions;
using KeenEyes.Navigation.DotRecast;

namespace KeenEyes.Navigation.DotRecast.Tests;

/// <summary>
/// Tests for <see cref="DotRecastMeshBuilder"/> class.
/// </summary>
public class DotRecastMeshBuilderTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_DefaultConfig_CreatesBuilder()
    {
        var builder = new DotRecastMeshBuilder();

        Assert.NotNull(builder);
        Assert.Equal(NavMeshConfig.Default.CellSize, builder.Config.CellSize);
    }

    [Fact]
    public void Constructor_CustomConfig_UsesConfig()
    {
        var config = new NavMeshConfig { CellSize = 0.5f };
        var builder = new DotRecastMeshBuilder(config);

        Assert.Equal(0.5f, builder.Config.CellSize);
    }

    [Fact]
    public void Constructor_NullConfig_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => new DotRecastMeshBuilder(null!));
    }

    [Fact]
    public void Constructor_InvalidConfig_ThrowsArgument()
    {
        var invalidConfig = new NavMeshConfig { CellSize = 0 };

        Assert.Throws<ArgumentException>(() => new DotRecastMeshBuilder(invalidConfig));
    }

    #endregion

    #region Build Tests

    [Fact]
    public void Build_EmptyVertices_ThrowsArgument()
    {
        var builder = new DotRecastMeshBuilder();

        Assert.Throws<ArgumentException>(() =>
            builder.Build(ReadOnlySpan<float>.Empty, new int[] { 0, 1, 2 }));
    }

    [Fact]
    public void Build_EmptyIndices_ThrowsArgument()
    {
        var builder = new DotRecastMeshBuilder();
        var vertices = new float[] { 0, 0, 0, 1, 0, 0, 0, 0, 1 };

        Assert.Throws<ArgumentException>(() =>
            builder.Build(vertices, ReadOnlySpan<int>.Empty));
    }

    [Fact]
    public void Build_InvalidVertexCount_ThrowsArgument()
    {
        var builder = new DotRecastMeshBuilder();
        var vertices = new float[] { 0, 0 };  // Not a multiple of 3
        var indices = new int[] { 0 };

        Assert.Throws<ArgumentException>(() =>
            builder.Build(vertices, indices));
    }

    [Fact]
    public void Build_InvalidIndexCount_ThrowsArgument()
    {
        var builder = new DotRecastMeshBuilder();
        var vertices = new float[] { 0, 0, 0, 1, 0, 0, 0, 0, 1 };
        var indices = new int[] { 0, 1 };  // Not a multiple of 3

        Assert.Throws<ArgumentException>(() =>
            builder.Build(vertices, indices));
    }

    [Fact(Skip = "Requires proper 3D mesh geometry - see integration tests")]
    public void Build_SimpleQuad_ReturnsNavMesh()
    {
        var builder = TestHelper.CreateTestBuilder();

        // Simple quad on XZ plane (50x50 for reliable mesh generation)
        var vertices = new float[]
        {
            0, 0, 0,
            50, 0, 0,
            50, 0, 50,
            0, 0, 50
        };

        var indices = new int[]
        {
            0, 1, 2,
            0, 2, 3
        };

        var navMesh = builder.Build(vertices, indices);

        Assert.NotNull(navMesh);
        Assert.True(navMesh.PolygonCount > 0);
    }

    [Fact(Skip = "Requires proper 3D mesh geometry - see integration tests")]
    public void Build_Vector3Overload_ReturnsNavMesh()
    {
        var builder = TestHelper.CreateTestBuilder();

        var vertices = new Vector3[]
        {
            new(0, 0, 0),
            new(50, 0, 0),
            new(50, 0, 50),
            new(0, 0, 50)
        };

        var indices = new int[]
        {
            0, 1, 2,
            0, 2, 3
        };

        var navMesh = builder.Build(vertices, indices);

        Assert.NotNull(navMesh);
    }

    #endregion

    #region BuildBox Tests

    [Fact(Skip = "Requires proper 3D mesh geometry - see integration tests")]
    public void BuildBox_ReturnsNavMesh()
    {
        var builder = TestHelper.CreateTestBuilder();

        var navMesh = builder.BuildBox(
            new Vector3(0, 0, 0),
            new Vector3(50, 0, 50));

        Assert.NotNull(navMesh);
        Assert.True(navMesh.PolygonCount > 0);
    }

    [Fact(Skip = "Requires proper 3D mesh geometry - see integration tests")]
    public void BuildBox_HasCorrectBounds()
    {
        var builder = TestHelper.CreateTestBuilder();
        var min = new Vector3(0, 0, 0);
        var max = new Vector3(50, 0, 50);

        var navMesh = builder.BuildBox(min, max);

        var bounds = navMesh.Bounds;
        Assert.True(bounds.Min.X <= min.X + 1f);  // Allow for padding
        Assert.True(bounds.Max.X >= max.X - 1f);
        Assert.True(bounds.Min.Z <= min.Z + 1f);
        Assert.True(bounds.Max.Z >= max.Z - 1f);
    }

    #endregion

    #region NavMesh Properties Tests

    [Fact(Skip = "Requires proper 3D mesh geometry - see integration tests")]
    public void Build_ResultHasCorrectAgentSettings()
    {
        var config = TestHelper.CreateTestConfig();
        var builder = new DotRecastMeshBuilder(config);

        var navMesh = builder.BuildBox(
            new Vector3(0, 0, 0),
            new Vector3(50, 0, 50));

        Assert.Equal(config.AgentRadius, navMesh.BuiltForAgent.Radius);
        Assert.Equal(config.AgentHeight, navMesh.BuiltForAgent.Height);
    }

    [Fact(Skip = "Requires proper 3D mesh geometry - see integration tests")]
    public void Build_ResultIsINavigationMesh()
    {
        var builder = TestHelper.CreateTestBuilder();

        INavigationMesh navMesh = builder.BuildBox(
            new Vector3(0, 0, 0),
            new Vector3(50, 0, 50));

        Assert.NotNull(navMesh);
        Assert.False(string.IsNullOrEmpty(navMesh.Id));
    }

    #endregion
}
