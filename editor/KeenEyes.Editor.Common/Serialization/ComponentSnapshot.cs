namespace KeenEyes.Editor.Common.Serialization;

/// <summary>
/// Represents a snapshot of a component's state for editor operations.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <see cref="KeenEyes.Serialization.SerializedComponent"/> which uses JSON
/// for world persistence, this record stores component data as boxed objects for
/// in-memory editor operations like clipboard and undo/redo.
/// </para>
/// <para>
/// This approach avoids JSON serialization overhead for temporary editor operations
/// while still capturing complete component state.
/// </para>
/// </remarks>
public sealed record ComponentSnapshot
{
    /// <summary>
    /// Gets or sets the CLR type of the component.
    /// </summary>
    /// <remarks>
    /// The actual Type object is stored rather than a type name string since
    /// editor operations happen within the same process and don't require
    /// cross-process serialization.
    /// </remarks>
    public required Type ComponentType { get; init; }

    /// <summary>
    /// Gets or sets the component data as a boxed value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For regular components, this contains a boxed copy of the component struct.
    /// For tag components, this is null since tags carry no data.
    /// </para>
    /// <para>
    /// The value is a deep copy created at snapshot time to ensure the snapshot
    /// is independent of the original entity's state.
    /// </para>
    /// </remarks>
    public object? Value { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether this is a tag component.
    /// </summary>
    /// <remarks>
    /// Tag components have no data and are used purely as markers on entities.
    /// When true, <see cref="Value"/> should be null.
    /// </remarks>
    public bool IsTag { get; init; }
}
