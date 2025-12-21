namespace KeenEyes.Assets;

/// <summary>
/// A raw binary asset containing unprocessed file data.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="RawAsset"/> is used for loading arbitrary binary files that don't
/// have a specialized loader. The raw bytes can then be processed by application code.
/// </para>
/// <para>
/// Use this for custom file formats, configuration files, or any other binary data
/// that needs to be loaded through the asset system.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var raw = assets.Load&lt;RawAsset&gt;("data/config.bin");
/// if (raw.IsLoaded)
/// {
///     var data = raw.Asset!.Data;
///     // Process the raw bytes...
/// }
/// </code>
/// </example>
/// <param name="data">The raw file data.</param>
public sealed class RawAsset(byte[] data) : IDisposable
{
    private bool disposed;

    /// <summary>
    /// Gets the raw file data.
    /// </summary>
    public byte[] Data { get; } = data;

    /// <summary>
    /// Gets the size of the data in bytes.
    /// </summary>
    public long SizeBytes => Data.Length;

    /// <summary>
    /// Gets the data as a read-only span.
    /// </summary>
    public ReadOnlySpan<byte> AsSpan() => Data;

    /// <summary>
    /// Gets the data as a read-only memory.
    /// </summary>
    public ReadOnlyMemory<byte> AsMemory() => Data;

    /// <summary>
    /// Creates a stream for reading the data.
    /// </summary>
    /// <returns>A memory stream containing the data.</returns>
    public MemoryStream CreateStream() => new(Data, writable: false);

    /// <summary>
    /// Releases the data.
    /// </summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        // GC will clean up the byte array
    }
}
