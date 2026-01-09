using System.Numerics;

namespace KeenEyes.Graphics.Abstractions;

/// <summary>
/// An opaque handle to a texture resource.
/// </summary>
/// <remarks>
/// <para>
/// Texture handles are returned by <see cref="IResourceManager{THandle}"/> when creating textures
/// and must be used to reference the texture in draw calls and resource management operations.
/// </para>
/// <para>
/// Handles are opaque identifiers that avoid exposing backend-specific resource types,
/// enabling portability across different graphics APIs (OpenGL, Vulkan, DirectX, etc.).
/// </para>
/// <para>
/// The handle includes texture dimensions (Width, Height) to enable scale mode calculations
/// in the UI system without requiring runtime texture queries.
/// </para>
/// </remarks>
/// <param name="Id">The internal identifier for this texture resource.</param>
/// <param name="Width">The texture width in pixels.</param>
/// <param name="Height">The texture height in pixels.</param>
public readonly record struct TextureHandle(int Id, int Width = 0, int Height = 0)
{
    /// <summary>
    /// An invalid texture handle representing no texture.
    /// </summary>
    public static readonly TextureHandle Invalid = new(-1, 0, 0);

    /// <summary>
    /// Gets whether this handle refers to a valid texture resource.
    /// </summary>
    /// <remarks>
    /// A valid handle has a non-negative ID. Note that a valid handle does not guarantee
    /// the resource still exists - it may have been disposed.
    /// </remarks>
    public bool IsValid => Id >= 0;

    /// <summary>
    /// Gets the texture size as a vector.
    /// </summary>
    public Vector2 Size => new(Width, Height);

    /// <inheritdoc />
    public override string ToString() => IsValid ? $"Texture({Id}, {Width}x{Height})" : "Texture(Invalid)";
}
