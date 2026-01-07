using System.Numerics;
using DotRecast.Core.Numerics;
using DotRecast.Detour;
using KeenEyes.Navigation.Abstractions;

namespace KeenEyes.Navigation.DotRecast;

/// <summary>
/// Navigation provider using DotRecast for 3D navmesh pathfinding.
/// </summary>
/// <remarks>
/// <para>
/// This provider implements industry-standard navigation mesh pathfinding
/// using DotRecast, a C# port of Recast/Detour. It supports:
/// </para>
/// <list type="bullet">
/// <item>Synchronous and asynchronous path computation</item>
/// <item>Path smoothing using the funnel algorithm</item>
/// <item>Raycast line-of-sight queries</item>
/// <item>Area-based cost modifiers</item>
/// <item>Thread-safe query pooling</item>
/// </list>
/// </remarks>
public sealed class DotRecastProvider : INavigationProvider
{
    private readonly NavMeshConfig config;
    private readonly Queue<DotRecastPathRequest> pendingRequests;
    private readonly Lock requestLock = new();
    private readonly float[] areaCosts = new float[32];

    private NavMeshData? activeMesh;
    private NavMeshQueryPool? queryPool;
    private bool disposed;

    /// <summary>
    /// Creates a new DotRecast navigation provider.
    /// </summary>
    /// <param name="config">Configuration options.</param>
    /// <exception cref="ArgumentNullException">Thrown when config is null.</exception>
    /// <exception cref="ArgumentException">Thrown when config validation fails.</exception>
    public DotRecastProvider(NavMeshConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var error = config.Validate();
        if (error != null)
        {
            throw new ArgumentException(error, nameof(config));
        }

        this.config = config;
        pendingRequests = new Queue<DotRecastPathRequest>(config.MaxPendingRequests);

        // Initialize default area costs
        for (int i = 0; i < areaCosts.Length; i++)
        {
            areaCosts[i] = 1.0f;
        }
    }

    /// <summary>
    /// Creates a new DotRecast navigation provider with a pre-built navmesh.
    /// </summary>
    /// <param name="navMeshData">The pre-built navigation mesh.</param>
    /// <param name="config">Configuration options.</param>
    public DotRecastProvider(NavMeshData navMeshData, NavMeshConfig config)
        : this(config)
    {
        SetNavMesh(navMeshData);
    }

    /// <summary>
    /// Creates a new DotRecast navigation provider with default configuration.
    /// </summary>
    public DotRecastProvider()
        : this(NavMeshConfig.Default)
    {
    }

    /// <inheritdoc/>
    public NavigationStrategy Strategy => NavigationStrategy.NavMesh;

    /// <inheritdoc/>
    public bool IsReady => !disposed && activeMesh != null;

    /// <inheritdoc/>
    public INavigationMesh? ActiveMesh => activeMesh;

    /// <summary>
    /// Gets the current configuration.
    /// </summary>
    public NavMeshConfig Config => config;

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

    /// <summary>
    /// Sets the active navigation mesh.
    /// </summary>
    /// <param name="navMeshData">The navigation mesh to use.</param>
    /// <exception cref="ArgumentNullException">Thrown when navMeshData is null.</exception>
    public void SetNavMesh(NavMeshData navMeshData)
    {
        ArgumentNullException.ThrowIfNull(navMeshData);
        ThrowIfDisposed();

        // Dispose old query pool
        queryPool?.Dispose();

        activeMesh = navMeshData;
        queryPool = new NavMeshQueryPool(navMeshData.InternalNavMesh);
    }

    #region Pathfinding

    /// <inheritdoc/>
    public NavPath FindPath(Vector3 start, Vector3 end, AgentSettings agent, NavAreaMask areaMask = NavAreaMask.All)
    {
        ThrowIfDisposed();

        if (activeMesh == null || queryPool == null)
        {
            return NavPath.Empty;
        }

        using var pooledQuery = queryPool.Borrow();
        var query = pooledQuery.Query;
        var filter = CreateFilter(areaMask);

        // Find start and end polygons
        var startVec = ToRcVec3f(start);
        var endVec = ToRcVec3f(end);
        var extents = new RcVec3f(agent.Radius * 2, agent.Height, agent.Radius * 2);

        var status = query.FindNearestPoly(startVec, extents, filter, out var startRef, out var startPt, out _);
        if (status.Failed() || startRef == 0)
        {
            return NavPath.Empty;
        }

        status = query.FindNearestPoly(endVec, extents, filter, out var endRef, out var endPt, out _);
        if (status.Failed() || endRef == 0)
        {
            return NavPath.Empty;
        }

        // Find path through polygon corridor
        var pathList = new List<long>(256);
        status = query.FindPath(startRef, endRef, startPt, endPt, filter, ref pathList, DtFindPathOption.AnyAngle);

        if (status.Failed() || pathList.Count == 0)
        {
            return NavPath.Empty;
        }

        // Convert polygon path to straight path (funnel algorithm)
        Span<DtStraightPath> straightPath = stackalloc DtStraightPath[256];
        status = query.FindStraightPath(
            startPt, endPt,
            pathList, pathList.Count,
            straightPath, out var straightPathCount, 256,
            DtStraightPathOptions.DT_STRAIGHTPATH_ALL_CROSSINGS);

        if (status.Failed() || straightPathCount == 0)
        {
            return NavPath.Empty;
        }

        // Convert to NavPath
        var waypoints = new NavPoint[straightPathCount];
        float totalCost = 0f;

        for (int i = 0; i < straightPathCount; i++)
        {
            var pt = straightPath[i];
            var pos = ToVector3(pt.pos);

            activeMesh.InternalNavMesh.GetPolyArea(pt.refs, out var area);
            waypoints[i] = new NavPoint(pos, (NavAreaType)area, (uint)pt.refs);

            if (i > 0)
            {
                float dist = Vector3.Distance(waypoints[i - 1].Position, pos);
                float cost = dist * GetAreaCost((NavAreaType)area);
                totalCost += cost;
            }
        }

        // Check if path reaches destination
        bool isComplete = straightPath[straightPathCount - 1].refs == endRef ||
                         Vector3.DistanceSquared(waypoints[^1].Position, end) < 0.25f;

        return new NavPath(waypoints, isComplete, totalCost);
    }

    /// <inheritdoc/>
    public IPathRequest RequestPath(Vector3 start, Vector3 end, AgentSettings agent, NavAreaMask areaMask = NavAreaMask.All)
    {
        ThrowIfDisposed();

        var request = new DotRecastPathRequest(start, end, agent, areaMask);

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

        if (activeMesh == null || queryPool == null)
        {
            hitPosition = end;
            return false;
        }

        using var pooledQuery = queryPool.Borrow();
        var query = pooledQuery.Query;
        var filter = new DtQueryDefaultFilter();

        var startVec = ToRcVec3f(start);
        var endVec = ToRcVec3f(end);
        var extents = new RcVec3f(1f, 2f, 1f);

        // Find start polygon
        var status = query.FindNearestPoly(startVec, extents, filter, out var startRef, out var startPt, out _);
        if (status.Failed() || startRef == 0)
        {
            hitPosition = start;
            return true;
        }

        // Perform raycast using DtRaycastHit
        var hit = new DtRaycastHit();
        status = query.Raycast(startRef, startPt, endVec, filter, 0, ref hit, 0);

        if (status.Failed())
        {
            hitPosition = end;
            return false;
        }

        if (hit.t < 1.0f)
        {
            // Hit occurred
            hitPosition = Vector3.Lerp(start, end, hit.t);
            return true;
        }

        hitPosition = end;
        return false;
    }

    /// <inheritdoc/>
    public bool Raycast(Vector3 start, Vector3 end, NavAreaMask areaMask, out Vector3 hitPosition, out NavAreaType hitAreaType)
    {
        ThrowIfDisposed();

        if (activeMesh == null || queryPool == null)
        {
            hitPosition = end;
            hitAreaType = NavAreaType.NotWalkable;
            return false;
        }

        using var pooledQuery = queryPool.Borrow();
        var query = pooledQuery.Query;
        var filter = CreateFilter(areaMask);

        var startVec = ToRcVec3f(start);
        var endVec = ToRcVec3f(end);
        var extents = new RcVec3f(1f, 2f, 1f);

        // Find start polygon
        var status = query.FindNearestPoly(startVec, extents, filter, out var startRef, out var startPt, out _);
        if (status.Failed() || startRef == 0)
        {
            hitPosition = start;
            hitAreaType = NavAreaType.NotWalkable;
            return true;
        }

        // Perform raycast using DtRaycastHit
        var hit = new DtRaycastHit();
        status = query.Raycast(startRef, startPt, endVec, filter, 0, ref hit, 0);

        if (status.Failed())
        {
            hitPosition = end;
            hitAreaType = NavAreaType.Walkable;
            return false;
        }

        if (hit.t < 1.0f)
        {
            // Hit occurred
            hitPosition = Vector3.Lerp(start, end, hit.t);

            // Get area type at hit point
            if (hit.path.Count > 0)
            {
                activeMesh.InternalNavMesh.GetPolyArea(hit.path[^1], out var area);
                hitAreaType = (NavAreaType)area;
            }
            else
            {
                hitAreaType = NavAreaType.NotWalkable;
            }

            return true;
        }

        hitPosition = end;

        // Get area type at end point
        status = query.FindNearestPoly(endVec, extents, filter, out var endRef, out _, out _);
        if (status.Succeeded() && endRef != 0)
        {
            activeMesh.InternalNavMesh.GetPolyArea(endRef, out var area);
            hitAreaType = (NavAreaType)area;
        }
        else
        {
            hitAreaType = NavAreaType.Walkable;
        }

        return false;
    }

    #endregion

    #region Point Queries

    /// <inheritdoc/>
    public NavPoint? FindNearestPoint(Vector3 position, float searchRadius = 10f)
    {
        ThrowIfDisposed();

        return activeMesh?.FindNearestPoint(position, searchRadius);
    }

    /// <inheritdoc/>
    public bool IsNavigable(Vector3 position, AgentSettings agent)
    {
        ThrowIfDisposed();

        if (activeMesh == null)
        {
            return false;
        }

        return activeMesh.IsOnNavMesh(position, agent.Radius);
    }

    /// <inheritdoc/>
    public Vector3? ProjectToNavMesh(Vector3 position, float maxDistance = 5f)
    {
        ThrowIfDisposed();

        var nearest = FindNearestPoint(position, maxDistance);
        return nearest?.Position;
    }

    #endregion

    #region Area Costs

    /// <inheritdoc/>
    public float GetAreaCost(NavAreaType areaType)
    {
        int index = (int)areaType;
        if (index >= 0 && index < areaCosts.Length)
        {
            return areaCosts[index];
        }

        return 1.0f;
    }

    /// <inheritdoc/>
    public void SetAreaCost(NavAreaType areaType, float cost)
    {
        ThrowIfDisposed();

        int index = (int)areaType;
        if (index >= 0 && index < areaCosts.Length)
        {
            areaCosts[index] = cost;
        }
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
            DotRecastPathRequest? request;

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
                var path = FindPath(request.Start, request.End, request.Agent, request.AreaMask);
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

        // Cancel pending requests
        lock (requestLock)
        {
            while (pendingRequests.Count > 0)
            {
                var request = pendingRequests.Dequeue();
                request.Cancel();
            }
        }

        queryPool?.Dispose();
        disposed = true;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }

    private IDtQueryFilter CreateFilter(NavAreaMask areaMask)
    {
        // DtQueryDefaultFilter constructor takes include flags, exclude flags, and area costs array
        return new DtQueryDefaultFilter((int)areaMask, 0, areaCosts);
    }

    private static Vector3 ToVector3(RcVec3f v) => new(v.X, v.Y, v.Z);

    private static RcVec3f ToRcVec3f(Vector3 v) => new(v.X, v.Y, v.Z);
}
