using System.Text.Json;

namespace KeenEyes.TestBridge.AI;

/// <summary>
/// Snapshot of a single blackboard entry.
/// </summary>
public sealed record BlackboardEntrySnapshot
{
    /// <summary>
    /// Gets the entry key.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// Gets the value type name.
    /// </summary>
    public required string ValueType { get; init; }

    /// <summary>
    /// Gets the value as a JSON element for serialization.
    /// </summary>
    public JsonElement? Value { get; init; }

    /// <summary>
    /// Gets the value as a string representation for display.
    /// </summary>
    public string? ValueString { get; init; }
}
