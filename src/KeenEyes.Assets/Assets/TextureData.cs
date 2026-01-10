namespace KeenEyes.Assets;

/// <summary>
/// Raw texture image data extracted from a model (not a GPU resource).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="TextureData"/> holds CPU-side pixel data that can later be uploaded to GPU
/// via <see cref="TextureLoader"/>. This allows <see cref="ModelAsset"/> to remain
/// graphics-context-independent.
/// </para>
/// <para>
/// Textures can be embedded in glTF files (base64 encoded) or referenced as external files.
/// For external textures, <see cref="SourcePath"/> contains the resolved file path.
/// </para>
/// </remarks>
/// <param name="name">The texture name from the glTF file.</param>
/// <param name="pixels">Raw pixel data in row-major order, top-to-bottom.</param>
/// <param name="width">Texture width in pixels.</param>
/// <param name="height">Texture height in pixels.</param>
/// <param name="components">Number of color components (3 for RGB, 4 for RGBA).</param>
/// <param name="sourcePath">Original file path for external textures, or null for embedded.</param>
public sealed class TextureData(
    string name,
    byte[] pixels,
    int width,
    int height,
    int components,
    string? sourcePath = null) : IDisposable
{
    private bool disposed;

    /// <summary>
    /// Gets the texture name.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the raw pixel data.
    /// </summary>
    /// <remarks>
    /// Pixel data is stored in row-major order, top-to-bottom.
    /// Each pixel has <see cref="Components"/> bytes (RGB or RGBA).
    /// Total size is <c>Width * Height * Components</c> bytes.
    /// </remarks>
    public byte[] Pixels { get; } = pixels;

    /// <summary>
    /// Gets the texture width in pixels.
    /// </summary>
    public int Width { get; } = width;

    /// <summary>
    /// Gets the texture height in pixels.
    /// </summary>
    public int Height { get; } = height;

    /// <summary>
    /// Gets the number of color components per pixel.
    /// </summary>
    /// <remarks>
    /// Common values:
    /// <list type="bullet">
    /// <item><description>3 - RGB (no alpha)</description></item>
    /// <item><description>4 - RGBA (with alpha)</description></item>
    /// </list>
    /// </remarks>
    public int Components { get; } = components;

    /// <summary>
    /// Gets the original file path for external textures.
    /// </summary>
    /// <remarks>
    /// This is null for embedded textures (base64 in glTF).
    /// For external textures, this is the resolved absolute path.
    /// </remarks>
    public string? SourcePath { get; } = sourcePath;

    /// <summary>
    /// Gets the size of the texture data in bytes.
    /// </summary>
    public long SizeBytes => Pixels.LongLength;

    /// <summary>
    /// Gets whether this texture has an alpha channel.
    /// </summary>
    public bool HasAlpha => Components == 4;

    /// <summary>
    /// Creates a span over the pixel data.
    /// </summary>
    /// <returns>A read-only span of the pixel bytes.</returns>
    public ReadOnlySpan<byte> AsSpan() => Pixels;

    /// <summary>
    /// Creates a memory region over the pixel data.
    /// </summary>
    /// <returns>A read-only memory of the pixel bytes.</returns>
    public ReadOnlyMemory<byte> AsMemory() => Pixels;

    /// <summary>
    /// Releases the texture data.
    /// </summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        // Pixel array will be collected by GC
    }
}
