using System.Diagnostics.CodeAnalysis;

using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graphics.Silk.Text;

/// <summary>
/// OpenGL texture wrapper for FontStashSharp integration.
/// </summary>
/// <remarks>
/// This class wraps an OpenGL texture handle and provides data upload
/// for font atlas textures used by FontStashSharp.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "Requires real GPU context")]
internal sealed class FontStashTexture : IDisposable
{
    private readonly IGraphicsDevice device;
    private bool disposed;

    /// <summary>
    /// Creates a new FontStashSharp-compatible texture.
    /// </summary>
    /// <param name="device">The graphics device for texture operations.</param>
    /// <param name="width">The texture width in pixels.</param>
    /// <param name="height">The texture height in pixels.</param>
    public FontStashTexture(IGraphicsDevice device, int width, int height)
    {
        this.device = device ?? throw new ArgumentNullException(nameof(device));
        Width = width;
        Height = height;

        // Create OpenGL texture
        TextureHandle = device.GenTexture();

        device.ActiveTexture(TextureUnit.Texture0);
        device.BindTexture(TextureTarget.Texture2D, TextureHandle);

        // Allocate storage with RGBA format (null data = allocate without initializing)
        device.TexImage2D(TextureTarget.Texture2D, 0, width, height, PixelFormat.RGBA, ReadOnlySpan<byte>.Empty);

        // Set texture parameters for font rendering
        device.TexParameter(TextureTarget.Texture2D, TextureParam.WrapS, (int)TextureWrapMode.ClampToEdge);
        device.TexParameter(TextureTarget.Texture2D, TextureParam.WrapT, (int)TextureWrapMode.ClampToEdge);
        device.TexParameter(TextureTarget.Texture2D, TextureParam.MinFilter, (int)TextureMinFilter.Linear);
        device.TexParameter(TextureTarget.Texture2D, TextureParam.MagFilter, (int)TextureMagFilter.Linear);

        device.BindTexture(TextureTarget.Texture2D, 0);
    }

    /// <summary>
    /// Gets the OpenGL texture handle.
    /// </summary>
    public uint TextureHandle { get; }

    /// <summary>
    /// Gets the texture width in pixels.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Gets the texture height in pixels.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Sets pixel data for a region of the texture.
    /// </summary>
    /// <param name="bounds">The region to update.</param>
    /// <param name="data">The RGBA pixel data (tightly packed, starting at byte 0).</param>
    /// <remarks>
    /// FontStashSharp passes a buffer that may be larger than bounds.Width * bounds.Height * 4,
    /// but the glyph data is always tightly packed at the START of the buffer.
    /// OpenGL's TexSubImage2D reads exactly width*height*4 bytes from the pointer.
    /// </remarks>
    public void SetData(System.Drawing.Rectangle bounds, byte[] data)
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(FontStashTexture));
        }

        device.ActiveTexture(TextureUnit.Texture0);
        device.BindTexture(TextureTarget.Texture2D, TextureHandle);

        // Pass data directly - OpenGL reads exactly bounds.Width * bounds.Height * 4 bytes
        // from the start of the buffer. Any extra bytes at the end are unused padding.
        device.TexSubImage2D(
            TextureTarget.Texture2D,
            0,
            bounds.X,
            bounds.Y,
            bounds.Width,
            bounds.Height,
            PixelFormat.RGBA,
            data);

        device.BindTexture(TextureTarget.Texture2D, 0);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        if (TextureHandle != 0)
        {
            device.DeleteTexture(TextureHandle);
        }
    }
}
