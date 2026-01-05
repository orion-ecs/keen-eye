using System.Numerics;
using KeenEyes.Navigation.Abstractions;
using KeenEyes.Navigation.Grid;

namespace KeenEyes.Navigation.Grid.Tests;

/// <summary>
/// Tests for <see cref="GridNavigationProvider"/> class.
/// </summary>
public class GridNavigationProviderTests : IDisposable
{
    private readonly GridNavigationProvider provider;
    private readonly GridConfig config;

    public GridNavigationProviderTests()
    {
        config = new GridConfig
        {
            Width = 20,
            Height = 20,
            CellSize = 1f,
            AllowDiagonal = true
        };
        provider = new GridNavigationProvider(config);
    }

    public void Dispose()
    {
        provider.Dispose();
    }

    #region Construction Tests

    [Fact]
    public void Constructor_ValidConfig_CreatesProvider()
    {
        Assert.NotNull(provider);
        Assert.Equal(NavigationStrategy.Grid, provider.Strategy);
        Assert.True(provider.IsReady);
        Assert.Null(provider.ActiveMesh);  // Grid doesn't use navmesh
    }

    [Fact]
    public void Constructor_NullConfig_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => new GridNavigationProvider(null!));
    }

    [Fact]
    public void Constructor_InvalidConfig_ThrowsArgument()
    {
        var invalidConfig = new GridConfig { Width = 0 };

        Assert.Throws<ArgumentException>(() => new GridNavigationProvider(invalidConfig));
    }

    [Fact]
    public void Constructor_WithExistingGrid_UsesGrid()
    {
        var grid = new NavigationGrid(10, 10, 2f);
        grid[5, 5] = GridCell.Blocked;

        using var providerWithGrid = new GridNavigationProvider(grid, GridConfig.Default);

        Assert.Same(grid, providerWithGrid.Grid);
        Assert.False(providerWithGrid.Grid[5, 5].IsWalkable);
    }

    #endregion

    #region FindPath Tests

    [Fact]
    public void FindPath_ValidPath_ReturnsPath()
    {
        var path = provider.FindPath(
            new Vector3(0.5f, 0, 0.5f),
            new Vector3(5.5f, 0, 5.5f),
            AgentSettings.Default);

        Assert.True(path.IsValid);
        Assert.True(path.IsComplete);
        Assert.True(path.Count >= 2);
    }

    [Fact]
    public void FindPath_GridCoordinates_ReturnsPath()
    {
        var path = provider.FindPath(
            new GridCoordinate(0, 0),
            new GridCoordinate(5, 5));

        Assert.True(path.IsValid);
        Assert.True(path.IsComplete);
    }

    [Fact]
    public void FindPath_SpanOverload_ReturnsPathLength()
    {
        Span<GridCoordinate> result = stackalloc GridCoordinate[50];

        int length = provider.FindPath(
            new GridCoordinate(0, 0),
            new GridCoordinate(5, 5),
            result);

        Assert.True(length > 0);
    }

    [Fact]
    public void FindPath_WithAreaMask_FiltersCorrectly()
    {
        // Create a water barrier
        for (int y = 0; y < 10; y++)
        {
            provider.Grid[10, y] = GridCell.WithAreaType(NavAreaType.Water);
        }

        // Path that needs to cross water
        var pathWithWater = provider.FindPath(
            new Vector3(5.5f, 0, 5.5f),
            new Vector3(15.5f, 0, 5.5f),
            AgentSettings.Default,
            NavAreaMask.Walkable | NavAreaMask.Water);

        var pathWithoutWater = provider.FindPath(
            new Vector3(5.5f, 0, 5.5f),
            new Vector3(15.5f, 0, 5.5f),
            AgentSettings.Default,
            NavAreaMask.Walkable);

        Assert.True(pathWithWater.IsValid);
        Assert.True(pathWithoutWater.IsValid);
        // Path without water should be longer as it goes around
        Assert.True(pathWithoutWater.Count > pathWithWater.Count);
    }

    #endregion

    #region Async Path Request Tests

    [Fact]
    public void RequestPath_ReturnsPathRequest()
    {
        using var request = provider.RequestPath(
            new Vector3(0.5f, 0, 0.5f),
            new Vector3(5.5f, 0, 5.5f),
            AgentSettings.Default);

        Assert.NotNull(request);
        Assert.True(request.Id > 0);
        Assert.Equal(PathRequestStatus.Pending, request.Status);
    }

    [Fact]
    public void RequestPath_AfterUpdate_CompletesPath()
    {
        using var request = provider.RequestPath(
            new Vector3(0.5f, 0, 0.5f),
            new Vector3(5.5f, 0, 5.5f),
            AgentSettings.Default);

        provider.Update(0.016f);  // Simulate frame update

        Assert.Equal(PathRequestStatus.Completed, request.Status);
        Assert.True(request.Result.IsValid);
    }

    [Fact]
    public void RequestPath_Cancel_CancelsRequest()
    {
        using var request = provider.RequestPath(
            new Vector3(0.5f, 0, 0.5f),
            new Vector3(5.5f, 0, 5.5f),
            AgentSettings.Default);

        request.Cancel();

        Assert.Equal(PathRequestStatus.Cancelled, request.Status);
    }

    [Fact]
    public async Task RequestPath_AsTask_Completes()
    {
        using var request = provider.RequestPath(
            new Vector3(0.5f, 0, 0.5f),
            new Vector3(5.5f, 0, 5.5f),
            AgentSettings.Default);

        // Process the request synchronously, then await the completed task
        provider.Update(0.016f);
        var path = await request.AsTask();

        Assert.True(path.IsValid);
    }

    [Fact]
    public void CancelAllRequests_CancelsAllPending()
    {
        var request1 = provider.RequestPath(Vector3.Zero, new Vector3(5, 0, 5), AgentSettings.Default);
        var request2 = provider.RequestPath(Vector3.Zero, new Vector3(10, 0, 10), AgentSettings.Default);

        provider.CancelAllRequests();

        Assert.Equal(PathRequestStatus.Cancelled, request1.Status);
        Assert.Equal(PathRequestStatus.Cancelled, request2.Status);
    }

    [Fact]
    public void PendingRequestCount_ReflectsQueueSize()
    {
        Assert.Equal(0, provider.PendingRequestCount);

        using var request1 = provider.RequestPath(Vector3.Zero, new Vector3(5, 0, 5), AgentSettings.Default);
        using var request2 = provider.RequestPath(Vector3.Zero, new Vector3(10, 0, 10), AgentSettings.Default);

        Assert.Equal(2, provider.PendingRequestCount);

        provider.Update(0.016f);  // Process requests

        Assert.Equal(0, provider.PendingRequestCount);
    }

    #endregion

    #region Raycast Tests

    [Fact]
    public void Raycast_ClearPath_ReturnsFalse()
    {
        bool hit = provider.Raycast(
            new Vector3(0.5f, 0, 0.5f),
            new Vector3(5.5f, 0, 5.5f),
            out var hitPosition);

        Assert.False(hit);
        Assert.Equal(new Vector3(5.5f, 0, 5.5f), hitPosition);
    }

    [Fact]
    public void Raycast_HitsObstacle_ReturnsTrue()
    {
        provider.Grid[3, 0] = GridCell.Blocked;  // Block middle of ray

        bool hit = provider.Raycast(
            new Vector3(0.5f, 0, 0.5f),
            new Vector3(5.5f, 0, 0.5f),
            out _);

        Assert.True(hit);
    }

    [Fact]
    public void Raycast_WithAreaMask_RespectsFilter()
    {
        provider.Grid[3, 0] = GridCell.WithAreaType(NavAreaType.Water);

        bool hitWithoutWater = provider.Raycast(
            new Vector3(0.5f, 0, 0.5f),
            new Vector3(5.5f, 0, 0.5f),
            NavAreaMask.Walkable,
            out _,
            out _);

        bool hitWithWater = provider.Raycast(
            new Vector3(0.5f, 0, 0.5f),
            new Vector3(5.5f, 0, 0.5f),
            NavAreaMask.Walkable | NavAreaMask.Water,
            out _,
            out _);

        Assert.True(hitWithoutWater);   // Water blocks when not in mask
        Assert.False(hitWithWater);     // Water is traversable when in mask
    }

    [Fact]
    public void RaycastGrid_ReturnsCorrectHitCoordinate()
    {
        provider.Grid[5, 5] = GridCell.Blocked;

        bool hit = provider.RaycastGrid(
            new GridCoordinate(0, 5),
            new GridCoordinate(10, 5),
            NavAreaMask.All,
            out var hitCoord);

        Assert.True(hit);
        Assert.Equal(new GridCoordinate(5, 5), hitCoord);
    }

    #endregion

    #region Point Query Tests

    [Fact]
    public void FindNearestPoint_WalkablePosition_ReturnsPoint()
    {
        var point = provider.FindNearestPoint(new Vector3(5.5f, 0, 5.5f));

        Assert.NotNull(point);
        Assert.Equal(NavAreaType.Walkable, point.Value.AreaType);
    }

    [Fact]
    public void FindNearestPoint_BlockedPosition_FindsNearest()
    {
        provider.Grid[5, 5] = GridCell.Blocked;

        var point = provider.FindNearestPoint(new Vector3(5.5f, 0, 5.5f), searchRadius: 5f);

        Assert.NotNull(point);
        // Should find a nearby walkable cell
        Assert.True(point.Value.DistanceTo(new NavPoint(new Vector3(5.5f, 0, 5.5f))) > 0);
    }

    [Fact]
    public void FindNearestPoint_NoWalkableInRadius_ReturnsNull()
    {
        // Block a large area
        provider.Grid.Fill(new GridCoordinate(0, 0), new GridCoordinate(19, 19), GridCell.Blocked);

        var point = provider.FindNearestPoint(new Vector3(10.5f, 0, 10.5f), searchRadius: 3f);

        Assert.Null(point);
    }

    [Fact]
    public void IsNavigable_WalkablePosition_ReturnsTrue()
    {
        bool navigable = provider.IsNavigable(new Vector3(5.5f, 0, 5.5f), AgentSettings.Default);

        Assert.True(navigable);
    }

    [Fact]
    public void IsNavigable_BlockedPosition_ReturnsFalse()
    {
        provider.Grid[5, 5] = GridCell.Blocked;

        bool navigable = provider.IsNavigable(new Vector3(5.5f, 0, 5.5f), AgentSettings.Default);

        Assert.False(navigable);
    }

    [Fact]
    public void IsNavigable_GridCoordinate_Works()
    {
        Assert.True(provider.IsNavigable(new GridCoordinate(5, 5)));

        provider.Grid[5, 5] = GridCell.Blocked;

        Assert.False(provider.IsNavigable(new GridCoordinate(5, 5)));
    }

    [Fact]
    public void ProjectToNavMesh_ReturnsNearestWalkable()
    {
        var projected = provider.ProjectToNavMesh(new Vector3(5.5f, 10f, 5.5f));

        Assert.NotNull(projected);
        Assert.Equal(0f, projected.Value.Y, 0.0001f);  // Grid is at Y=0
    }

    [Fact]
    public void ProjectToNavMesh_NoNearby_ReturnsNull()
    {
        provider.Grid.Fill(new GridCoordinate(0, 0), new GridCoordinate(19, 19), GridCell.Blocked);

        var projected = provider.ProjectToNavMesh(new Vector3(10.5f, 0, 10.5f), maxDistance: 3f);

        Assert.Null(projected);
    }

    #endregion

    #region Area Cost Tests

    [Fact]
    public void GetAreaCost_Default_ReturnsOne()
    {
        Assert.Equal(1f, provider.GetAreaCost(NavAreaType.Walkable));
    }

    [Fact]
    public void SetAreaCost_UpdatesCost()
    {
        provider.SetAreaCost(NavAreaType.Mud, 5f);

        Assert.Equal(5f, provider.GetAreaCost(NavAreaType.Mud));
    }

    #endregion

    #region Disposal Tests

    [Fact]
    public void Dispose_MarksNotReady()
    {
        var tempProvider = new GridNavigationProvider(GridConfig.Default);
        Assert.True(tempProvider.IsReady);

        tempProvider.Dispose();

        Assert.False(tempProvider.IsReady);
    }

    [Fact]
    public void Dispose_SubsequentCalls_ThrowObjectDisposed()
    {
        var tempProvider = new GridNavigationProvider(GridConfig.Default);
        tempProvider.Dispose();

        Assert.Throws<ObjectDisposedException>(() =>
            tempProvider.FindPath(Vector3.Zero, Vector3.One, AgentSettings.Default));
    }

    [Fact]
    public void Dispose_CancelsPendingRequests()
    {
        var tempProvider = new GridNavigationProvider(GridConfig.Default);
        var request = tempProvider.RequestPath(Vector3.Zero, new Vector3(5, 0, 5), AgentSettings.Default);

        tempProvider.Dispose();

        Assert.Equal(PathRequestStatus.Cancelled, request.Status);
    }

    #endregion
}
