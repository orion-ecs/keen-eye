using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graphics.Silk.Resources;

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
    /// The texture handle.
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
    /// Graphics device for GPU operations. Set during initialization.
    /// </summary>
    public IGraphicsDevice? Device { get; set; }

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
        if (Device is null)
        {
            throw new InvalidOperationException("TextureManager not initialized with graphics device");
        }

        uint handle = Device.GenTexture();
        Device.BindTexture(TextureTarget.Texture2D, handle);

        // Upload texture data
        Device.TexImage2D(TextureTarget.Texture2D, 0, width, height, Abstractions.PixelFormat.RGBA, data);

        // Set filtering
        var (minFilter, magFilter) = filter switch
        {
            TextureFilter.Nearest => (TextureMinFilter.Nearest, TextureMagFilter.Nearest),
            TextureFilter.Linear => (TextureMinFilter.Linear, TextureMagFilter.Linear),
            TextureFilter.Trilinear => (TextureMinFilter.LinearMipmapLinear, TextureMagFilter.Linear),
            _ => (TextureMinFilter.Linear, TextureMagFilter.Linear)
        };

        Device.TexParameter(TextureTarget.Texture2D, TextureParam.MinFilter, (int)minFilter);
        Device.TexParameter(TextureTarget.Texture2D, TextureParam.MagFilter, (int)magFilter);

        // Set wrapping
        var wrapMode = wrap switch
        {
            TextureWrap.Repeat => TextureWrapMode.Repeat,
            TextureWrap.MirroredRepeat => TextureWrapMode.MirroredRepeat,
            TextureWrap.ClampToEdge => TextureWrapMode.ClampToEdge,
            TextureWrap.ClampToBorder => TextureWrapMode.ClampToBorder,
            _ => TextureWrapMode.Repeat
        };

        Device.TexParameter(TextureTarget.Texture2D, TextureParam.WrapS, (int)wrapMode);
        Device.TexParameter(TextureTarget.Texture2D, TextureParam.WrapT, (int)wrapMode);

        // Generate mipmaps for trilinear filtering
        if (filter == TextureFilter.Trilinear)
        {
            Device.GenerateMipmap(TextureTarget.Texture2D);
        }

        Device.BindTexture(TextureTarget.Texture2D, 0);

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
    /// Creates a GPU-compressed texture from pre-compressed data with mipmaps.
    /// </summary>
    /// <param name="width">The base texture width.</param>
    /// <param name="height">The base texture height.</param>
    /// <param name="format">The compressed texture format.</param>
    /// <param name="mipmaps">The mipmap chain data, from largest (level 0) to smallest.</param>
    /// <param name="filter">The texture filtering mode.</param>
    /// <param name="wrap">The texture wrapping mode.</param>
    /// <returns>The texture resource handle.</returns>
    public int CreateCompressedTexture(
        int width,
        int height,
        CompressedTextureFormat format,
        ReadOnlySpan<ReadOnlyMemory<byte>> mipmaps,
        TextureFilter filter = TextureFilter.Linear,
        TextureWrap wrap = TextureWrap.Repeat)
    {
        if (Device is null)
        {
            throw new InvalidOperationException("TextureManager not initialized with graphics device");
        }

        if (mipmaps.IsEmpty)
        {
            throw new ArgumentException("At least one mipmap level is required", nameof(mipmaps));
        }

        uint handle = Device.GenTexture();
        Device.BindTexture(TextureTarget.Texture2D, handle);

        // Upload each mipmap level
        int mipWidth = width;
        int mipHeight = height;
        for (int level = 0; level < mipmaps.Length; level++)
        {
            Device.CompressedTexImage2D(
                TextureTarget.Texture2D,
                level,
                mipWidth,
                mipHeight,
                format,
                mipmaps[level].Span);

            // Next mipmap is half the size (min 1)
            mipWidth = Math.Max(1, mipWidth / 2);
            mipHeight = Math.Max(1, mipHeight / 2);
        }

        // Set filtering based on whether we have mipmaps
        var hasMipmaps = mipmaps.Length > 1;
        var (minFilter, magFilter) = filter switch
        {
            TextureFilter.Nearest when hasMipmaps => (TextureMinFilter.NearestMipmapNearest, TextureMagFilter.Nearest),
            TextureFilter.Nearest => (TextureMinFilter.Nearest, TextureMagFilter.Nearest),
            TextureFilter.Linear when hasMipmaps => (TextureMinFilter.LinearMipmapLinear, TextureMagFilter.Linear),
            TextureFilter.Linear => (TextureMinFilter.Linear, TextureMagFilter.Linear),
            TextureFilter.Trilinear => (TextureMinFilter.LinearMipmapLinear, TextureMagFilter.Linear),
            _ => (TextureMinFilter.Linear, TextureMagFilter.Linear)
        };

        Device.TexParameter(TextureTarget.Texture2D, TextureParam.MinFilter, (int)minFilter);
        Device.TexParameter(TextureTarget.Texture2D, TextureParam.MagFilter, (int)magFilter);

        // Set wrapping
        var wrapMode = wrap switch
        {
            TextureWrap.Repeat => TextureWrapMode.Repeat,
            TextureWrap.MirroredRepeat => TextureWrapMode.MirroredRepeat,
            TextureWrap.ClampToEdge => TextureWrapMode.ClampToEdge,
            TextureWrap.ClampToBorder => TextureWrapMode.ClampToBorder,
            _ => TextureWrapMode.Repeat
        };

        Device.TexParameter(TextureTarget.Texture2D, TextureParam.WrapS, (int)wrapMode);
        Device.TexParameter(TextureTarget.Texture2D, TextureParam.WrapT, (int)wrapMode);

        Device.BindTexture(TextureTarget.Texture2D, 0);

        var textureData = new TextureData
        {
            Handle = handle,
            Width = width,
            Height = height,
            HasAlpha = HasAlphaChannel(format),
            DeleteAction = DeleteTextureData
        };

        int id = nextTextureId++;
        textures[id] = textureData;
        return id;
    }

    private static bool HasAlphaChannel(CompressedTextureFormat format) => format switch
    {
        CompressedTextureFormat.Bc1 => false,
        CompressedTextureFormat.Bc1Alpha => true,
        CompressedTextureFormat.Bc2 => true,
        CompressedTextureFormat.Bc3 => true,
        CompressedTextureFormat.Bc4 => false,
        CompressedTextureFormat.Bc5 => false,
        CompressedTextureFormat.Bc6h => false,
        CompressedTextureFormat.Bc7 => true,
        _ => false
    };

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
        Device?.DeleteTexture(data.Handle);
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
