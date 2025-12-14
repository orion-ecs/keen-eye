namespace KeenEyes.Serialization;

/// <summary>
/// Specifies the compression algorithm used for save files.
/// </summary>
/// <remarks>
/// <para>
/// Compression can significantly reduce save file sizes at the cost of
/// additional CPU time during save/load operations. The optimal choice
/// depends on your use case:
/// </para>
/// <list type="bullet">
/// <item>
/// <description><see cref="None"/> - Fastest, no compression overhead</description>
/// </item>
/// <item>
/// <description><see cref="GZip"/> - Good balance of speed and compression ratio</description>
/// </item>
/// <item>
/// <description><see cref="Brotli"/> - Best compression ratio, slower than GZip</description>
/// </item>
/// </list>
/// </remarks>
public enum CompressionMode
{
    /// <summary>
    /// No compression. Data is stored as-is.
    /// </summary>
    /// <remarks>
    /// Use when save/load speed is critical or when data is already compressed.
    /// </remarks>
    None = 0,

    /// <summary>
    /// GZip compression using <see cref="System.IO.Compression.GZipStream"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// GZip provides a good balance between compression ratio and speed.
    /// Typically achieves 60-80% size reduction for ECS world data.
    /// </para>
    /// <para>
    /// This is the recommended default for most applications.
    /// </para>
    /// </remarks>
    GZip = 1,

    /// <summary>
    /// Brotli compression using <see cref="System.IO.Compression.BrotliStream"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Brotli typically achieves better compression ratios than GZip (10-20% smaller)
    /// but requires more CPU time, especially for compression.
    /// </para>
    /// <para>
    /// Use when file size is more important than save/load performance,
    /// such as for cloud saves where bandwidth is a concern.
    /// </para>
    /// </remarks>
    Brotli = 2
}
