namespace KeenEyes.Serialization;

/// <summary>
/// Interface for AOT-compatible binary component serialization.
/// </summary>
/// <remarks>
/// <para>
/// This interface extends serialization capabilities to support efficient binary formats.
/// It is implemented by generated code when components are marked with
/// <c>[Component(Serializable = true)]</c>. The source generator creates a strongly-typed
/// implementation that avoids runtime reflection.
/// </para>
/// <para>
/// Binary serialization provides significant performance and size benefits over JSON:
/// <list type="bullet">
/// <item><description>Smaller file sizes (typically 50-80% reduction)</description></item>
/// <item><description>Faster serialization/deserialization</description></item>
/// <item><description>No string parsing overhead</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Use generated serializer for binary serialization
/// var serializer = new ComponentSerializer();
/// var binary = SnapshotManager.ToBinary(snapshot, serializer);
/// var restored = SnapshotManager.FromBinary(binary, serializer);
/// </code>
/// </example>
public interface IBinaryComponentSerializer
{
    /// <summary>
    /// Writes a component to a binary writer.
    /// </summary>
    /// <param name="type">The type of the component.</param>
    /// <param name="value">The component value to serialize.</param>
    /// <param name="writer">The binary writer to write to.</param>
    /// <returns>True if the component was serialized; false if the type is not registered.</returns>
    bool WriteTo(Type type, object value, BinaryWriter writer);

    /// <summary>
    /// Reads a component from a binary reader.
    /// </summary>
    /// <param name="typeName">The fully-qualified type name of the component.</param>
    /// <param name="reader">The binary reader to read from.</param>
    /// <returns>The deserialized component, or null if the type is not registered.</returns>
    object? ReadFrom(string typeName, BinaryReader reader);
}
