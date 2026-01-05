// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Navigation.Abstractions;
using KeenEyes.Navigation.Abstractions.Components;

namespace KeenEyes.Navigation.Abstractions.Tests.Components;

public class NavMeshSurfaceTests
{
    #region Create Tests

    [Fact]
    public void Create_ReturnsWalkableSurface()
    {
        var surface = NavMeshSurface.Create();

        Assert.Equal(NavAreaType.Walkable, surface.AreaType);
        Assert.True(surface.IsWalkable);
    }

    [Fact]
    public void Create_IncludesChildrenByDefault()
    {
        var surface = NavMeshSurface.Create();

        Assert.True(surface.IncludeChildren);
    }

    [Fact]
    public void Create_UsesRenderMeshesByDefault()
    {
        var surface = NavMeshSurface.Create();

        Assert.Equal(NavMeshCollectGeometry.RenderMeshes, surface.CollectGeometry);
    }

    [Fact]
    public void Create_HasZeroLayerByDefault()
    {
        var surface = NavMeshSurface.Create();

        Assert.Equal(0, surface.Layer);
    }

    [Fact]
    public void Create_UsesDefaultsByDefault()
    {
        var surface = NavMeshSurface.Create();

        Assert.True(surface.UseDefaults);
    }

    [Fact]
    public void Create_HasNoOverrideVoxelSize()
    {
        var surface = NavMeshSurface.Create();

        Assert.Equal(0, surface.OverrideVoxelSize);
    }

    [Fact]
    public void Create_HasNoOverrideTileSize()
    {
        var surface = NavMeshSurface.Create();

        Assert.Equal(0, surface.OverrideTileSize);
    }

    #endregion

    #region Create With AreaType Tests

    [Fact]
    public void Create_WithAreaType_SetsAreaType()
    {
        var surface = NavMeshSurface.Create(NavAreaType.Water);

        Assert.Equal(NavAreaType.Water, surface.AreaType);
    }

    [Fact]
    public void Create_WithAreaType_RemainsWalkable()
    {
        var surface = NavMeshSurface.Create(NavAreaType.Road);

        Assert.True(surface.IsWalkable);
    }

    #endregion

    #region CreateObstacle Tests

    [Fact]
    public void CreateObstacle_IsNotWalkable()
    {
        var surface = NavMeshSurface.CreateObstacle();

        Assert.False(surface.IsWalkable);
    }

    [Fact]
    public void CreateObstacle_HasNotWalkableAreaType()
    {
        var surface = NavMeshSurface.CreateObstacle();

        Assert.Equal(NavAreaType.NotWalkable, surface.AreaType);
    }

    [Fact]
    public void CreateObstacle_IncludesChildren()
    {
        var surface = NavMeshSurface.CreateObstacle();

        Assert.True(surface.IncludeChildren);
    }

    #endregion
}
