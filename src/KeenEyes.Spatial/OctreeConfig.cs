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

        return null;
    }
}
