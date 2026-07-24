using System.Buffers;
using System.Numerics;
using KeenEyes.Common;

namespace KeenEyes.Spatial.Partitioning;

/// <summary>
/// Quadtree-based spatial partitioning for 2D space.
/// </summary>
/// <remarks>
/// <para>
/// This implementation divides 2D space (X-Z plane) into a hierarchy of quadrants.
/// When a node exceeds the maximum entity threshold, it subdivides into four children
/// (Northwest, Northeast, Southwest, Southeast). This provides better performance than
/// grids for clustered entity distributions.
/// </para>
/// <para>
/// Performance characteristics:
/// - Insert/Update: O(log n) average, O(MaxDepth) worst case
/// - Remove: O(log n) average
/// - Query: O(log n + k) where k is result count
/// - Memory: O(n) where n is entity count
/// </para>
/// <para>
/// Best for clustered 2D distributions. For uniform distributions, grid partitioning
/// may be more efficient. For 3D space, consider octree partitioning.
/// </para>
/// </remarks>
internal sealed class QuadtreePartitioner : ISpatialPartitioner
{
    private readonly QuadtreeConfig config;
    private readonly QuadtreeNode root;

    // Track which node each entity belongs to for O(1) removal
    private readonly Dictionary<Entity, QuadtreeNode> entityNodes = [];

    // Track entity positions for redistribution during subdivision
    private readonly Dictionary<Entity, Vector2> entityPositions = [];

    // Track each entity's world-space AABB (X-Z plane) so bounded queries can
    // test against the entity's footprint rather than only its center point.
    private readonly Dictionary<Entity, (Vector2 Min, Vector2 Max)> entityBounds = [];

    // Pool for child node arrays (4 elements for quadtree)
    private readonly ArrayPool<QuadtreeNode>? nodePool;

    private int entityCount;

    /// <summary>
    /// Creates a new quadtree partitioner with the specified configuration.
    /// </summary>
    /// <param name="config">The quadtree configuration.</param>
    /// <exception cref="ArgumentException">Thrown if configuration is invalid.</exception>
    public QuadtreePartitioner(QuadtreeConfig config)
    {
        var error = config.Validate();
        if (error != null)
        {
            throw new ArgumentException($"Invalid QuadtreeConfig: {error}", nameof(config));
        }

        this.config = config;

        // Initialize node pool if pooling is enabled
        nodePool = config.UseNodePooling ? ArrayPool<QuadtreeNode>.Shared : null;

        // Create root node spanning the entire world bounds
        root = new QuadtreeNode(
            new Vector2(config.WorldMin.X, config.WorldMin.Z),
            new Vector2(config.WorldMax.X, config.WorldMax.Z),
            depth: 0);
    }

    /// <inheritdoc/>
    public int EntityCount => entityCount;

    // The looseness factor to expand node bounds by during query traversal.
    // Entities are stored in a node while they remain within its loose bounds,
    // so traversal must prune against those same loose bounds (a factor of 1.0
    // leaves node bounds unchanged for tight quadtrees).
    private float QueryLoosenessFactor => config.UseLooseBounds ? config.LoosenessFactor : 1.0f;

    /// <inheritdoc/>
    public void Update(Entity entity, Vector3 position)
    {
        var point = new Vector2(position.X, position.Z);
        UpdateInternal(entity, point, null);
    }

    /// <inheritdoc/>
    public void Update(Entity entity, Vector3 position, SpatialBounds bounds)
    {
        var point = new Vector2(position.X, position.Z);
        var min = new Vector2(position.X + bounds.Min.X, position.Z + bounds.Min.Z);
        var max = new Vector2(position.X + bounds.Max.X, position.Z + bounds.Max.Z);
        UpdateInternal(entity, point, (min, max));
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
        entityBounds.Remove(entity);
        entityCount--;
    }

    /// <inheritdoc/>
    public IEnumerable<Entity> QueryRadius(Vector3 center, float radius)
    {
        var min = new Vector2(center.X - radius, center.Z - radius);
        var max = new Vector2(center.X + radius, center.Z + radius);

        var results = new HashSet<Entity>();
        root.QueryBounds(min, max, results, entityBounds, QueryLoosenessFactor);
        return MaybeSortResults(results);
    }

    /// <inheritdoc/>
    public IEnumerable<Entity> QueryBounds(Vector3 min, Vector3 max)
    {
        var min2D = new Vector2(min.X, min.Z);
        var max2D = new Vector2(max.X, max.Z);

        var results = new HashSet<Entity>();
        root.QueryBounds(min2D, max2D, results, entityBounds, QueryLoosenessFactor);
        return MaybeSortResults(results);
    }

    /// <inheritdoc/>
    public IEnumerable<Entity> QueryPoint(Vector3 point)
    {
        var point2D = new Vector2(point.X, point.Z);
        var node = root.FindLeafNode(point2D);

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
    public int QueryRadius(Vector3 center, float radius, Span<Entity> results)
    {
        var min = new Vector2(center.X - radius, center.Z - radius);
        var max = new Vector2(center.X + radius, center.Z + radius);

        int count = 0;
        bool overflow = false;
        root.QueryBoundsSpan(min, max, results, ref count, ref overflow, entityBounds, QueryLoosenessFactor);

        if (config.DeterministicMode && count > 0)
        {
            SortSpan(results[..count]);
        }

        return overflow ? -1 : count;
    }

    /// <inheritdoc/>
    public int QueryBounds(Vector3 min, Vector3 max, Span<Entity> results)
    {
        var min2D = new Vector2(min.X, min.Z);
        var max2D = new Vector2(max.X, max.Z);

        int count = 0;
        bool overflow = false;
        root.QueryBoundsSpan(min2D, max2D, results, ref count, ref overflow, entityBounds, QueryLoosenessFactor);

        if (config.DeterministicMode && count > 0)
        {
            SortSpan(results[..count]);
        }

        return overflow ? -1 : count;
    }

    /// <inheritdoc/>
    public int QueryPoint(Vector3 point, Span<Entity> results)
    {
        var point2D = new Vector2(point.X, point.Z);
        var node = root.FindLeafNode(point2D);

        if (node == null)
        {
            return 0;
        }

        int count = 0;
        bool overflow = false;

        foreach (var entity in node.Entities)
        {
            if (count < results.Length)
            {
                results[count++] = entity;
            }
            else
            {
                overflow = true;
                break;
            }
        }

        if (config.DeterministicMode && count > 0)
        {
            SortSpan(results[..count]);
        }

        return overflow ? -1 : count;
    }

    /// <inheritdoc/>
    public int QueryFrustum(Frustum frustum, Span<Entity> results)
    {
        int count = 0;
        bool overflow = false;
        root.QueryFrustumSpan(frustum, results, ref count, ref overflow, entityPositions);

        if (config.DeterministicMode && count > 0)
        {
            SortSpan(results[..count]);
        }

        return overflow ? -1 : count;
    }

    /// <summary>
    /// Sorts a span of entities by ID and version for deterministic ordering.
    /// </summary>
    private static void SortSpan(Span<Entity> span)
    {
        for (int i = 1; i < span.Length; i++)
        {
            var key = span[i];
            int j = i - 1;

            while (j >= 0 && CompareEntities(span[j], key) > 0)
            {
                span[j + 1] = span[j];
                j--;
            }
            span[j + 1] = key;
        }
    }

    /// <summary>
    /// Compares two entities for sorting (by ID, then version).
    /// </summary>
    private static int CompareEntities(Entity a, Entity b)
    {
        int idCompare = a.Id.CompareTo(b.Id);
        return idCompare != 0 ? idCompare : a.Version.CompareTo(b.Version);
    }

    /// <inheritdoc/>
    public void Clear()
    {
        root.Clear(nodePool);
        entityNodes.Clear();
        entityPositions.Clear();
        entityBounds.Clear();
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
    private void UpdateInternal(Entity entity, Vector2 position, (Vector2 min, Vector2 max)? bounds)
    {
        // Store entity position for potential redistribution during subdivision
        entityPositions[entity] = position;

        // Store the entity's world-space AABB. Point entities (no bounds) get a
        // degenerate box collapsed onto their position so queries can treat every
        // entity uniformly as bounds instead of a bare center point.
        entityBounds[entity] = bounds ?? (position, position);

        // Check if entity is already indexed and still fits in its current node
        if (entityNodes.TryGetValue(entity, out var oldNode))
        {
            // For loose quadtrees, check if entity is still within loose bounds
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
    /// Subdivides a node into four children and redistributes entities.
    /// </summary>
    private void Subdivide(QuadtreeNode node)
    {
        var midX = (node.Min.X + node.Max.X) * 0.5f;
        var midZ = (node.Min.Y + node.Max.Y) * 0.5f;
        var childDepth = node.Depth + 1;

        // Create four child nodes (NW, NE, SW, SE)
        QuadtreeNode[] children;
        if (nodePool != null)
        {
            // Rent from pool and manually initialize each element
            children = nodePool.Rent(4);
            children[0] = new QuadtreeNode(node.Min, new Vector2(midX, midZ), childDepth);                    // NW
            children[1] = new QuadtreeNode(new Vector2(midX, node.Min.Y), new Vector2(node.Max.X, midZ), childDepth); // NE
            children[2] = new QuadtreeNode(new Vector2(node.Min.X, midZ), new Vector2(midX, node.Max.Y), childDepth); // SW
            children[3] = new QuadtreeNode(new Vector2(midX, midZ), node.Max, childDepth);                     // SE
            node.Children = children;
            node.IsPooled = true;
        }
        else
        {
            // Direct allocation (no pooling)
            node.Children = [
                new QuadtreeNode(node.Min, new Vector2(midX, midZ), childDepth),                    // NW
                new QuadtreeNode(new Vector2(midX, node.Min.Y), new Vector2(node.Max.X, midZ), childDepth), // NE
                new QuadtreeNode(new Vector2(node.Min.X, midZ), new Vector2(midX, node.Max.Y), childDepth), // SW
                new QuadtreeNode(new Vector2(midX, midZ), node.Max, childDepth)                     // SE
            ];
            node.IsPooled = false;
        }

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
    /// Represents a node in the quadtree.
    /// </summary>
    private sealed class QuadtreeNode(Vector2 min, Vector2 max, int depth)
    {
        public Vector2 Min { get; } = min;
        public Vector2 Max { get; } = max;
        public int Depth { get; } = depth;
        public HashSet<Entity> Entities { get; } = [];
        public QuadtreeNode[]? Children { get; set; }
        public bool IsPooled { get; set; }
        public bool IsSubdivided => Children != null;

        /// <summary>
        /// Checks if a point is within the loose bounds of this node.
        /// </summary>
        public bool ContainsPointLoose(Vector2 point, float loosenessFactor)
        {
            var expansion = ((Max - Min) * (loosenessFactor - 1.0f)) * 0.5f;
            var looseMin = Min - expansion;
            var looseMax = Max + expansion;
            return point.X >= looseMin.X && point.X <= looseMax.X &&
                   point.Y >= looseMin.Y && point.Y <= looseMax.Y;
        }

        /// <summary>
        /// Finds the leaf node that should contain the given point.
        /// </summary>
        public QuadtreeNode FindLeafNode(Vector2 point)
        {
            if (!IsSubdivided)
            {
                return this;
            }

            // Determine which child contains the point
            var midX = (Min.X + Max.X) * 0.5f;
            var midZ = (Min.Y + Max.Y) * 0.5f;

            var childIndex = (point.X >= midX ? 1 : 0) + (point.Y >= midZ ? 2 : 0);
            return Children![childIndex].FindLeafNode(point);
        }

        /// <summary>
        /// Queries all entities within the given bounds, recursively traversing children.
        /// </summary>
        public void QueryBounds(Vector2 min, Vector2 max, HashSet<Entity> results, Dictionary<Entity, (Vector2 Min, Vector2 Max)> entityBounds, float loosenessFactor)
        {
            // Check if this node (expanded by the looseness factor) intersects the
            // query bounds. Entities are stored while within loose bounds, so pruning
            // must honour those same loose bounds to avoid false negatives.
            if (!Intersects(min, max, loosenessFactor))
            {
                return;
            }

            // Add entities from this node whose footprint (AABB) overlaps the query
            // box. Testing the stored bounds rather than the center point keeps large
            // entities visible when the query box lies inside their footprint.
            foreach (var entity in Entities)
            {
                if (entityBounds.TryGetValue(entity, out var b) &&
                    b.Max.X >= min.X && b.Min.X <= max.X &&
                    b.Max.Y >= min.Y && b.Min.Y <= max.Y)
                {
                    results.Add(entity);
                }
            }

            // Recursively query children if subdivided
            if (IsSubdivided)
            {
                // Only iterate over the first 4 children (pooled arrays may be larger)
                for (int i = 0; i < 4; i++)
                {
                    Children![i].QueryBounds(min, max, results, entityBounds, loosenessFactor);
                }
            }
        }

        /// <summary>
        /// Queries entities within a frustum (broadphase).
        /// </summary>
        public void QueryFrustum(Frustum frustum, HashSet<Entity> results, Dictionary<Entity, Vector2> entityPositions)
        {
            // Convert 2D quadtree bounds to 3D AABB for frustum testing
            // Use Y range that covers reasonable height values
            var min3D = new Vector3(Min.X, -1000, Min.Y);
            var max3D = new Vector3(Max.X, 1000, Max.Y);

            // Early out if node doesn't intersect frustum
            if (!frustum.Intersects(min3D, max3D))
            {
                return;
            }

            // Add entities from this node that are within frustum
            foreach (var entity in Entities)
            {
                if (entityPositions.TryGetValue(entity, out var pos2D))
                {
                    var pos3D = new Vector3(pos2D.X, 0, pos2D.Y);
                    if (frustum.Contains(pos3D))
                    {
                        results.Add(entity);
                    }
                }
            }

            // Recursively query children if subdivided
            if (IsSubdivided)
            {
                // Only iterate over the first 4 children (pooled arrays may be larger)
                for (int i = 0; i < 4; i++)
                {
                    Children![i].QueryFrustum(frustum, results, entityPositions);
                }
            }
        }

        /// <summary>
        /// Queries all entities within the given bounds into a span (zero-allocation).
        /// </summary>
        public void QueryBoundsSpan(Vector2 min, Vector2 max, Span<Entity> results, ref int count, ref bool overflow, Dictionary<Entity, (Vector2 Min, Vector2 Max)> entityBounds, float loosenessFactor)
        {
            // Check if this node (expanded by the looseness factor) intersects the query bounds
            if (!Intersects(min, max, loosenessFactor))
            {
                return;
            }

            // Add entities from this node whose footprint (AABB) overlaps the query box
            foreach (var entity in Entities)
            {
                if (entityBounds.TryGetValue(entity, out var b) &&
                    b.Max.X >= min.X && b.Min.X <= max.X &&
                    b.Max.Y >= min.Y && b.Min.Y <= max.Y &&
                    !SpanContainsEntity(results, count, entity))
                {
                    if (count < results.Length)
                    {
                        results[count++] = entity;
                    }
                    else
                    {
                        overflow = true;
                    }
                }
            }

            // Recursively query children if subdivided
            if (IsSubdivided)
            {
                for (int i = 0; i < 4; i++)
                {
                    Children![i].QueryBoundsSpan(min, max, results, ref count, ref overflow, entityBounds, loosenessFactor);
                }
            }
        }

        /// <summary>
        /// Queries entities within a frustum into a span (zero-allocation).
        /// </summary>
        public void QueryFrustumSpan(Frustum frustum, Span<Entity> results, ref int count, ref bool overflow, Dictionary<Entity, Vector2> entityPositions)
        {
            // Convert 2D quadtree bounds to 3D AABB for frustum testing
            var min3D = new Vector3(Min.X, -1000, Min.Y);
            var max3D = new Vector3(Max.X, 1000, Max.Y);

            // Early out if node doesn't intersect frustum
            if (!frustum.Intersects(min3D, max3D))
            {
                return;
            }

            // Add entities from this node that are within frustum
            foreach (var entity in Entities)
            {
                if (entityPositions.TryGetValue(entity, out var pos2D))
                {
                    var pos3D = new Vector3(pos2D.X, 0, pos2D.Y);
                    if (frustum.Contains(pos3D) && !SpanContainsEntity(results, count, entity))
                    {
                        if (count < results.Length)
                        {
                            results[count++] = entity;
                        }
                        else
                        {
                            overflow = true;
                        }
                    }
                }
            }

            // Recursively query children if subdivided
            if (IsSubdivided)
            {
                for (int i = 0; i < 4; i++)
                {
                    Children![i].QueryFrustumSpan(frustum, results, ref count, ref overflow, entityPositions);
                }
            }
        }

        /// <summary>
        /// Checks if the span already contains an entity.
        /// </summary>
        private static bool SpanContainsEntity(Span<Entity> span, int count, Entity entity)
        {
            for (int i = 0; i < count; i++)
            {
                if (span[i].Id == entity.Id && span[i].Version == entity.Version)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if this node's bounds, expanded by the looseness factor, intersect
        /// with the given bounds. A factor of 1.0 tests the strict node bounds.
        /// </summary>
        private bool Intersects(Vector2 min, Vector2 max, float loosenessFactor)
        {
            var expansion = ((Max - Min) * (loosenessFactor - 1.0f)) * 0.5f;
            var looseMin = Min - expansion;
            var looseMax = Max + expansion;
            return !(looseMax.X < min.X || looseMin.X > max.X || looseMax.Y < min.Y || looseMin.Y > max.Y);
        }

        /// <summary>
        /// Clears all entities and children from this node.
        /// </summary>
        public void Clear(ArrayPool<QuadtreeNode>? pool)
        {
            Entities.Clear();

            if (IsSubdivided)
            {
                // Recursively clear children first (only first 4 elements for quadtree)
                for (int i = 0; i < 4; i++)
                {
                    Children![i].Clear(pool);
                }

                // Return pooled array to pool
                if (IsPooled && pool != null)
                {
                    pool.Return(Children!, clearArray: false);
                }

                Children = null;
                IsPooled = false;
            }
        }
    }
}
