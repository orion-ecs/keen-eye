namespace KeenEyes.Serialization;

/// <summary>
/// Represents a serialized singleton with its type name and data.
/// </summary>
/// <remarks>
/// <para>
/// Singletons are world-level data not tied to any entity.
/// This record captures singleton state for snapshot persistence.
/// </para>
/// </remarks>
public sealed record SerializedSingleton
{
    /// <summary>
    /// Gets or sets the fully-qualified type name of the singleton.
    /// </summary>
    /// <remarks>
    /// This should be the result of <see cref="Type.AssemblyQualifiedName"/>
    /// to ensure proper type resolution during deserialization.
    /// </remarks>
    public required string TypeName { get; init; }

    /// <summary>
    /// Gets or sets the serialized singleton data.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For JSON serialization, this will be a <see cref="System.Text.Json.JsonElement"/>
    /// representing the singleton's fields.
    /// </para>
    /// <para>
    /// For binary serialization, this will be a byte array containing
    /// the serialized singleton data.
    /// </para>
    /// </remarks>
    public required object Data { get; init; }
}
