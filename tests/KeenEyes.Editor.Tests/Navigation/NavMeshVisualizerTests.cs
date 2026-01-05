// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.Numerics;
using KeenEyes.Editor.Navigation;
using KeenEyes.Navigation.Abstractions;

namespace KeenEyes.Editor.Tests.Navigation;

public class NavMeshVisualizerTests
{
    #region Property Tests

    [Fact]
    public void Id_ReturnsExpectedValue()
    {
        var visualizer = new NavMeshVisualizer();

        Assert.Equal("navmesh-visualizer", visualizer.Id);
    }

    [Fact]
    public void DisplayName_ReturnsNavMesh()
    {
        var visualizer = new NavMeshVisualizer();

        Assert.Equal("NavMesh", visualizer.DisplayName);
    }

    [Fact]
    public void IsEnabled_DefaultsToTrue()
    {
        var visualizer = new NavMeshVisualizer();

        Assert.True(visualizer.IsEnabled);
    }

    [Fact]
    public void IsEnabled_CanBeSet()
    {
        var visualizer = new NavMeshVisualizer { IsEnabled = false };

        Assert.False(visualizer.IsEnabled);
    }

    [Fact]
    public void DisplayMode_DefaultsToSurfacesAndEdges()
    {
        var visualizer = new NavMeshVisualizer();

        Assert.Equal(NavMeshDisplayMode.SurfacesAndEdges, visualizer.DisplayMode);
    }

    [Fact]
    public void DisplayMode_CanBeChanged()
    {
        var visualizer = new NavMeshVisualizer { DisplayMode = NavMeshDisplayMode.All };

        Assert.Equal(NavMeshDisplayMode.All, visualizer.DisplayMode);
    }

    [Fact]
    public void SurfaceAlpha_DefaultsToHalf()
    {
        var visualizer = new NavMeshVisualizer();

        Assert.Equal(0.5f, visualizer.SurfaceAlpha);
    }

    [Fact]
    public void Order_Returns100()
    {
        var visualizer = new NavMeshVisualizer();

        Assert.Equal(100, visualizer.Order);
    }

    #endregion

    #region NavMesh Management Tests

    [Fact]
    public void SetNavMesh_UpdatesCurrentMesh()
    {
        var visualizer = new NavMeshVisualizer();

        // Create a null navmesh first, then set it to null to verify behavior
        visualizer.SetNavMesh(null);

        Assert.Null(visualizer.GetNavMesh());
    }

    #endregion

    #region Area Color Tests

    [Fact]
    public void GetAreaColor_ReturnsDefaultColorForWalkable()
    {
        var visualizer = new NavMeshVisualizer();

        var color = visualizer.GetAreaColor(NavAreaType.Walkable);

        // Should return cyan-ish color
        Assert.True(color.Z > 0.5f); // Blue component
    }

    [Fact]
    public void GetAreaColor_ReturnsRedForHazard()
    {
        var visualizer = new NavMeshVisualizer();

        var color = visualizer.GetAreaColor(NavAreaType.Hazard);

        Assert.True(color.X > 0.5f); // Red component
        Assert.True(color.Y < 0.3f); // Green component
    }

    [Fact]
    public void SetAreaColor_OverridesDefaultColor()
    {
        var visualizer = new NavMeshVisualizer();
        var customColor = new Vector4(1.0f, 0.5f, 0.0f, 1.0f);

        visualizer.SetAreaColor(NavAreaType.Walkable, customColor);

        Assert.Equal(customColor, visualizer.GetAreaColor(NavAreaType.Walkable));
    }

    #endregion

    #region Display Mode Flags Tests

    [Fact]
    public void DisplayMode_SurfacesAndEdges_HasBothFlags()
    {
        var mode = NavMeshDisplayMode.SurfacesAndEdges;

        Assert.True(mode.HasFlag(NavMeshDisplayMode.Surfaces));
        Assert.True(mode.HasFlag(NavMeshDisplayMode.Edges));
        Assert.False(mode.HasFlag(NavMeshDisplayMode.Vertices));
    }

    [Fact]
    public void DisplayMode_All_HasAllFlags()
    {
        var mode = NavMeshDisplayMode.All;

        Assert.True(mode.HasFlag(NavMeshDisplayMode.Surfaces));
        Assert.True(mode.HasFlag(NavMeshDisplayMode.Edges));
        Assert.True(mode.HasFlag(NavMeshDisplayMode.Vertices));
    }

    [Fact]
    public void DisplayMode_None_HasNoFlags()
    {
        var mode = NavMeshDisplayMode.None;

        Assert.False(mode.HasFlag(NavMeshDisplayMode.Surfaces));
        Assert.False(mode.HasFlag(NavMeshDisplayMode.Edges));
        Assert.False(mode.HasFlag(NavMeshDisplayMode.Vertices));
    }

    #endregion

    #region Edge and Vertex Settings Tests

    [Fact]
    public void EdgeWidth_DefaultsToTwo()
    {
        var visualizer = new NavMeshVisualizer();

        Assert.Equal(2.0f, visualizer.EdgeWidth);
    }

    [Fact]
    public void VertexSize_DefaultsToFive()
    {
        var visualizer = new NavMeshVisualizer();

        Assert.Equal(5.0f, visualizer.VertexSize);
    }

    [Fact]
    public void HeightOffset_DefaultsToSmallValue()
    {
        var visualizer = new NavMeshVisualizer();

        Assert.Equal(0.02f, visualizer.HeightOffset);
    }

    [Fact]
    public void ShowAreaLabels_DefaultsToFalse()
    {
        var visualizer = new NavMeshVisualizer();

        Assert.False(visualizer.ShowAreaLabels);
    }

    [Fact]
    public void ShowPolygonIndices_DefaultsToFalse()
    {
        var visualizer = new NavMeshVisualizer();

        Assert.False(visualizer.ShowPolygonIndices);
    }

    #endregion
}
