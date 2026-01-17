namespace KeenEyes.TestBridge.Profile;

/// <summary>
/// Snapshot of statistics for a single archetype.
/// </summary>
/// <remarks>
/// Each archetype stores entities with the same set of component types.
/// These statistics provide insight into memory usage and storage efficiency.
/// </remarks>
public sealed record ArchetypeStatsSnapshot
{
    /// <summary>
    /// Gets the unique identifier for this archetype.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the number of entities currently stored in this archetype.
    /// </summary>
    public required int EntityCount { get; init; }

    /// <summary>
    /// Gets the number of chunks used by this archetype.
    /// </summary>
    public required int ChunkCount { get; init; }

    /// <summary>
    /// Gets the total capacity across all chunks in this archetype.
    /// </summary>
    public required int TotalCapacity { get; init; }

    /// <summary>
    /// Gets the names of component types in this archetype.
    /// </summary>
    public required IReadOnlyList<string> ComponentTypeNames { get; init; }

    /// <summary>
    /// Gets the estimated memory usage in bytes for this archetype.
    /// </summary>
    public required long EstimatedMemoryBytes { get; init; }

    /// <summary>
    /// Gets the fragmentation percentage for this archetype.
    /// </summary>
    /// <remarks>
    /// Represents unused capacity within allocated chunks (0-100).
    /// Lower is better.
    /// </remarks>
    public required double FragmentationPercentage { get; init; }

    /// <summary>
    /// Gets the utilization percentage for this archetype.
    /// </summary>
    /// <remarks>
    /// Represents how much of the allocated capacity is being used (0-100).
    /// Higher is better.
    /// </remarks>
    public required double UtilizationPercentage { get; init; }
}
