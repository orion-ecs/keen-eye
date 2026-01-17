namespace KeenEyes.TestBridge.Replay;

/// <summary>
/// Snapshot of a recorded input event.
/// </summary>
public sealed record InputEventSnapshot
{
    /// <summary>
    /// Gets the type of input event.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Gets the frame number when this input occurred.
    /// </summary>
    public required int Frame { get; init; }

    /// <summary>
    /// Gets the key or button identifier, if applicable.
    /// </summary>
    public string? Key { get; init; }

    /// <summary>
    /// Gets the numeric value (axis value, scroll delta, etc.).
    /// </summary>
    public float? Value { get; init; }

    /// <summary>
    /// Gets the X position, if applicable.
    /// </summary>
    public float? PositionX { get; init; }

    /// <summary>
    /// Gets the Y position, if applicable.
    /// </summary>
    public float? PositionY { get; init; }

    /// <summary>
    /// Gets the custom type name for custom input events.
    /// </summary>
    public string? CustomType { get; init; }

    /// <summary>
    /// Gets the timestamp offset within the frame in milliseconds.
    /// </summary>
    public float? TimestampMs { get; init; }
}
