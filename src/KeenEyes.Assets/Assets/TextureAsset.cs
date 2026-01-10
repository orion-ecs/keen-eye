using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Assets;

/// <summary>
/// Represents the pixel format of a texture.
/// </summary>
public enum TextureFormat
{
    /// <summary>RGBA format with 8 bits per channel.</summary>
    Rgba8,

    /// <summary>RGB format with 8 bits per channel.</summary>
    Rgb8,

    /// <summary>Red channel only, 8 bits.</summary>
    R8,

    /// <summary>Red and green channels, 8 bits each.</summary>
    Rg8,

    /// <summary>RGBA format with 16 bits per channel (HDR).</summary>
    Rgba16F,

    /// <summary>BC1/DXT1 compressed RGB (4 bpp).</summary>
    Bc1,

    /// <summary>BC1/DXT1 compressed RGBA with 1-bit alpha (4 bpp).</summary>
    Bc1Alpha,

    /// <summary>BC2/DXT3 compressed RGBA (8 bpp).</summary>
    Bc2,

    /// <summary>BC3/DXT5 compressed RGBA (8 bpp).</summary>
    Bc3,

    /// <summary>BC4 compressed single channel (4 bpp).</summary>
    Bc4,

    /// <summary>BC5 compressed two channels (8 bpp).</summary>
    Bc5,

    /// <summary>BC6H compressed HDR (8 bpp).</summary>
    Bc6h,

    /// <summary>BC7 compressed high-quality RGBA (8 bpp).</summary>
    Bc7,

    /// <summary>Unknown or unsupported format.</summary>
    Unknown
}

/// <summary>
/// A loaded texture asset containing the GPU texture handle and metadata.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="TextureAsset"/> wraps a <see cref="TextureHandle"/> from the graphics
/// context along with metadata like dimensions and format. It is created by
/// <see cref="TextureLoader"/> and managed by <see cref="AssetManager"/>.
/// </para>
/// <para>
/// Disposing a TextureAsset releases the underlying GPU resource. However,
/// when using through <see cref="AssetHandle{T}"/>, the asset manager handles
/// disposal based on reference counting.
/// </para>
/// </remarks>
public sealed class TextureAsset : IDisposable
{
    private readonly IGraphicsContext? graphics;
    private bool disposed;

    /// <summary>
    /// Gets the underlying GPU texture handle.
    /// </summary>
    public TextureHandle Handle { get; }

    /// <summary>
    /// Gets the width of the texture in pixels.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Gets the height of the texture in pixels.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Gets the pixel format of the texture.
    /// </summary>
    public TextureFormat Format { get; }

    /// <summary>
    /// Gets the size of the texture in bytes (estimated).
    /// </summary>
    public long SizeBytes => Width * Height * GetBytesPerPixel(Format);

    /// <summary>
    /// Creates a new texture asset.
    /// </summary>
    /// <param name="handle">The GPU texture handle.</param>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    /// <param name="format">Pixel format.</param>
    /// <param name="graphics">Graphics context for resource cleanup.</param>
    internal TextureAsset(
        TextureHandle handle,
        int width,
        int height,
        TextureFormat format,
        IGraphicsContext? graphics)
    {
        Handle = handle;
        Width = width;
        Height = height;
        Format = format;
        this.graphics = graphics;
    }

    /// <summary>
    /// Releases the GPU texture resource.
    /// </summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        if (Handle.IsValid && graphics != null)
        {
            graphics.DeleteTexture(Handle);
        }
    }

    /// <summary>
    /// Calculates the bytes per pixel for the given format.
    /// For block-compressed formats, returns the bits per pixel as a fraction
    /// (e.g., BC1 = 4 bpp means 0.5 bytes per pixel on average).
    /// </summary>
    private static int GetBytesPerPixel(TextureFormat format) => format switch
    {
        TextureFormat.Rgba8 => 4,
        TextureFormat.Rgb8 => 3,
        TextureFormat.R8 => 1,
        TextureFormat.Rg8 => 2,
        TextureFormat.Rgba16F => 8,
        // For BC formats, we calculate based on block size:
        // BC1/BC4: 8 bytes per 4x4 block = 0.5 bytes per pixel (use 1 as minimum)
        // BC2/BC3/BC5/BC6H/BC7: 16 bytes per 4x4 block = 1 byte per pixel
        TextureFormat.Bc1 or TextureFormat.Bc1Alpha or TextureFormat.Bc4 => 1,
        TextureFormat.Bc2 or TextureFormat.Bc3 or TextureFormat.Bc5 or TextureFormat.Bc6h or TextureFormat.Bc7 => 1,
        _ => 4
    };
}
