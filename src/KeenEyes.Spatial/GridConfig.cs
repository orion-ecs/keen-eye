using System.Numerics;

namespace KeenEyes.Spatial;

/// <summary>
/// Configuration for grid-based spatial partitioning.
/// </summary>
public sealed class GridConfig
{
    /// <summary>
    /// The size of each grid cell in world units.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Smaller cells provide more precise spatial queries but increase memory usage
    /// and update overhead. Larger cells are more memory-efficient but may return
    /// more false positives in queries.
    /// </para>
    /// <para>
    /// As a rule of thumb, set CellSize to approximately 2x the average entity size
    /// for optimal query performance.
    /// </para>
    /// </remarks>
    public float CellSize { get; init; } = 100f;

    /// <summary>
    /// The minimum corner of the world bounds.
    /// </summary>
    /// <remarks>
    /// Entities outside these bounds will still be indexed, but performance
    /// may degrade for very large coordinate values due to hash collisions.
    /// </remarks>
    public Vector3 WorldMin { get; init; } = new(-10000, -10000, -10000);

    /// <summary>
    /// The maximum corner of the world bounds.
    /// </summary>
    /// <remarks>
    /// Entities outside these bounds will still be indexed, but performance
    /// may degrade for very large coordinate values due to hash collisions.
    /// </remarks>
    public Vector3 WorldMax { get; init; } = new(10000, 10000, 10000);

    /// <summary>
    /// Validates the configuration and returns any errors.
    /// </summary>
    /// <returns>An error message if invalid, or null if valid.</returns>
    public string? Validate()
    {
        if (CellSize <= 0)
        {
            return $"CellSize must be positive, got {CellSize}";
        }

        if (WorldMin.X >= WorldMax.X || WorldMin.Y >= WorldMax.Y || WorldMin.Z >= WorldMax.Z)
        {
            return $"WorldMin must be less than WorldMax in all dimensions";
        }

        return null;
    }
}
