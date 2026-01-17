namespace KeenEyes.TestBridge.Snapshot;

/// <summary>
/// Represents changes to a single entity between snapshots.
/// </summary>
public sealed record EntityDiff
{
    /// <summary>
    /// Gets the entity ID.
    /// </summary>
    public required int EntityId { get; init; }

    /// <summary>
    /// Gets the entity name, if any.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the type of change (Added, Removed, or Modified).
    /// </summary>
    public required string ChangeType { get; init; }

    /// <summary>
    /// Gets components that were added to the entity.
    /// </summary>
    public IReadOnlyList<string>? AddedComponents { get; init; }

    /// <summary>
    /// Gets components that were removed from the entity.
    /// </summary>
    public IReadOnlyList<string>? RemovedComponents { get; init; }

    /// <summary>
    /// Gets components that were modified on the entity.
    /// </summary>
    public IReadOnlyList<ComponentDiff>? ModifiedComponents { get; init; }
}
