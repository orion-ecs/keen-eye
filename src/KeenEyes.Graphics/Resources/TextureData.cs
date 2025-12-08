namespace KeenEyes.Graphics;

/// <summary>
/// Specifies the texture filtering mode.
/// </summary>
public enum TextureFilter
{
    /// <summary>
    /// Nearest-neighbor filtering (pixelated).
    /// </summary>
    Nearest,

    /// <summary>
    /// Bilinear filtering (smooth).
    /// </summary>
    Linear,

    /// <summary>
    /// Trilinear filtering with mipmaps.
    /// </summary>
    Trilinear
}

/// <summary>
/// Specifies the texture wrapping mode.
/// </summary>
public enum TextureWrap
{
    /// <summary>
    /// Repeat the texture.
    /// </summary>
    Repeat,

    /// <summary>
    /// Mirror the texture on each repeat.
    /// </summary>
    MirroredRepeat,

    /// <summary>
    /// Clamp to the edge color.
    /// </summary>
    ClampToEdge,

    /// <summary>
    /// Clamp to a specified border color.
    /// </summary>
    ClampToBorder
}

/// <summary>
/// Represents texture data stored on the GPU.
/// </summary>
internal sealed class TextureData : IDisposable
{
    /// <summary>
    /// The OpenGL texture handle.
    /// </summary>
    public uint Handle { get; init; }

    /// <summary>
    /// The texture width in pixels.
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    /// The texture height in pixels.
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    /// Whether the texture has an alpha channel.
    /// </summary>
    public bool HasAlpha { get; init; }

    private bool disposed;

    /// <summary>
    /// Action to delete GPU resources. Set by the TextureManager.
    /// </summary>
    public Action<TextureData>? DeleteAction { get; set; }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        DeleteAction?.Invoke(this);
    }
}

/// <summary>
/// Manages texture resources on the GPU.
/// </summary>
internal sealed class TextureManager : IDisposable
{
    private readonly Dictionary<int, TextureData> textures = [];
    private int nextTextureId = 1;
    private bool disposed;

    /// <summary>
    /// Silk.NET OpenGL context. Set during initialization.
    /// </summary>
    public Silk.NET.OpenGL.GL? GL { get; set; }

    /// <summary>
    /// Creates a texture from raw RGBA pixel data.
    /// </summary>
    /// <param name="width">The texture width.</param>
    /// <param name="height">The texture height.</param>
    /// <param name="data">The RGBA pixel data.</param>
    /// <param name="filter">The texture filtering mode.</param>
    /// <param name="wrap">The texture wrapping mode.</param>
    /// <returns>The texture resource handle.</returns>
    public int CreateTexture(
        int width,
        int height,
        ReadOnlySpan<byte> data,
        TextureFilter filter = TextureFilter.Linear,
        TextureWrap wrap = TextureWrap.Repeat)
    {
        if (GL is null)
        {
            throw new InvalidOperationException("TextureManager not initialized with GL context");
        }

        uint handle = GL.GenTexture();
        GL.BindTexture(Silk.NET.OpenGL.TextureTarget.Texture2D, handle);

        // Upload texture data
        unsafe
        {
            fixed (byte* ptr = data)
            {
                GL.TexImage2D(
                    Silk.NET.OpenGL.TextureTarget.Texture2D,
                    0,
                    Silk.NET.OpenGL.InternalFormat.Rgba,
                    (uint)width,
                    (uint)height,
                    0,
                    Silk.NET.OpenGL.PixelFormat.Rgba,
                    Silk.NET.OpenGL.PixelType.UnsignedByte,
                    ptr);
            }
        }

        // Set filtering
        var (minFilter, magFilter) = filter switch
        {
            TextureFilter.Nearest => (
                Silk.NET.OpenGL.TextureMinFilter.Nearest,
                Silk.NET.OpenGL.TextureMagFilter.Nearest),
            TextureFilter.Linear => (
                Silk.NET.OpenGL.TextureMinFilter.Linear,
                Silk.NET.OpenGL.TextureMagFilter.Linear),
            TextureFilter.Trilinear => (
                Silk.NET.OpenGL.TextureMinFilter.LinearMipmapLinear,
                Silk.NET.OpenGL.TextureMagFilter.Linear),
            _ => (
                Silk.NET.OpenGL.TextureMinFilter.Linear,
                Silk.NET.OpenGL.TextureMagFilter.Linear)
        };

        GL.TexParameter(Silk.NET.OpenGL.TextureTarget.Texture2D,
            Silk.NET.OpenGL.TextureParameterName.TextureMinFilter, (int)minFilter);
        GL.TexParameter(Silk.NET.OpenGL.TextureTarget.Texture2D,
            Silk.NET.OpenGL.TextureParameterName.TextureMagFilter, (int)magFilter);

        // Set wrapping
        var wrapMode = wrap switch
        {
            TextureWrap.Repeat => Silk.NET.OpenGL.TextureWrapMode.Repeat,
            TextureWrap.MirroredRepeat => Silk.NET.OpenGL.TextureWrapMode.MirroredRepeat,
            TextureWrap.ClampToEdge => Silk.NET.OpenGL.TextureWrapMode.ClampToEdge,
            TextureWrap.ClampToBorder => Silk.NET.OpenGL.TextureWrapMode.ClampToBorder,
            _ => Silk.NET.OpenGL.TextureWrapMode.Repeat
        };

        GL.TexParameter(Silk.NET.OpenGL.TextureTarget.Texture2D,
            Silk.NET.OpenGL.TextureParameterName.TextureWrapS, (int)wrapMode);
        GL.TexParameter(Silk.NET.OpenGL.TextureTarget.Texture2D,
            Silk.NET.OpenGL.TextureParameterName.TextureWrapT, (int)wrapMode);

        // Generate mipmaps for trilinear filtering
        if (filter == TextureFilter.Trilinear)
        {
            GL.GenerateMipmap(Silk.NET.OpenGL.TextureTarget.Texture2D);
        }

        GL.BindTexture(Silk.NET.OpenGL.TextureTarget.Texture2D, 0);

        var textureData = new TextureData
        {
            Handle = handle,
            Width = width,
            Height = height,
            HasAlpha = true,
            DeleteAction = DeleteTextureData
        };

        int id = nextTextureId++;
        textures[id] = textureData;
        return id;
    }

    /// <summary>
    /// Creates a simple 1x1 solid color texture.
    /// </summary>
    /// <param name="r">Red component (0-255).</param>
    /// <param name="g">Green component (0-255).</param>
    /// <param name="b">Blue component (0-255).</param>
    /// <param name="a">Alpha component (0-255).</param>
    /// <returns>The texture resource handle.</returns>
    public int CreateSolidColorTexture(byte r, byte g, byte b, byte a = 255)
    {
        byte[] data = [r, g, b, a];
        return CreateTexture(1, 1, data, TextureFilter.Nearest);
    }

    /// <summary>
    /// Gets the texture data for the specified handle.
    /// </summary>
    /// <param name="textureId">The texture resource handle.</param>
    /// <returns>The texture data, or null if not found.</returns>
    public TextureData? GetTexture(int textureId)
    {
        return textures.GetValueOrDefault(textureId);
    }

    /// <summary>
    /// Deletes a texture resource.
    /// </summary>
    /// <param name="textureId">The texture resource handle.</param>
    /// <returns>True if deleted, false if not found.</returns>
    public bool DeleteTexture(int textureId)
    {
        if (textures.Remove(textureId, out var textureData))
        {
            textureData.Dispose();
            return true;
        }
        return false;
    }

    private void DeleteTextureData(TextureData data)
    {
        GL?.DeleteTexture(data.Handle);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        foreach (var texture in textures.Values)
        {
            texture.Dispose();
        }
        textures.Clear();
    }
}
