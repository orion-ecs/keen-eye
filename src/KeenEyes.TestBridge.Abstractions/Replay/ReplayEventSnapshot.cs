namespace KeenEyes.TestBridge.Replay;

/// <summary>
/// Snapshot of a recorded replay event.
/// </summary>
public sealed record ReplayEventSnapshot
{
    /// <summary>
    /// Gets the type of replay event.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Gets the frame number when this event occurred.
    /// </summary>
    public required int Frame { get; init; }

    /// <summary>
    /// Gets the timestamp offset within the frame in milliseconds.
    /// </summary>
    public required float TimestampMs { get; init; }

    /// <summary>
    /// Gets the entity ID involved, if applicable.
    /// </summary>
    public int? EntityId { get; init; }

    /// <summary>
    /// Gets the component type name, if applicable.
    /// </summary>
    public string? ComponentTypeName { get; init; }

    /// <summary>
    /// Gets the system type name, if applicable.
    /// </summary>
    public string? SystemTypeName { get; init; }

    /// <summary>
    /// Gets the custom event type name.
    /// </summary>
    public string? CustomType { get; init; }
}
