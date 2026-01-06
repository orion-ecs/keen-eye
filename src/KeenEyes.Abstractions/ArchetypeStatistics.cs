namespace KeenEyes;

/// <summary>
/// Provides statistics for a single archetype in the ECS world.
/// </summary>
/// <remarks>
/// <para>
/// Each archetype stores entities with the same set of component types.
/// These statistics provide insight into memory usage and storage efficiency
/// for individual archetypes.
/// </para>
/// </remarks>
public readonly record struct ArchetypeStatistics
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
    /// <remarks>
    /// Chunks are fixed-size storage units. More chunks may indicate
    /// fragmentation if many are only partially filled.
    /// </remarks>
    public required int ChunkCount { get; init; }

    /// <summary>
    /// Gets the total capacity across all chunks in this archetype.
    /// </summary>
    public required int TotalCapacity { get; init; }

    /// <summary>
    /// Gets the names of component types in this archetype.
    /// </summary>
    /// <remarks>
    /// Component type names are used instead of Type objects for
    /// AOT compatibility and serialization friendliness.
    /// </remarks>
    public required IReadOnlyList<string> ComponentTypeNames { get; init; }

    /// <summary>
    /// Gets the estimated memory usage in bytes for component storage in this archetype.
    /// </summary>
    /// <remarks>
    /// This is an estimate based on component sizes and entity count.
    /// Actual memory usage may differ due to array pooling and alignment.
    /// </remarks>
    public required long EstimatedMemoryBytes { get; init; }

    /// <summary>
    /// Gets the fragmentation percentage for this archetype.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Fragmentation represents unused capacity within allocated chunks.
    /// A value of 0% means all chunk slots are used (perfect efficiency).
    /// Higher values indicate wasted memory due to partially filled chunks.
    /// </para>
    /// <para>
    /// Formula: (TotalCapacity - EntityCount) / TotalCapacity * 100
    /// </para>
    /// </remarks>
    public double FragmentationPercentage
    {
        get
        {
            if (TotalCapacity == 0)
            {
                return 0.0;
            }

            return (double)(TotalCapacity - EntityCount) / TotalCapacity * 100.0;
        }
    }

    /// <summary>
    /// Gets the utilization percentage for this archetype.
    /// </summary>
    /// <remarks>
    /// Utilization is the inverse of fragmentation, representing how much
    /// of the allocated capacity is being used.
    /// Formula: EntityCount / TotalCapacity * 100
    /// </remarks>
    public double UtilizationPercentage => 100.0 - FragmentationPercentage;
}
