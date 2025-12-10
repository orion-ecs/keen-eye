using System.Numerics;

namespace KeenEyes.Spatial;

/// <summary>
/// Configuration for octree-based spatial partitioning.
/// </summary>
/// <remarks>
/// <para>
/// An octree recursively divides 3D space into eight octants when the number
/// of entities in a node exceeds the configured threshold. This provides better
/// performance than grids for clustered entity distributions in 3D space.
/// </para>
/// <para>
/// Tuning guidelines:
/// - MaxDepth: Higher values allow finer subdivision but increase tree traversal cost (recommended: 6-10)
/// - MaxEntitiesPerNode: Lower values create more subdivisions, higher values reduce tree depth (recommended: 4-16)
/// - Bounds: Should encompass your game world; queries outside bounds still work but may be less efficient
/// </para>
/// </remarks>
public sealed class OctreeConfig
{
    /// <summary>
    /// Maximum depth of the octree. Each level subdivides nodes into 8 children.
    /// </summary>
    /// <remarks>
    /// Depth 0 is the root node. Depth 6 provides 8^6 = 262,144 leaf nodes.
    /// Higher depths allow finer spatial resolution but increase memory and traversal cost.
    /// Note: Octrees grow faster than quadtrees (8 vs 4 children), so recommended max depth is lower.
    /// </remarks>
    public int MaxDepth { get; init; } = 6;

    /// <summary>
    /// Maximum number of entities allowed in a node before it subdivides.
    /// </summary>
    /// <remarks>
    /// When a node contains more than this many entities AND the node is not at
    /// MaxDepth, it will subdivide into eight child octants.
    /// </remarks>
    public int MaxEntitiesPerNode { get; init; } = 8;

    /// <summary>
    /// Whether to use loose bounds for the octree (reduces updates for moving entities).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Loose octrees expand node bounds by a configurable factor, allowing entities
    /// to move within a larger region without needing to be reassigned to different nodes.
    /// This significantly reduces the cost of updates for dynamic scenes at the expense
    /// of slightly less efficient queries.
    /// </para>
    /// <para>
    /// Use loose bounds when:
    /// - Entities move frequently
    /// - Movement is relatively small compared to node size
    /// - Query performance is less critical than update performance
    /// </para>
    /// <para>
    /// Default is false (tight bounds) for optimal query performance.
    /// </para>
    /// </remarks>
    public bool UseLooseBounds { get; init; } = false;

    /// <summary>
    /// The factor by which node bounds are expanded when using loose bounds.
    /// </summary>
    /// <remarks>
    /// A factor of 2.0 means each node's bounds are doubled in size (1.0 expansion in each direction).
    /// Higher values reduce update frequency but increase query candidate count.
    /// Typical values: 1.5 - 3.0. Only used when UseLooseBounds is true.
    /// </remarks>
    public float LoosenessFactor { get; init; } = 2.0f;

    /// <summary>
    /// The minimum corner of the octree's root bounds.
    /// </summary>
    /// <remarks>
    /// Entities outside these bounds are still indexed in the tree (they go into
    /// the closest node), but performance is optimal when entities stay within bounds.
    /// </remarks>
    public Vector3 WorldMin { get; init; } = new(-10000, -10000, -10000);

    /// <summary>
    /// The maximum corner of the octree's root bounds.
    /// </summary>
    /// <remarks>
    /// Entities outside these bounds are still indexed in the tree (they go into
    /// the closest node), but performance is optimal when entities stay within bounds.
    /// </remarks>
    public Vector3 WorldMax { get; init; } = new(10000, 10000, 10000);

    /// <summary>
    /// When true, ensures query results are returned in a stable, deterministic order.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Deterministic mode guarantees that repeated queries with the same input
    /// will return results in the same order, which is essential for networked games
    /// and replay systems where behavior must be reproducible.
    /// </para>
    /// <para>
    /// Results are sorted by entity ID when deterministic mode is enabled.
    /// This adds a small performance cost (~10-20% slower queries) but ensures consistency.
    /// </para>
    /// </remarks>
    public bool DeterministicMode { get; init; } = false;

    /// <summary>
    /// Validates the configuration and returns any errors.
    /// </summary>
    /// <returns>An error message if invalid, or null if valid.</returns>
    public string? Validate()
    {
        if (MaxDepth < 1 || MaxDepth > 12)
        {
            return $"MaxDepth must be between 1 and 12, got {MaxDepth}";
        }

        if (MaxEntitiesPerNode < 1)
        {
            return $"MaxEntitiesPerNode must be positive, got {MaxEntitiesPerNode}";
        }

        if (WorldMin.X >= WorldMax.X || WorldMin.Y >= WorldMax.Y || WorldMin.Z >= WorldMax.Z)
        {
            return "WorldMin must be less than WorldMax in all dimensions";
        }

        if (UseLooseBounds && (LoosenessFactor < 1.0f || LoosenessFactor > 10.0f))
        {
            return $"LoosenessFactor must be between 1.0 and 10.0, got {LoosenessFactor}";
        }

        return null;
    }
}
