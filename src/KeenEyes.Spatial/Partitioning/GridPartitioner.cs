using System.Numerics;
using KeenEyes.Common;

namespace KeenEyes.Spatial.Partitioning;

/// <summary>
/// Grid-based spatial partitioning using spatial hashing.
/// </summary>
/// <remarks>
/// <para>
/// This implementation divides 3D space into a uniform grid of cells. Each entity
/// is placed in one or more cells based on its position and bounds. Queries check
/// only the cells that intersect the query region, providing O(1) cell lookup and
/// efficient spatial queries.
/// </para>
/// <para>
/// Performance characteristics:
/// - Cell lookup: O(1)
/// - Insert/Update: O(cells_occupied) - typically 1-8 cells per entity
/// - Remove: O(cells_occupied)
/// - Query: O(cells_queried × entities_per_cell)
/// - Memory: O(occupied_cells + total_entities)
/// </para>
/// <para>
/// Best for uniformly distributed entities. For clustered distributions, consider
/// quadtree or octree strategies (Phase 2).
/// </para>
/// </remarks>
internal sealed class GridPartitioner : ISpatialPartitioner
{
    private readonly float cellSize;
    private readonly float invCellSize; // 1 / cellSize for faster division

    // Grid storage: cell coordinates → entities in that cell
    private readonly Dictionary<(int x, int y, int z), HashSet<Entity>> grid = [];

    // Entity tracking: entity → list of cells it occupies
    private readonly Dictionary<Entity, List<(int x, int y, int z)>> entityCells = [];

    // Entity count
    private int entityCount;

    /// <summary>
    /// Creates a new grid partitioner with the specified configuration.
    /// </summary>
    /// <param name="config">The grid configuration.</param>
    /// <exception cref="ArgumentException">Thrown if configuration is invalid.</exception>
    public GridPartitioner(GridConfig config)
    {
        var error = config.Validate();
        if (error != null)
        {
            throw new ArgumentException($"Invalid GridConfig: {error}", nameof(config));
        }

        cellSize = config.CellSize;
        invCellSize = 1f / cellSize;
    }

    /// <inheritdoc/>
    public int EntityCount => entityCount;

    /// <inheritdoc/>
    public void Update(Entity entity, Vector3 position)
    {
        // Point-based entity (no bounds)
        var cell = GetCell(position);
        UpdateEntityCells(entity, new[] { cell });
    }

    /// <inheritdoc/>
    public void Update(Entity entity, Vector3 position, SpatialBounds bounds)
    {
        // AABB-based entity - calculate all cells it occupies
        var minCell = GetCell(position + bounds.Min);
        var maxCell = GetCell(position + bounds.Max);

        var cells = new List<(int x, int y, int z)>();
        for (int x = minCell.x; x <= maxCell.x; x++)
        {
            for (int y = minCell.y; y <= maxCell.y; y++)
            {
                for (int z = minCell.z; z <= maxCell.z; z++)
                {
                    cells.Add((x, y, z));
                }
            }
        }

        UpdateEntityCells(entity, cells);
    }

    /// <inheritdoc/>
    public void Remove(Entity entity)
    {
        if (!entityCells.TryGetValue(entity, out var cells))
        {
            return; // Not indexed
        }

        // Remove from all cells
        foreach (var cell in cells)
        {
            if (grid.TryGetValue(cell, out var entitiesInCell))
            {
                entitiesInCell.Remove(entity);

                // Clean up empty cells to save memory
                if (entitiesInCell.Count == 0)
                {
                    grid.Remove(cell);
                }
            }
        }

        entityCells.Remove(entity);
        entityCount--;
    }

    /// <inheritdoc/>
    public IEnumerable<Entity> QueryRadius(Vector3 center, float radius)
    {
        // Calculate bounding box of the sphere
        var radiusVec = new Vector3(radius, radius, radius);
        var min = center - radiusVec;
        var max = center + radiusVec;

        var minCell = GetCell(min);
        var maxCell = GetCell(max);

        // Use HashSet to deduplicate entities that appear in multiple cells
        var results = new HashSet<Entity>();

        for (int x = minCell.x; x <= maxCell.x; x++)
        {
            for (int y = minCell.y; y <= maxCell.y; y++)
            {
                for (int z = minCell.z; z <= maxCell.z; z++)
                {
                    if (grid.TryGetValue((x, y, z), out var entitiesInCell))
                    {
                        foreach (var entity in entitiesInCell)
                        {
                            // Broadphase: add all entities in intersecting cells
                            // Note: This may include false positives at cell boundaries
                            // Callers should perform exact distance checks if needed
                            results.Add(entity);
                        }
                    }
                }
            }
        }

        return results;
    }

    /// <inheritdoc/>
    public IEnumerable<Entity> QueryBounds(Vector3 min, Vector3 max)
    {
        var minCell = GetCell(min);
        var maxCell = GetCell(max);

        // Use HashSet to deduplicate entities that appear in multiple cells
        var results = new HashSet<Entity>();

        for (int x = minCell.x; x <= maxCell.x; x++)
        {
            for (int y = minCell.y; y <= maxCell.y; y++)
            {
                for (int z = minCell.z; z <= maxCell.z; z++)
                {
                    if (grid.TryGetValue((x, y, z), out var entitiesInCell))
                    {
                        foreach (var entity in entitiesInCell)
                        {
                            results.Add(entity);
                        }
                    }
                }
            }
        }

        return results;
    }

    /// <inheritdoc/>
    public IEnumerable<Entity> QueryPoint(Vector3 point)
    {
        var cell = GetCell(point);

        if (grid.TryGetValue(cell, out var entitiesInCell))
        {
            return entitiesInCell.ToList(); // Return copy to avoid collection modification issues
        }

        return Enumerable.Empty<Entity>();
    }

    /// <inheritdoc/>
    public IEnumerable<Entity> QueryFrustum(Frustum frustum)
    {
        // For grid partitioner, we approximate the frustum with an AABB
        // by testing the 8 corners of each cell against the frustum
        var results = new HashSet<Entity>();

        // Iterate all cells (this is inefficient but correct for grid)
        // A better approach would be to calculate frustum bounds first
        foreach (var kvp in grid)
        {
            var (x, y, z) = kvp.Key;
            var cellMin = new Vector3(x * cellSize, y * cellSize, z * cellSize);
            var cellMax = cellMin + new Vector3(cellSize, cellSize, cellSize);

            // Test if cell AABB intersects frustum
            if (frustum.Intersects(cellMin, cellMax))
            {
                foreach (var entity in kvp.Value)
                {
                    results.Add(entity);
                }
            }
        }

        return results;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        grid.Clear();
        entityCells.Clear();
        entityCount = 0;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Clear();
    }

    /// <summary>
    /// Converts a world position to grid cell coordinates.
    /// </summary>
    private (int x, int y, int z) GetCell(Vector3 position)
    {
        return (
            (int)MathF.Floor(position.X * invCellSize),
            (int)MathF.Floor(position.Y * invCellSize),
            (int)MathF.Floor(position.Z * invCellSize)
        );
    }

    /// <summary>
    /// Updates the cells occupied by an entity.
    /// </summary>
    private void UpdateEntityCells(Entity entity, IEnumerable<(int x, int y, int z)> newCells)
    {
        var newCellsList = newCells.ToList();

        // If entity already indexed, remove from old cells first
        if (entityCells.TryGetValue(entity, out var oldCells))
        {
            // Remove from cells it no longer occupies
            foreach (var oldCell in oldCells)
            {
                if (!newCellsList.Contains(oldCell))
                {
                    if (grid.TryGetValue(oldCell, out var entitiesInCell))
                    {
                        entitiesInCell.Remove(entity);
                        if (entitiesInCell.Count == 0)
                        {
                            grid.Remove(oldCell);
                        }
                    }
                }
            }
        }
        else
        {
            // New entity
            entityCount++;
        }

        // Add to new cells
        foreach (var newCell in newCellsList)
        {
            if (!grid.TryGetValue(newCell, out var entitiesInCell))
            {
                entitiesInCell = [];
                grid[newCell] = entitiesInCell;
            }
            entitiesInCell.Add(entity);
        }

        // Update entity's cell list
        entityCells[entity] = newCellsList;
    }
}
