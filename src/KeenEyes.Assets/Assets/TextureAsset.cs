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

    private static int GetBytesPerPixel(TextureFormat format) => format switch
    {
        TextureFormat.Rgba8 => 4,
        TextureFormat.Rgb8 => 3,
        TextureFormat.R8 => 1,
        TextureFormat.Rg8 => 2,
        TextureFormat.Rgba16F => 8,
        _ => 4
    };
}
