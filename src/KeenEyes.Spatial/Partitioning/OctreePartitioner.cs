using System.Buffers;
using System.Numerics;
using KeenEyes.Common;

namespace KeenEyes.Spatial.Partitioning;

/// <summary>
/// Octree-based spatial partitioning for 3D space.
/// </summary>
/// <remarks>
/// <para>
/// This implementation divides 3D space into a hierarchy of octants. When a node
/// exceeds the maximum entity threshold, it subdivides into eight children. This
/// provides better performance than grids for clustered entity distributions in 3D.
/// </para>
/// <para>
/// Performance characteristics:
/// - Insert/Update: O(log n) average, O(MaxDepth) worst case
/// - Remove: O(log n) average
/// - Query: O(log n + k) where k is result count
/// - Memory: O(n) where n is entity count
/// </para>
/// <para>
/// Best for clustered 3D distributions. For uniform distributions, grid partitioning
/// may be more efficient. For 2D space, consider quadtree partitioning.
/// </para>
/// </remarks>
internal sealed class OctreePartitioner : ISpatialPartitioner
{
    private readonly OctreeConfig config;
    private readonly OctreeNode root;

    // Track which node each entity belongs to for O(1) removal
    private readonly Dictionary<Entity, OctreeNode> entityNodes = [];

    // Track entity positions for redistribution during subdivision
    private readonly Dictionary<Entity, Vector3> entityPositions = [];

    private int entityCount;

    /// <summary>
    /// Creates a new octree partitioner with the specified configuration.
    /// </summary>
    /// <param name="config">The octree configuration.</param>
    /// <exception cref="ArgumentException">Thrown if configuration is invalid.</exception>
    public OctreePartitioner(OctreeConfig config)
    {
        var error = config.Validate();
        if (error != null)
        {
            throw new ArgumentException($"Invalid OctreeConfig: {error}", nameof(config));
        }

        this.config = config;

        // Create root node spanning the entire world bounds
        root = new OctreeNode(config.WorldMin, config.WorldMax, depth: 0);
    }

    /// <inheritdoc/>
    public int EntityCount => entityCount;

    /// <inheritdoc/>
    public void Update(Entity entity, Vector3 position)
    {
        UpdateInternal(entity, position, null);
    }

    /// <inheritdoc/>
    public void Update(Entity entity, Vector3 position, SpatialBounds bounds)
    {
        UpdateInternal(entity, position, (position + bounds.Min, position + bounds.Max));
    }

    /// <inheritdoc/>
    public void Remove(Entity entity)
    {
        if (!entityNodes.TryGetValue(entity, out var node))
        {
            return; // Not indexed
        }

        node.Entities.Remove(entity);
        entityNodes.Remove(entity);
        entityPositions.Remove(entity);
        entityCount--;
    }

    /// <inheritdoc/>
    public IEnumerable<Entity> QueryRadius(Vector3 center, float radius)
    {
        var radiusVec = new Vector3(radius, radius, radius);
        var min = center - radiusVec;
        var max = center + radiusVec;

        var results = new HashSet<Entity>();
        root.QueryBounds(min, max, results, entityPositions);
        return MaybeSortResults(results);
    }

    /// <inheritdoc/>
    public IEnumerable<Entity> QueryBounds(Vector3 min, Vector3 max)
    {
        var results = new HashSet<Entity>();
        root.QueryBounds(min, max, results, entityPositions);
        return MaybeSortResults(results);
    }

    /// <inheritdoc/>
    public IEnumerable<Entity> QueryPoint(Vector3 point)
    {
        var node = root.FindLeafNode(point);

        // Return all entities in the leaf node (broadphase query)
        var entities = node?.Entities.ToList() ?? Enumerable.Empty<Entity>();
        return MaybeSortResults(entities);
    }

    /// <inheritdoc/>
    public IEnumerable<Entity> QueryFrustum(Frustum frustum)
    {
        var results = new HashSet<Entity>();
        root.QueryFrustum(frustum, results, entityPositions);
        return MaybeSortResults(results);
    }

    /// <inheritdoc/>
    public void Clear()
    {
        root.Clear();
        entityNodes.Clear();
        entityPositions.Clear();
        entityCount = 0;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Clear();
    }

    /// <summary>
    /// Returns query results, sorted by entity ID if deterministic mode is enabled.
    /// </summary>
    private IEnumerable<Entity> MaybeSortResults(IEnumerable<Entity> results)
    {
        if (!config.DeterministicMode)
        {
            return results;
        }

        // Sort by entity ID for deterministic ordering
        return results.OrderBy(e => e.Id).ThenBy(e => e.Version);
    }

    /// <summary>
    /// Internal update logic that handles both point and bounded entities.
    /// </summary>
    private void UpdateInternal(Entity entity, Vector3 position, (Vector3 min, Vector3 max)? bounds)
    {
        // Store entity position for potential redistribution during subdivision
        entityPositions[entity] = position;

        // Check if entity is already indexed and still fits in its current node
        if (entityNodes.TryGetValue(entity, out var oldNode))
        {
            // For loose octrees, check if entity is still within loose bounds
            if (config.UseLooseBounds && oldNode.ContainsPointLoose(position, config.LoosenessFactor))
            {
                // Entity is still within loose bounds, no need to move
                return;
            }

            // Entity needs to move - remove from old node
            oldNode.Entities.Remove(entity);
        }
        else
        {
            entityCount++;
        }

        // Find the appropriate node for this entity
        var targetNode = root.FindLeafNode(position);

        // Insert into the target node
        targetNode.Entities.Add(entity);
        entityNodes[entity] = targetNode;

        // Check if node needs to subdivide
        if (targetNode.Entities.Count > config.MaxEntitiesPerNode &&
            targetNode.Depth < config.MaxDepth &&
            !targetNode.IsSubdivided)
        {
            Subdivide(targetNode);
        }
    }

    /// <summary>
    /// Subdivides a node into eight children and redistributes entities.
    /// </summary>
    private void Subdivide(OctreeNode node)
    {
        var mid = (node.Min + node.Max) * 0.5f;
        var childDepth = node.Depth + 1;

        // Create eight child nodes (octants)
        node.Children = [
            // Bottom four (z < mid)
            new OctreeNode(node.Min, mid, childDepth),                                                    // 0: ---
            new OctreeNode(new Vector3(mid.X, node.Min.Y, node.Min.Z), new Vector3(node.Max.X, mid.Y, mid.Z), childDepth), // 1: +--
            new OctreeNode(new Vector3(node.Min.X, mid.Y, node.Min.Z), new Vector3(mid.X, node.Max.Y, mid.Z), childDepth), // 2: -+-
            new OctreeNode(new Vector3(mid.X, mid.Y, node.Min.Z), new Vector3(node.Max.X, node.Max.Y, mid.Z), childDepth), // 3: ++-
            // Top four (z >= mid)
            new OctreeNode(new Vector3(node.Min.X, node.Min.Y, mid.Z), new Vector3(mid.X, mid.Y, node.Max.Z), childDepth), // 4: --+
            new OctreeNode(new Vector3(mid.X, node.Min.Y, mid.Z), new Vector3(node.Max.X, mid.Y, node.Max.Z), childDepth), // 5: +-+
            new OctreeNode(new Vector3(node.Min.X, mid.Y, mid.Z), new Vector3(mid.X, node.Max.Y, node.Max.Z), childDepth), // 6: -++
            new OctreeNode(mid, node.Max, childDepth)                                                     // 7: +++
        ];

        // Redistribute entities to children
        var entitiesToRedistribute = node.Entities.ToList();
        node.Entities.Clear();

        foreach (var entity in entitiesToRedistribute)
        {
            if (entityPositions.TryGetValue(entity, out var position))
            {
                // Find the appropriate child for this entity
                var childNode = node.FindLeafNode(position);
                childNode.Entities.Add(entity);
                entityNodes[entity] = childNode;
            }
        }
    }

    /// <summary>
    /// Represents a node in the octree.
    /// </summary>
    private sealed class OctreeNode(Vector3 min, Vector3 max, int depth)
    {
        public Vector3 Min { get; } = min;
        public Vector3 Max { get; } = max;
        public int Depth { get; } = depth;
        public HashSet<Entity> Entities { get; } = [];
        public OctreeNode[]? Children { get; set; }
        public bool IsSubdivided => Children != null;

        /// <summary>
        /// Checks if a point is within the loose bounds of this node.
        /// </summary>
        public bool ContainsPointLoose(Vector3 point, float loosenessFactor)
        {
            var expansion = ((Max - Min) * (loosenessFactor - 1.0f)) * 0.5f;
            var looseMin = Min - expansion;
            var looseMax = Max + expansion;
            return point.X >= looseMin.X && point.X <= looseMax.X &&
                   point.Y >= looseMin.Y && point.Y <= looseMax.Y &&
                   point.Z >= looseMin.Z && point.Z <= looseMax.Z;
        }

        /// <summary>
        /// Finds the leaf node that should contain the given point.
        /// </summary>
        public OctreeNode FindLeafNode(Vector3 point)
        {
            if (!IsSubdivided)
            {
                return this;
            }

            // Determine which child contains the point
            var mid = (Min + Max) * 0.5f;

            var childIndex = (point.X >= mid.X ? 1 : 0) +
                           (point.Y >= mid.Y ? 2 : 0) +
                           (point.Z >= mid.Z ? 4 : 0);

            return Children![childIndex].FindLeafNode(point);
        }

        /// <summary>
        /// Queries all entities within the given bounds, recursively traversing children.
        /// </summary>
        public void QueryBounds(Vector3 min, Vector3 max, HashSet<Entity> results, Dictionary<Entity, Vector3> entityPositions)
        {
            // Check if this node intersects the query bounds
            if (!Intersects(min, max))
            {
                return;
            }

            // Add entities from this node that are actually within bounds
            // Use SIMD for bulk filtering when there are enough entities (threshold: 16)
            var count = Entities.Count;
            if (count >= 16 && count <= 128)
            {
                // Stack allocation for small-medium arrays (zero heap allocation)
                Span<Entity> entitySpan = stackalloc Entity[count];
                Span<Vector3> positionSpan = stackalloc Vector3[count];

                // Copy entities to span
                int idx = 0;
                foreach (var entity in Entities)
                {
                    entitySpan[idx++] = entity;
                }

                // Extract positions
                for (int i = 0; i < count; i++)
                {
                    if (entityPositions.TryGetValue(entitySpan[i], out var pos))
                    {
                        positionSpan[i] = pos;
                    }
                }

                // SIMD-accelerated AABB filtering (zero-allocation with stackalloc)
                Span<int> indices = stackalloc int[count];
                int matchCount = SimdHelpers.FilterByAABBSIMD(positionSpan, min, max, indices);

                // Add filtered entities
                for (int i = 0; i < matchCount; i++)
                {
                    results.Add(entitySpan[indices[i]]);
                }
            }
            else if (count > 128)
            {
                // ArrayPool for large arrays (reusable, zero allocation amortized)
                var rentedEntities = ArrayPool<Entity>.Shared.Rent(count);
                var rentedPositions = ArrayPool<Vector3>.Shared.Rent(count);
                var rentedIndices = ArrayPool<int>.Shared.Rent(count);

                try
                {
                    Entities.CopyTo(rentedEntities, 0);
                    var entitySpan = rentedEntities.AsSpan(0, count);
                    var positionSpan = rentedPositions.AsSpan(0, count);
                    var indicesSpan = rentedIndices.AsSpan(0, count);

                    // Extract positions
                    for (int i = 0; i < count; i++)
                    {
                        if (entityPositions.TryGetValue(entitySpan[i], out var pos))
                        {
                            positionSpan[i] = pos;
                        }
                    }

                    // SIMD-accelerated AABB filtering (zero-allocation with pooled arrays)
                    int matchCount = SimdHelpers.FilterByAABBSIMD(positionSpan, min, max, indicesSpan);

                    // Add filtered entities
                    for (int i = 0; i < matchCount; i++)
                    {
                        results.Add(entitySpan[indicesSpan[i]]);
                    }
                }
                finally
                {
                    ArrayPool<Entity>.Shared.Return(rentedEntities);
                    ArrayPool<Vector3>.Shared.Return(rentedPositions);
                    ArrayPool<int>.Shared.Return(rentedIndices);
                }
            }
            else
            {
                // Scalar path for small entity counts (< 16)
                foreach (var entity in Entities)
                {
                    if (entityPositions.TryGetValue(entity, out var pos) &&
                        pos.X >= min.X && pos.X <= max.X &&
                        pos.Y >= min.Y && pos.Y <= max.Y &&
                        pos.Z >= min.Z && pos.Z <= max.Z)
                    {
                        results.Add(entity);
                    }
                }
            }

            // Recursively query children if subdivided
            if (IsSubdivided)
            {
                foreach (var child in Children!)
                {
                    child.QueryBounds(min, max, results, entityPositions);
                }
            }
        }

        /// <summary>
        /// Queries entities within a frustum (broadphase).
        /// </summary>
        public void QueryFrustum(Frustum frustum, HashSet<Entity> results, Dictionary<Entity, Vector3> entityPositions)
        {
            // Early out if node doesn't intersect frustum
            if (!frustum.Intersects(Min, Max))
            {
                return;
            }

            // Add entities from this node that are within frustum
            foreach (var entity in Entities)
            {
                if (entityPositions.TryGetValue(entity, out var pos))
                {
                    if (frustum.Contains(pos))
                    {
                        results.Add(entity);
                    }
                }
            }

            // Recursively query children if subdivided
            if (IsSubdivided)
            {
                foreach (var child in Children!)
                {
                    child.QueryFrustum(frustum, results, entityPositions);
                }
            }
        }

        /// <summary>
        /// Checks if this node's bounds intersect with the given bounds.
        /// </summary>
        private bool Intersects(Vector3 min, Vector3 max)
        {
            return !(Max.X < min.X || Min.X > max.X ||
                    Max.Y < min.Y || Min.Y > max.Y ||
                    Max.Z < min.Z || Min.Z > max.Z);
        }

        /// <summary>
        /// Clears all entities and children from this node.
        /// </summary>
        public void Clear()
        {
            Entities.Clear();

            if (IsSubdivided)
            {
                foreach (var child in Children!)
                {
                    child.Clear();
                }
                Children = null;
            }
        }
    }
}
