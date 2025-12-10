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
        return results;
    }

    /// <inheritdoc/>
    public IEnumerable<Entity> QueryBounds(Vector3 min, Vector3 max)
    {
        var results = new HashSet<Entity>();
        root.QueryBounds(min, max, results, entityPositions);
        return results;
    }

    /// <inheritdoc/>
    public IEnumerable<Entity> QueryPoint(Vector3 point)
    {
        var node = root.FindLeafNode(point);

        // Return all entities in the leaf node (broadphase query)
        return node?.Entities.ToList() ?? Enumerable.Empty<Entity>();
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
    /// Internal update logic that handles both point and bounded entities.
    /// </summary>
    private void UpdateInternal(Entity entity, Vector3 position, (Vector3 min, Vector3 max)? bounds)
    {
        // Remove from old node if already indexed
        if (entityNodes.TryGetValue(entity, out var oldNode))
        {
            oldNode.Entities.Remove(entity);
        }
        else
        {
            entityCount++;
        }

        // Store entity position for potential redistribution during subdivision
        entityPositions[entity] = position;

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
