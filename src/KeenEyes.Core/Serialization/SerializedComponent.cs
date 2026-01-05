namespace KeenEyes.Serialization;

/// <summary>
/// Represents a serialized component with its type name and data.
/// </summary>
/// <remarks>
/// <para>
/// This record is used as part of the world snapshot system to store
/// component data in a format suitable for JSON or binary serialization.
/// </para>
/// <para>
/// The type is stored as a fully-qualified assembly name to enable
/// proper deserialization across different contexts.
/// </para>
/// </remarks>
public sealed record SerializedComponent
{
    /// <summary>
    /// Gets or sets the fully-qualified type name of the component.
    /// </summary>
    /// <remarks>
    /// This should be the result of <see cref="Type.AssemblyQualifiedName"/>
    /// to ensure proper type resolution during deserialization.
    /// </remarks>
    public required string TypeName { get; init; }

    /// <summary>
    /// Gets or sets the serialized component data as a JSON element.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Component data is pre-serialized to JSON format using <see cref="IComponentSerializer"/>
    /// for Native AOT compatibility. This eliminates the need for reflection during JSON serialization.
    /// </para>
    /// <para>
    /// Null for tag components which have no data.
    /// </para>
    /// </remarks>
    public System.Text.Json.JsonElement? Data { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether this component is a tag component.
    /// </summary>
    /// <remarks>
    /// Tag components have no data and are used purely as markers.
    /// When <see langword="true"/>, the <see cref="Data"/> property may be null or empty.
    /// </remarks>
    public bool IsTag { get; init; }

    /// <summary>
    /// Gets or sets the schema version of this component.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This value is used during deserialization to detect version mismatches
    /// and trigger component migrations when necessary.
    /// </para>
    /// <para>
    /// Default is 1 for backward compatibility with data serialized before
    /// versioning was introduced.
    /// </para>
    /// </remarks>
    public int Version { get; init; } = 1;
}
