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
    /// Gets or sets the serialized singleton data as a JSON element.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Singleton data is pre-serialized to JSON format using <see cref="IComponentSerializer"/>
    /// for Native AOT compatibility. This eliminates the need for reflection during JSON serialization.
    /// </para>
    /// </remarks>
    public required System.Text.Json.JsonElement Data { get; init; }
}
