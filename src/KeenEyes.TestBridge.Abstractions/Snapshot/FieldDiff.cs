namespace KeenEyes.TestBridge.Snapshot;

/// <summary>
/// Represents a change to a single field in a component.
/// </summary>
public sealed record FieldDiff
{
    /// <summary>
    /// Gets the field name.
    /// </summary>
    public required string FieldName { get; init; }

    /// <summary>
    /// Gets the old value (as a string representation).
    /// </summary>
    public required string OldValue { get; init; }

    /// <summary>
    /// Gets the new value (as a string representation).
    /// </summary>
    public required string NewValue { get; init; }
}
