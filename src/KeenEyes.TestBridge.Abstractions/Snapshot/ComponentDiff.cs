namespace KeenEyes.TestBridge.Snapshot;

/// <summary>
/// Represents changes to a component between snapshots.
/// </summary>
public sealed record ComponentDiff
{
    /// <summary>
    /// Gets the component type name.
    /// </summary>
    public required string ComponentType { get; init; }

    /// <summary>
    /// Gets the fields that changed in this component.
    /// </summary>
    public required IReadOnlyList<FieldDiff> Fields { get; init; }
}
