using System.Diagnostics.CodeAnalysis;

using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graphics.Silk;

/// <summary>
/// Internal data for a standard render target.
/// </summary>
internal sealed class RenderTargetData
{
    public required uint FramebufferId { get; init; }
    public required uint ColorTextureId { get; init; }
    public required uint DepthTextureId { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required RenderTargetFormat Format { get; init; }
    public bool IsDepthOnly => Format is RenderTargetFormat.Depth24 or RenderTargetFormat.Depth32F;
}

/// <summary>
/// Internal data for a cubemap render target.
/// </summary>
internal sealed class CubemapRenderTargetData
{
    public required uint FramebufferId { get; init; }
    public required uint CubemapTextureId { get; init; }
    public required uint DepthRenderbufferId { get; init; }
    public required int Size { get; init; }
    public required bool HasDepth { get; init; }
    public required int MipLevels { get; init; }
}

/// <summary>
/// Manages render target (framebuffer) lifecycle and state tracking.
/// </summary>
/// <remarks>
/// This internal manager handles:
/// <list type="bullet">
/// <item><description>Creation and deletion of framebuffer objects</description></item>
/// <item><description>Attachment of color and depth textures</description></item>
/// <item><description>Binding and unbinding of render targets</description></item>
/// <item><description>Handle-to-resource mapping</description></item>
/// </list>
/// </remarks>
/// <param name="device">The graphics device for GPU operations.</param>
[ExcludeFromCodeCoverage(Justification = "Requires real GPU context")]
internal sealed class RenderTargetManager(IGraphicsDevice device) : IDisposable
{
    private readonly IGraphicsDevice device = device ?? throw new ArgumentNullException(nameof(device));
    private readonly Dictionary<int, RenderTargetData> renderTargets = [];
    private readonly Dictionary<int, CubemapRenderTargetData> cubemapRenderTargets = [];
    private int nextRenderTargetId;
    private int nextCubemapRenderTargetId;
    private bool disposed;

    /// <summary>
    /// Creates a render target with the specified format.
    /// </summary>
    public RenderTargetHandle CreateRenderTarget(int width, int height, RenderTargetFormat format)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        // Generate framebuffer
        var fbo = device.GenFramebuffer();
        device.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);

        uint colorTexture = 0;
        var isDepthOnly = format is RenderTargetFormat.Depth24 or RenderTargetFormat.Depth32F;

        if (!isDepthOnly)
        {
            // Create color texture
            colorTexture = CreateColorTexture(width, height, format);
            device.FramebufferTexture2D(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D,
                colorTexture,
                0);
        }
        else
        {
            // For depth-only render targets, disable color writes
            device.DrawBuffer(DrawBufferMode.None);
            device.ReadBuffer(DrawBufferMode.None);
        }

        // Create depth texture (all formats use depth attachment)
        var depthTexture = CreateDepthTexture(width, height, format);
        device.FramebufferTexture2D(
            FramebufferTarget.Framebuffer,
            FramebufferAttachment.DepthAttachment,
            TextureTarget.Texture2D,
            depthTexture,
            0);

        // Check completeness
        var status = device.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != FramebufferStatus.Complete)
        {
            // Clean up on failure
            device.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            device.DeleteFramebuffer(fbo);
            if (colorTexture != 0)
            {
                device.DeleteTexture(colorTexture);
            }

            device.DeleteTexture(depthTexture);
            throw new InvalidOperationException($"Failed to create render target: {status}");
        }

        // Unbind
        device.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        // Store data
        var id = nextRenderTargetId++;
        renderTargets[id] = new RenderTargetData
        {
            FramebufferId = fbo,
            ColorTextureId = colorTexture,
            DepthTextureId = depthTexture,
            Width = width,
            Height = height,
            Format = format
        };

        return new RenderTargetHandle(id, width, height, format);
    }

    /// <summary>
    /// Creates a depth-only render target for shadow mapping.
    /// </summary>
    public RenderTargetHandle CreateDepthOnlyRenderTarget(int width, int height)
    {
        return CreateRenderTarget(width, height, RenderTargetFormat.Depth24);
    }

    /// <summary>
    /// Creates a cubemap render target for omnidirectional rendering.
    /// </summary>
    public CubemapRenderTargetHandle CreateCubemapRenderTarget(int size, bool withDepth, int mipLevels = 1)
    {
        if (size <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size));
        }

        if (mipLevels < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(mipLevels));
        }

        // Generate framebuffer
        var fbo = device.GenFramebuffer();
        device.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);

        // Create cubemap texture
        var cubemapTexture = device.GenTexture();
        device.BindTexture(TextureTarget.TextureCubeMap, cubemapTexture);

        // Allocate storage for each face
        for (int face = 0; face < 6; face++)
        {
            var currentSize = size;
            for (int mip = 0; mip < mipLevels; mip++)
            {
                // TextureCubeMapPositiveX = 0x8515, each face is consecutive
                var faceTarget = (TextureTarget)(0x8515 + face);
                device.TexImage2D(faceTarget, mip, currentSize, currentSize, PixelFormat.RGBA, ReadOnlySpan<byte>.Empty);
                currentSize = Math.Max(1, currentSize / 2);
            }
        }

        // Set texture parameters
        device.TexParameter(TextureTarget.TextureCubeMap, TextureParam.MinFilter,
            mipLevels > 1 ? (int)TextureMinFilter.LinearMipmapLinear : (int)TextureMinFilter.Linear);
        device.TexParameter(TextureTarget.TextureCubeMap, TextureParam.MagFilter, (int)TextureMagFilter.Linear);
        device.TexParameter(TextureTarget.TextureCubeMap, TextureParam.WrapS, (int)TextureWrapMode.ClampToEdge);
        device.TexParameter(TextureTarget.TextureCubeMap, TextureParam.WrapT, (int)TextureWrapMode.ClampToEdge);
        device.TexParameter(TextureTarget.TextureCubeMap, TextureParam.WrapR, (int)TextureWrapMode.ClampToEdge);

        // Create depth renderbuffer if needed
        uint depthRenderbuffer = 0;
        if (withDepth)
        {
            depthRenderbuffer = device.GenRenderbuffer();
            device.BindRenderbuffer(depthRenderbuffer);
            device.RenderbufferStorage(RenderbufferFormat.DepthComponent24, (uint)size, (uint)size);
            device.FramebufferRenderbuffer(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.DepthAttachment,
                depthRenderbuffer);
        }

        // Attach first face for initial completeness check
        device.FramebufferTexture2D(
            FramebufferTarget.Framebuffer,
            FramebufferAttachment.ColorAttachment0,
            (TextureTarget)0x8515, // TextureCubeMapPositiveX
            cubemapTexture,
            0);

        // Check completeness
        var status = device.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != FramebufferStatus.Complete)
        {
            // Clean up on failure
            device.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            device.DeleteFramebuffer(fbo);
            device.DeleteTexture(cubemapTexture);
            if (depthRenderbuffer != 0)
            {
                device.DeleteRenderbuffer(depthRenderbuffer);
            }

            throw new InvalidOperationException($"Failed to create cubemap render target: {status}");
        }

        // Unbind
        device.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        // Store data
        var id = nextCubemapRenderTargetId++;
        cubemapRenderTargets[id] = new CubemapRenderTargetData
        {
            FramebufferId = fbo,
            CubemapTextureId = cubemapTexture,
            DepthRenderbufferId = depthRenderbuffer,
            Size = size,
            HasDepth = withDepth,
            MipLevels = mipLevels
        };

        return new CubemapRenderTargetHandle(id, size, withDepth, mipLevels);
    }

    /// <summary>
    /// Binds a render target for rendering.
    /// </summary>
    public void BindRenderTarget(RenderTargetHandle target)
    {
        if (!target.IsValid || !renderTargets.TryGetValue(target.Id, out var data))
        {
            throw new ArgumentException("Invalid render target handle", nameof(target));
        }

        device.BindFramebuffer(FramebufferTarget.Framebuffer, data.FramebufferId);
        device.Viewport(0, 0, (uint)data.Width, (uint)data.Height);
    }

    /// <summary>
    /// Binds a specific face of a cubemap render target for rendering.
    /// </summary>
    public void BindCubemapRenderTarget(CubemapRenderTargetHandle target, CubemapFace face, int mipLevel = 0)
    {
        if (!target.IsValid || !cubemapRenderTargets.TryGetValue(target.Id, out var data))
        {
            throw new ArgumentException("Invalid cubemap render target handle", nameof(target));
        }

        if (mipLevel < 0 || mipLevel >= data.MipLevels)
        {
            throw new ArgumentOutOfRangeException(nameof(mipLevel));
        }

        device.BindFramebuffer(FramebufferTarget.Framebuffer, data.FramebufferId);

        // Attach the specified face at the specified mip level
        var faceTarget = (TextureTarget)(0x8515 + (int)face); // TextureCubeMapPositiveX + face
        device.FramebufferTexture2D(
            FramebufferTarget.Framebuffer,
            FramebufferAttachment.ColorAttachment0,
            faceTarget,
            data.CubemapTextureId,
            mipLevel);

        // Calculate viewport size for this mip level
        var mipSize = Math.Max(1, data.Size >> mipLevel);
        device.Viewport(0, 0, (uint)mipSize, (uint)mipSize);
    }

    /// <summary>
    /// Unbinds the current render target.
    /// </summary>
    public void UnbindRenderTarget()
    {
        device.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    /// <summary>
    /// Gets the color texture ID for a render target.
    /// </summary>
    public uint GetColorTextureId(RenderTargetHandle target)
    {
        if (!target.IsValid || !renderTargets.TryGetValue(target.Id, out var data))
        {
            return 0;
        }

        return data.ColorTextureId;
    }

    /// <summary>
    /// Gets the depth texture ID for a render target.
    /// </summary>
    public uint GetDepthTextureId(RenderTargetHandle target)
    {
        if (!target.IsValid || !renderTargets.TryGetValue(target.Id, out var data))
        {
            return 0;
        }

        return data.DepthTextureId;
    }

    /// <summary>
    /// Gets the cubemap texture ID for a cubemap render target.
    /// </summary>
    public uint GetCubemapTextureId(CubemapRenderTargetHandle target)
    {
        if (!target.IsValid || !cubemapRenderTargets.TryGetValue(target.Id, out var data))
        {
            return 0;
        }

        return data.CubemapTextureId;
    }

    /// <summary>
    /// Deletes a render target and its associated resources.
    /// </summary>
    public void DeleteRenderTarget(RenderTargetHandle target)
    {
        if (!target.IsValid || !renderTargets.TryGetValue(target.Id, out var data))
        {
            return;
        }

        device.DeleteFramebuffer(data.FramebufferId);
        if (data.ColorTextureId != 0)
        {
            device.DeleteTexture(data.ColorTextureId);
        }

        device.DeleteTexture(data.DepthTextureId);
        renderTargets.Remove(target.Id);
    }

    /// <summary>
    /// Deletes a cubemap render target and its associated resources.
    /// </summary>
    public void DeleteCubemapRenderTarget(CubemapRenderTargetHandle target)
    {
        if (!target.IsValid || !cubemapRenderTargets.TryGetValue(target.Id, out var data))
        {
            return;
        }

        device.DeleteFramebuffer(data.FramebufferId);
        device.DeleteTexture(data.CubemapTextureId);
        if (data.DepthRenderbufferId != 0)
        {
            device.DeleteRenderbuffer(data.DepthRenderbufferId);
        }

        cubemapRenderTargets.Remove(target.Id);
    }

    private uint CreateColorTexture(int width, int height, RenderTargetFormat format)
    {
        var texture = device.GenTexture();
        device.BindTexture(TextureTarget.Texture2D, texture);

        var pixelFormat = format switch
        {
            RenderTargetFormat.RGBA8Depth24 => PixelFormat.RGBA,
            RenderTargetFormat.RGBA16FDepth24 => PixelFormat.RGBA16F,
            RenderTargetFormat.RGBA32FDepth32F => PixelFormat.RGBA32F,
            _ => PixelFormat.RGBA
        };

        device.TexImage2D(TextureTarget.Texture2D, 0, width, height, pixelFormat, ReadOnlySpan<byte>.Empty);
        device.TexParameter(TextureTarget.Texture2D, TextureParam.MinFilter, (int)TextureMinFilter.Linear);
        device.TexParameter(TextureTarget.Texture2D, TextureParam.MagFilter, (int)TextureMagFilter.Linear);
        device.TexParameter(TextureTarget.Texture2D, TextureParam.WrapS, (int)TextureWrapMode.ClampToEdge);
        device.TexParameter(TextureTarget.Texture2D, TextureParam.WrapT, (int)TextureWrapMode.ClampToEdge);

        return texture;
    }

    private uint CreateDepthTexture(int width, int height, RenderTargetFormat format)
    {
        var texture = device.GenTexture();
        device.BindTexture(TextureTarget.Texture2D, texture);

        var pixelFormat = format switch
        {
            RenderTargetFormat.RGBA8Depth24 or RenderTargetFormat.RGBA16FDepth24 or RenderTargetFormat.Depth24
                => PixelFormat.Depth24,
            RenderTargetFormat.RGBA32FDepth32F or RenderTargetFormat.Depth32F
                => PixelFormat.Depth32F,
            _ => PixelFormat.Depth24
        };

        device.TexImage2D(TextureTarget.Texture2D, 0, width, height, pixelFormat, ReadOnlySpan<byte>.Empty);
        device.TexParameter(TextureTarget.Texture2D, TextureParam.MinFilter, (int)TextureMinFilter.Nearest);
        device.TexParameter(TextureTarget.Texture2D, TextureParam.MagFilter, (int)TextureMagFilter.Nearest);
        device.TexParameter(TextureTarget.Texture2D, TextureParam.WrapS, (int)TextureWrapMode.ClampToEdge);
        device.TexParameter(TextureTarget.Texture2D, TextureParam.WrapT, (int)TextureWrapMode.ClampToEdge);

        // For shadow mapping: enable depth comparison
        // GL_TEXTURE_COMPARE_MODE = 0x884C, GL_COMPARE_REF_TO_TEXTURE = 0x884E
        device.TexParameter(TextureTarget.Texture2D, TextureParam.CompareMode, 0x884E);
        // GL_TEXTURE_COMPARE_FUNC with GL_LEQUAL = 0x0203
        device.TexParameter(TextureTarget.Texture2D, TextureParam.CompareFunc, 0x0203);

        return texture;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        // Delete all render targets
        foreach (var data in renderTargets.Values)
        {
            device.DeleteFramebuffer(data.FramebufferId);
            if (data.ColorTextureId != 0)
            {
                device.DeleteTexture(data.ColorTextureId);
            }

            device.DeleteTexture(data.DepthTextureId);
        }

        renderTargets.Clear();

        // Delete all cubemap render targets
        foreach (var data in cubemapRenderTargets.Values)
        {
            device.DeleteFramebuffer(data.FramebufferId);
            device.DeleteTexture(data.CubemapTextureId);
            if (data.DepthRenderbufferId != 0)
            {
                device.DeleteRenderbuffer(data.DepthRenderbufferId);
            }
        }

        cubemapRenderTargets.Clear();
    }
}
