using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;
using KeenEyes.Navigation.Abstractions;

namespace KeenEyes.Navigation.Grid;

/// <summary>
/// A* pathfinding algorithm for grid-based navigation.
/// </summary>
/// <remarks>
/// <para>
/// Implements the A* algorithm with support for both 4-directional (cardinal)
/// and 8-directional (including diagonal) movement. Uses a binary heap for
/// efficient priority queue operations.
/// </para>
/// <para>
/// For zero-allocation pathfinding, use <see cref="FindPath(GridCoordinate, GridCoordinate, Span{GridCoordinate})"/>
/// with a pre-allocated buffer.
/// </para>
/// </remarks>
public sealed class AStarPathfinder
{
    private readonly NavigationGrid grid;
    private readonly GridConfig config;
    private readonly float[] areaCosts;

    // Node pool to reduce allocations
    private readonly PathNode[] nodePool;
    private readonly Dictionary<int, int> nodeIndices;
    private readonly PriorityQueue<int, float> openSet;
    private int nodeCount;

    // Reusable neighbor buffer
    private readonly GridCoordinate[] neighborBuffer = new GridCoordinate[8];

    private const float DiagonalCost = 1.41421356f; // sqrt(2)
    private const float CardinalCost = 1f;

    /// <summary>
    /// Creates a new A* pathfinder for the specified grid.
    /// </summary>
    /// <param name="grid">The navigation grid to pathfind on.</param>
    /// <param name="config">Configuration options.</param>
    public AStarPathfinder(NavigationGrid grid, GridConfig config)
    {
        this.grid = grid ?? throw new ArgumentNullException(nameof(grid));
        this.config = config ?? throw new ArgumentNullException(nameof(config));

        // Allocate node pool based on max iterations or grid size
        int poolSize = config.MaxIterations > 0
            ? Math.Min(config.MaxIterations, grid.CellCount)
            : grid.CellCount;

        nodePool = new PathNode[poolSize];
        nodeIndices = new Dictionary<int, int>(poolSize);
        openSet = new PriorityQueue<int, float>(poolSize);

        // Initialize area costs to 1.0
        areaCosts = new float[32];
        for (int i = 0; i < areaCosts.Length; i++)
        {
            areaCosts[i] = 1f;
        }
    }

    /// <summary>
    /// Gets the navigation grid used by this pathfinder.
    /// </summary>
    public NavigationGrid Grid => grid;

    /// <summary>
    /// Gets the configuration used by this pathfinder.
    /// </summary>
    public GridConfig Config => config;

    /// <summary>
    /// Gets or sets the cost multiplier for an area type.
    /// </summary>
    /// <param name="areaType">The area type.</param>
    /// <returns>The cost multiplier.</returns>
    public float GetAreaCost(NavAreaType areaType) => areaCosts[(int)areaType];

    /// <summary>
    /// Sets the cost multiplier for an area type.
    /// </summary>
    /// <param name="areaType">The area type.</param>
    /// <param name="cost">The cost multiplier (must be positive or zero for unwalkable).</param>
    public void SetAreaCost(NavAreaType areaType, float cost)
    {
        areaCosts[(int)areaType] = cost;
    }

    /// <summary>
    /// Finds a path from start to end and returns the result.
    /// </summary>
    /// <param name="start">The starting coordinate.</param>
    /// <param name="end">The destination coordinate.</param>
    /// <param name="areaMask">Optional area mask to filter traversable cells.</param>
    /// <returns>A NavPath containing the computed path.</returns>
    public NavPath FindPath(GridCoordinate start, GridCoordinate end, NavAreaMask areaMask = NavAreaMask.All)
    {
        // Use a reasonable default buffer size
        var buffer = ArrayPool<GridCoordinate>.Shared.Rent(grid.CellCount);
        try
        {
            int pathLength = FindPath(start, end, areaMask, buffer.AsSpan());
            if (pathLength <= 0)
            {
                return NavPath.Empty;
            }

            // Convert to NavPath
            var waypoints = new NavPoint[pathLength];
            float totalCost = 0f;

            for (int i = 0; i < pathLength; i++)
            {
                var coord = buffer[i];
                var areaType = grid.GetAreaType(coord);
                var position = grid.ToWorldPosition(coord);
                waypoints[i] = new NavPoint(position, areaType);

                if (i > 0)
                {
                    // Calculate cost between waypoints
                    bool isDiagonal = buffer[i - 1].X != coord.X && buffer[i - 1].Y != coord.Y;
                    float moveCost = isDiagonal ? DiagonalCost : CardinalCost;
                    totalCost += moveCost * grid.GetCost(coord) * areaCosts[(int)areaType];
                }
            }

            bool isComplete = pathLength > 0 && buffer[pathLength - 1] == end;
            return new NavPath(waypoints, isComplete, totalCost);
        }
        finally
        {
            ArrayPool<GridCoordinate>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Finds a path from start to end using a pre-allocated buffer (zero allocation).
    /// </summary>
    /// <param name="start">The starting coordinate.</param>
    /// <param name="end">The destination coordinate.</param>
    /// <param name="result">Buffer to store the path coordinates.</param>
    /// <returns>The number of coordinates in the path, or -1 if no path exists.</returns>
    public int FindPath(GridCoordinate start, GridCoordinate end, Span<GridCoordinate> result)
    {
        return FindPath(start, end, NavAreaMask.All, result);
    }

    /// <summary>
    /// Finds a path from start to end using a pre-allocated buffer with area filtering.
    /// </summary>
    /// <param name="start">The starting coordinate.</param>
    /// <param name="end">The destination coordinate.</param>
    /// <param name="areaMask">Area mask to filter traversable cells.</param>
    /// <param name="result">Buffer to store the path coordinates.</param>
    /// <returns>
    /// The number of coordinates in the path, or -1 if no path exists.
    /// Returns 0 if start equals end.
    /// </returns>
    public int FindPath(GridCoordinate start, GridCoordinate end, NavAreaMask areaMask, Span<GridCoordinate> result)
    {
        // Quick validation
        if (!grid.IsWalkable(start, areaMask) || !grid.IsWalkable(end, areaMask))
        {
            return -1;
        }

        if (start == end)
        {
            if (result.Length > 0)
            {
                result[0] = start;
                return 1;
            }

            return 0;
        }

        // Reset state
        Reset();

        // Add start node
        int startIndex = GetOrCreateNode(start);
        nodePool[startIndex].GCost = 0;
        nodePool[startIndex].HCost = CalculateHeuristic(start, end);
        nodePool[startIndex].State = NodeState.Open;
        openSet.Enqueue(startIndex, nodePool[startIndex].FCost);

        int iterations = 0;
        int maxIterations = config.MaxIterations > 0 ? config.MaxIterations : int.MaxValue;

        while (openSet.Count > 0 && iterations < maxIterations)
        {
            iterations++;

            int currentIndex = openSet.Dequeue();
            ref var currentNode = ref nodePool[currentIndex];

            // Skip if already closed (we may have duplicate entries in priority queue)
            if (currentNode.State == NodeState.Closed)
            {
                continue;
            }

            currentNode.State = NodeState.Closed;

            // Found the goal?
            if (currentNode.Coordinate == end)
            {
                return ReconstructPath(currentIndex, result);
            }

            // Explore neighbors
            int neighborCount = grid.GetWalkableNeighbors(
                currentNode.Coordinate,
                config.AllowDiagonal,
                areaMask,
                neighborBuffer);

            for (int i = 0; i < neighborCount; i++)
            {
                var neighbor = neighborBuffer[i];

                // Check if we've exceeded node pool capacity
                if (nodeCount >= nodePool.Length && !nodeIndices.ContainsKey(neighbor.Y * grid.Width + neighbor.X))
                {
                    // Can't expand further - path too complex for current limits
                    return -1;
                }

                int neighborIndex = GetOrCreateNode(neighbor);
                ref var neighborNode = ref nodePool[neighborIndex];

                if (neighborNode.State == NodeState.Closed)
                {
                    continue;
                }

                // Calculate tentative G cost
                bool isDiagonal = currentNode.Coordinate.X != neighbor.X &&
                                  currentNode.Coordinate.Y != neighbor.Y;
                float moveCost = isDiagonal ? DiagonalCost : CardinalCost;

                var areaType = grid.GetAreaType(neighbor);
                float areaCost = areaCosts[(int)areaType];
                if (areaCost <= 0f)
                {
                    continue; // Unwalkable due to area cost
                }

                float cellCost = grid.GetCost(neighbor);
                float tentativeG = currentNode.GCost + moveCost * cellCost * areaCost;

                if (tentativeG < neighborNode.GCost)
                {
                    neighborNode.ParentIndex = currentIndex;
                    neighborNode.GCost = tentativeG;
                    neighborNode.HCost = CalculateHeuristic(neighbor, end);
                    neighborNode.State = NodeState.Open;
                    openSet.Enqueue(neighborIndex, neighborNode.FCost);
                }
            }
        }

        // No path found (either no path exists or iteration/node limit reached)
        return -1;
    }

    /// <summary>
    /// Finds a path between two world positions.
    /// </summary>
    /// <param name="startWorld">The starting world position.</param>
    /// <param name="endWorld">The destination world position.</param>
    /// <param name="areaMask">Optional area mask.</param>
    /// <returns>A NavPath containing the computed path.</returns>
    public NavPath FindPath(Vector3 startWorld, Vector3 endWorld, NavAreaMask areaMask = NavAreaMask.All)
    {
        var start = grid.FromWorldPosition(startWorld);
        var end = grid.FromWorldPosition(endWorld);
        return FindPath(start, end, areaMask);
    }

    /// <summary>
    /// Checks if a path exists between two coordinates without computing the full path.
    /// </summary>
    /// <param name="start">The starting coordinate.</param>
    /// <param name="end">The destination coordinate.</param>
    /// <param name="areaMask">Optional area mask.</param>
    /// <returns>True if a path exists.</returns>
    public bool HasPath(GridCoordinate start, GridCoordinate end, NavAreaMask areaMask = NavAreaMask.All)
    {
        // Quick validation
        if (!grid.IsWalkable(start, areaMask) || !grid.IsWalkable(end, areaMask))
        {
            return false;
        }

        if (start == end)
        {
            return true;
        }

        // Use a reasonable buffer size for HasPath queries
        Span<GridCoordinate> buffer = stackalloc GridCoordinate[256];
        int result = FindPath(start, end, areaMask, buffer);
        // Result will be -1 if no path found
        // If buffer is too small, result is negative (negative path length)
        // Any value != -1 means path exists
        return result != -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float CalculateHeuristic(GridCoordinate from, GridCoordinate to)
    {
        return config.Heuristic switch
        {
            GridHeuristic.Manhattan => from.ManhattanDistance(to),
            GridHeuristic.Chebyshev => from.ChebyshevDistance(to),
            GridHeuristic.Euclidean => from.EuclideanDistance(to),
            GridHeuristic.Octile or _ => from.OctileDistance(to)
        };
    }

    private void Reset()
    {
        nodeCount = 0;
        nodeIndices.Clear();
        openSet.Clear();
    }

    private int GetOrCreateNode(GridCoordinate coord)
    {
        int key = coord.Y * grid.Width + coord.X;
        if (nodeIndices.TryGetValue(key, out int existingIndex))
        {
            return existingIndex;
        }

        if (nodeCount >= nodePool.Length)
        {
            throw new InvalidOperationException(
                $"Path search exceeded maximum node count of {nodePool.Length}. " +
                "Consider increasing MaxIterations or the path may be too long.");
        }

        int index = nodeCount++;
        nodePool[index] = new PathNode
        {
            Coordinate = coord,
            GCost = float.MaxValue,
            HCost = 0f,
            ParentIndex = -1,
            State = NodeState.Unvisited
        };

        nodeIndices[key] = index;
        return index;
    }

    private int ReconstructPath(int endIndex, Span<GridCoordinate> result)
    {
        // First, count path length by walking backwards
        int length = 0;
        int current = endIndex;
        while (current >= 0)
        {
            length++;
            current = nodePool[current].ParentIndex;
        }

        if (length > result.Length)
        {
            // Buffer too small - return the required length as negative
            return -length;
        }

        // Walk backwards and fill result in reverse
        current = endIndex;
        for (int i = length - 1; i >= 0; i--)
        {
            result[i] = nodePool[current].Coordinate;
            current = nodePool[current].ParentIndex;
        }

        return length;
    }

    private struct PathNode
    {
        public GridCoordinate Coordinate;
        public float GCost;     // Cost from start to this node
        public float HCost;     // Heuristic cost from this node to goal
        public int ParentIndex; // Index of parent node in pool
        public NodeState State;

        public readonly float FCost => GCost + HCost;
    }

    private enum NodeState : byte
    {
        Unvisited,
        Open,
        Closed
    }
}
