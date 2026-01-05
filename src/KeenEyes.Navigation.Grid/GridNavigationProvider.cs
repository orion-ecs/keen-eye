using System.Numerics;
using KeenEyes.Navigation.Abstractions;

namespace KeenEyes.Navigation.Grid;

/// <summary>
/// Grid-based navigation provider implementing A* pathfinding.
/// </summary>
/// <remarks>
/// <para>
/// Provides pathfinding services on a 2D grid using the A* algorithm.
/// Supports both synchronous and asynchronous path computation, area
/// filtering, and configurable movement costs.
/// </para>
/// <para>
/// For 2D games or games with tile-based movement, this provider offers
/// simple and efficient pathfinding without the complexity of navmesh generation.
/// </para>
/// </remarks>
public sealed class GridNavigationProvider : INavigationProvider
{
    private readonly NavigationGrid grid;
    private readonly GridConfig config;
    private readonly AStarPathfinder pathfinder;
    private readonly Queue<GridPathRequest> pendingRequests;
    private readonly Lock requestLock = new();
    private bool disposed;

    /// <summary>
    /// Creates a new grid navigation provider.
    /// </summary>
    /// <param name="config">Configuration options.</param>
    /// <exception cref="ArgumentNullException">Thrown when config is null.</exception>
    /// <exception cref="ArgumentException">Thrown when config validation fails.</exception>
    public GridNavigationProvider(GridConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var error = config.Validate();
        if (error != null)
        {
            throw new ArgumentException(error, nameof(config));
        }

        this.config = config;
        grid = new NavigationGrid(config.Width, config.Height, config.CellSize, config.WorldOrigin);
        pathfinder = new AStarPathfinder(grid, config);
        pendingRequests = new Queue<GridPathRequest>(config.MaxPendingRequests);
    }

    /// <summary>
    /// Creates a new grid navigation provider with an existing grid.
    /// </summary>
    /// <param name="grid">The navigation grid to use.</param>
    /// <param name="config">Configuration options.</param>
    /// <exception cref="ArgumentNullException">Thrown when grid or config is null.</exception>
    public GridNavigationProvider(NavigationGrid grid, GridConfig config)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(config);

        this.grid = grid;
        this.config = config;
        pathfinder = new AStarPathfinder(grid, config);
        pendingRequests = new Queue<GridPathRequest>(config.MaxPendingRequests);
    }

    /// <inheritdoc/>
    public NavigationStrategy Strategy => NavigationStrategy.Grid;

    /// <inheritdoc/>
    public bool IsReady => !disposed;

    /// <inheritdoc/>
    public INavigationMesh? ActiveMesh => null; // Grid-based navigation doesn't use meshes

    /// <summary>
    /// Gets the underlying navigation grid.
    /// </summary>
    public NavigationGrid Grid => grid;

    /// <summary>
    /// Gets the A* pathfinder instance.
    /// </summary>
    public AStarPathfinder Pathfinder => pathfinder;

    /// <summary>
    /// Gets the current number of pending path requests.
    /// </summary>
    public int PendingRequestCount
    {
        get
        {
            lock (requestLock)
            {
                return pendingRequests.Count;
            }
        }
    }

    #region Pathfinding

    /// <inheritdoc/>
    public NavPath FindPath(Vector3 start, Vector3 end, AgentSettings agent, NavAreaMask areaMask = NavAreaMask.All)
    {
        ThrowIfDisposed();

        // Grid navigation ignores agent settings for now (could be extended to consider radius)
        return pathfinder.FindPath(start, end, areaMask);
    }

    /// <summary>
    /// Finds a path using grid coordinates.
    /// </summary>
    /// <param name="start">The starting grid coordinate.</param>
    /// <param name="end">The destination grid coordinate.</param>
    /// <param name="areaMask">Optional area mask.</param>
    /// <returns>The computed path.</returns>
    public NavPath FindPath(GridCoordinate start, GridCoordinate end, NavAreaMask areaMask = NavAreaMask.All)
    {
        ThrowIfDisposed();
        return pathfinder.FindPath(start, end, areaMask);
    }

    /// <summary>
    /// Finds a path using grid coordinates and a pre-allocated buffer (zero allocation).
    /// </summary>
    /// <param name="start">The starting grid coordinate.</param>
    /// <param name="end">The destination grid coordinate.</param>
    /// <param name="result">Buffer to store the path coordinates.</param>
    /// <returns>The number of coordinates in the path, or -1 if no path exists.</returns>
    public int FindPath(GridCoordinate start, GridCoordinate end, Span<GridCoordinate> result)
    {
        ThrowIfDisposed();
        return pathfinder.FindPath(start, end, result);
    }

    /// <summary>
    /// Finds a path using grid coordinates and a pre-allocated buffer with area filtering.
    /// </summary>
    /// <param name="start">The starting grid coordinate.</param>
    /// <param name="end">The destination grid coordinate.</param>
    /// <param name="areaMask">Area mask to filter traversable cells.</param>
    /// <param name="result">Buffer to store the path coordinates.</param>
    /// <returns>The number of coordinates in the path, or -1 if no path exists.</returns>
    public int FindPath(GridCoordinate start, GridCoordinate end, NavAreaMask areaMask, Span<GridCoordinate> result)
    {
        ThrowIfDisposed();
        return pathfinder.FindPath(start, end, areaMask, result);
    }

    /// <inheritdoc/>
    public IPathRequest RequestPath(Vector3 start, Vector3 end, AgentSettings agent, NavAreaMask areaMask = NavAreaMask.All)
    {
        ThrowIfDisposed();

        var request = new GridPathRequest(start, end, agent, areaMask);

        lock (requestLock)
        {
            if (pendingRequests.Count >= config.MaxPendingRequests)
            {
                request.Fail();
                return request;
            }

            pendingRequests.Enqueue(request);
        }

        return request;
    }

    /// <inheritdoc/>
    public void CancelAllRequests()
    {
        ThrowIfDisposed();

        lock (requestLock)
        {
            while (pendingRequests.Count > 0)
            {
                var request = pendingRequests.Dequeue();
                request.Cancel();
            }
        }
    }

    #endregion

    #region Raycasting

    /// <inheritdoc/>
    public bool Raycast(Vector3 start, Vector3 end, out Vector3 hitPosition)
    {
        ThrowIfDisposed();

        var startCoord = grid.FromWorldPosition(start);
        var endCoord = grid.FromWorldPosition(end);

        if (RaycastGrid(startCoord, endCoord, NavAreaMask.All, out var hitCoord))
        {
            hitPosition = grid.ToWorldPosition(hitCoord);
            return true;
        }

        hitPosition = end;
        return false;
    }

    /// <inheritdoc/>
    public bool Raycast(Vector3 start, Vector3 end, NavAreaMask areaMask, out Vector3 hitPosition, out NavAreaType hitAreaType)
    {
        ThrowIfDisposed();

        var startCoord = grid.FromWorldPosition(start);
        var endCoord = grid.FromWorldPosition(end);

        if (RaycastGrid(startCoord, endCoord, areaMask, out var hitCoord))
        {
            hitPosition = grid.ToWorldPosition(hitCoord);
            hitAreaType = grid.GetAreaType(hitCoord);
            return true;
        }

        hitPosition = end;
        hitAreaType = grid.GetAreaType(endCoord);
        return false;
    }

    /// <summary>
    /// Performs a raycast on the grid using Bresenham's line algorithm.
    /// </summary>
    /// <param name="start">The starting grid coordinate.</param>
    /// <param name="end">The ending grid coordinate.</param>
    /// <param name="areaMask">Area mask to filter traversable cells.</param>
    /// <param name="hitCoord">The coordinate where the ray hit an obstacle.</param>
    /// <returns>True if the ray hit an obstacle, false if the path is clear.</returns>
    public bool RaycastGrid(GridCoordinate start, GridCoordinate end, NavAreaMask areaMask, out GridCoordinate hitCoord)
    {
        int x0 = start.X;
        int y0 = start.Y;
        int x1 = end.X;
        int y1 = end.Y;

        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            var current = new GridCoordinate(x0, y0);

            // Skip the start position
            if (current != start && !grid.IsWalkable(current, areaMask))
            {
                hitCoord = current;
                return true;
            }

            if (x0 == x1 && y0 == y1)
            {
                break;
            }

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }

        hitCoord = end;
        return false;
    }

    #endregion

    #region Point Queries

    /// <inheritdoc/>
    public NavPoint? FindNearestPoint(Vector3 position, float searchRadius = 10f)
    {
        ThrowIfDisposed();

        var centerCoord = grid.FromWorldPosition(position);
        int radiusCells = (int)MathF.Ceiling(searchRadius / grid.CellSize);

        // Check center first
        if (grid.IsWalkable(centerCoord))
        {
            return new NavPoint(
                grid.ToWorldPosition(centerCoord),
                grid.GetAreaType(centerCoord));
        }

        // Spiral outward
        float bestDistSq = float.MaxValue;
        GridCoordinate? best = null;

        for (int r = 1; r <= radiusCells; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    if (Math.Abs(dx) != r && Math.Abs(dy) != r)
                    {
                        continue; // Only check the perimeter at this radius
                    }

                    var coord = new GridCoordinate(centerCoord.X + dx, centerCoord.Y + dy);
                    if (!grid.IsWalkable(coord))
                    {
                        continue;
                    }

                    var worldPos = grid.ToWorldPosition(coord);
                    float distSq = Vector3.DistanceSquared(position, worldPos);
                    if (distSq < bestDistSq && distSq <= searchRadius * searchRadius)
                    {
                        bestDistSq = distSq;
                        best = coord;
                    }
                }
            }

            // If we found something at this radius, we're done
            if (best.HasValue)
            {
                break;
            }
        }

        if (!best.HasValue)
        {
            return null;
        }

        return new NavPoint(
            grid.ToWorldPosition(best.Value),
            grid.GetAreaType(best.Value));
    }

    /// <inheritdoc/>
    public bool IsNavigable(Vector3 position, AgentSettings agent)
    {
        ThrowIfDisposed();

        var coord = grid.FromWorldPosition(position);
        return grid.IsWalkable(coord);
    }

    /// <inheritdoc/>
    public Vector3? ProjectToNavMesh(Vector3 position, float maxDistance = 5f)
    {
        ThrowIfDisposed();

        var nearestPoint = FindNearestPoint(position, maxDistance);
        return nearestPoint?.Position;
    }

    /// <summary>
    /// Checks if a grid coordinate is navigable.
    /// </summary>
    /// <param name="coord">The coordinate to check.</param>
    /// <param name="areaMask">Optional area mask.</param>
    /// <returns>True if the coordinate is navigable.</returns>
    public bool IsNavigable(GridCoordinate coord, NavAreaMask areaMask = NavAreaMask.All)
    {
        ThrowIfDisposed();
        return grid.IsWalkable(coord, areaMask);
    }

    #endregion

    #region Area Costs

    /// <inheritdoc/>
    public float GetAreaCost(NavAreaType areaType)
    {
        ThrowIfDisposed();
        return pathfinder.GetAreaCost(areaType);
    }

    /// <inheritdoc/>
    public void SetAreaCost(NavAreaType areaType, float cost)
    {
        ThrowIfDisposed();
        pathfinder.SetAreaCost(areaType, cost);
    }

    #endregion

    #region Updates

    /// <inheritdoc/>
    public void Update(float deltaTime)
    {
        ThrowIfDisposed();

        int requestsToProcess = config.RequestsPerUpdate;

        for (int i = 0; i < requestsToProcess; i++)
        {
            GridPathRequest? request;

            lock (requestLock)
            {
                if (pendingRequests.Count == 0)
                {
                    break;
                }

                request = pendingRequests.Dequeue();
            }

            if (request.Status == PathRequestStatus.Cancelled)
            {
                continue;
            }

            request.MarkComputing();

            try
            {
                var path = pathfinder.FindPath(request.Start, request.End, request.AreaMask);
                request.Complete(path);
            }
            catch
            {
                request.Fail();
            }
        }
    }

    #endregion

    /// <inheritdoc/>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        // Cancel requests before marking as disposed
        lock (requestLock)
        {
            while (pendingRequests.Count > 0)
            {
                var request = pendingRequests.Dequeue();
                request.Cancel();
            }
        }

        disposed = true;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }
}
