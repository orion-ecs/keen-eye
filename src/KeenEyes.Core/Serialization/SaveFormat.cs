namespace KeenEyes.Serialization;

/// <summary>
/// Specifies the serialization format used for save files.
/// </summary>
/// <remarks>
/// <para>
/// The format determines how world data is serialized before compression.
/// </para>
/// <list type="bullet">
/// <item>
/// <description><see cref="Binary"/> - Compact binary format, recommended for production</description>
/// </item>
/// <item>
/// <description><see cref="Json"/> - Human-readable JSON format, useful for debugging</description>
/// </item>
/// </list>
/// </remarks>
public enum SaveFormat
{
    /// <summary>
    /// Binary format using <see cref="SnapshotManager.ToBinary{TSerializer}"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Binary format provides significant benefits:
    /// </para>
    /// <list type="bullet">
    /// <item><description>50-80% smaller than JSON</description></item>
    /// <item><description>Faster serialization/deserialization</description></item>
    /// <item><description>No string parsing overhead</description></item>
    /// </list>
    /// <para>
    /// This is the recommended format for production applications.
    /// </para>
    /// </remarks>
    Binary = 0,

    /// <summary>
    /// JSON format using <see cref="SnapshotManager.ToJson"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// JSON format is human-readable and useful for:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Debugging save file contents</description></item>
    /// <item><description>Manual editing of save data</description></item>
    /// <item><description>Integration with external tools</description></item>
    /// </list>
    /// <para>
    /// Note: JSON format results in larger file sizes and slower operations.
    /// </para>
    /// </remarks>
    Json = 1
}
