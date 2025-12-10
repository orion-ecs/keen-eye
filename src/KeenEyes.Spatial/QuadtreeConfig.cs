using System.Numerics;

namespace KeenEyes.Spatial;

/// <summary>
/// Configuration for quadtree-based spatial partitioning.
/// </summary>
/// <remarks>
/// <para>
/// A quadtree recursively divides 2D space into four quadrants when the number
/// of entities in a node exceeds the configured threshold. This provides better
/// performance than grids for clustered entity distributions.
/// </para>
/// <para>
/// Tuning guidelines:
/// - MaxDepth: Higher values allow finer subdivision but increase tree traversal cost (recommended: 8-12)
/// - MaxEntitiesPerNode: Lower values create more subdivisions, higher values reduce tree depth (recommended: 4-16)
/// - Bounds: Should encompass your game world; queries outside bounds still work but may be less efficient
/// </para>
/// </remarks>
public sealed class QuadtreeConfig
{
    /// <summary>
    /// Maximum depth of the quadtree. Each level subdivides nodes into 4 children.
    /// </summary>
    /// <remarks>
    /// Depth 0 is the root node. Depth 8 provides 4^8 = 65,536 leaf nodes.
    /// Higher depths allow finer spatial resolution but increase memory and traversal cost.
    /// </remarks>
    public int MaxDepth { get; init; } = 8;

    /// <summary>
    /// Maximum number of entities allowed in a node before it subdivides.
    /// </summary>
    /// <remarks>
    /// When a node contains more than this many entities AND the node is not at
    /// MaxDepth, it will subdivide into four child quadrants.
    /// </remarks>
    public int MaxEntitiesPerNode { get; init; } = 8;

    /// <summary>
    /// The minimum corner of the quadtree's root bounds (X and Z used for 2D).
    /// </summary>
    /// <remarks>
    /// Entities outside these bounds are still indexed in the tree (they go into
    /// the closest node), but performance is optimal when entities stay within bounds.
    /// </remarks>
    public Vector3 WorldMin { get; init; } = new(-10000, 0, -10000);

    /// <summary>
    /// The maximum corner of the quadtree's root bounds (X and Z used for 2D).
    /// </summary>
    /// <remarks>
    /// Entities outside these bounds are still indexed in the tree (they go into
    /// the closest node), but performance is optimal when entities stay within bounds.
    /// </remarks>
    public Vector3 WorldMax { get; init; } = new(10000, 0, 10000);

    /// <summary>
    /// Validates the configuration and returns any errors.
    /// </summary>
    /// <returns>An error message if invalid, or null if valid.</returns>
    public string? Validate()
    {
        if (MaxDepth < 1 || MaxDepth > 16)
        {
            return $"MaxDepth must be between 1 and 16, got {MaxDepth}";
        }

        if (MaxEntitiesPerNode < 1)
        {
            return $"MaxEntitiesPerNode must be positive, got {MaxEntitiesPerNode}";
        }

        if (WorldMin.X >= WorldMax.X || WorldMin.Z >= WorldMax.Z)
        {
            return "WorldMin must be less than WorldMax in X and Z dimensions";
        }

        return null;
    }
}
