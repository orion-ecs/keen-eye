using System.Numerics;

namespace KeenEyes.Graphics.Abstractions;

/// <summary>
/// An opaque handle to a render target (framebuffer) resource.
/// </summary>
/// <remarks>
/// <para>
/// Render target handles represent off-screen framebuffers used for multi-pass rendering
/// such as shadow maps, post-processing effects, and image-based lighting pre-computation.
/// </para>
/// <para>
/// Handles are opaque identifiers that avoid exposing backend-specific resource types,
/// enabling portability across different graphics APIs (OpenGL, Vulkan, DirectX, etc.).
/// </para>
/// <para>
/// The handle includes dimensions and format information to support queries without
/// requiring backend access.
/// </para>
/// </remarks>
/// <param name="Id">The internal identifier for this render target resource.</param>
/// <param name="Width">The render target width in pixels.</param>
/// <param name="Height">The render target height in pixels.</param>
/// <param name="Format">The render target format.</param>
public readonly record struct RenderTargetHandle(int Id, int Width = 0, int Height = 0, RenderTargetFormat Format = RenderTargetFormat.RGBA8Depth24)
{
    /// <summary>
    /// An invalid render target handle representing no render target.
    /// </summary>
    public static readonly RenderTargetHandle Invalid = new(-1, 0, 0);

    /// <summary>
    /// Gets whether this handle refers to a valid render target resource.
    /// </summary>
    /// <remarks>
    /// A valid handle has a non-negative ID. Note that a valid handle does not guarantee
    /// the resource still exists - it may have been disposed.
    /// </remarks>
    public bool IsValid => Id >= 0;

    /// <summary>
    /// Gets the render target size as a vector.
    /// </summary>
    public Vector2 Size => new(Width, Height);

    /// <summary>
    /// Gets whether this render target has a color attachment.
    /// </summary>
    public bool HasColorAttachment => Format is not (RenderTargetFormat.Depth24 or RenderTargetFormat.Depth32F);

    /// <summary>
    /// Gets whether this render target has a depth attachment.
    /// </summary>
    /// <remarks>
    /// All supported render target formats include a depth buffer.
    /// </remarks>
    public static bool HasDepthAttachment => true;

    /// <inheritdoc />
    public override string ToString() => IsValid ? $"RenderTarget({Id}, {Width}x{Height}, {Format})" : "RenderTarget(Invalid)";
}
