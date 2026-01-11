namespace KeenEyes.Graphics.Abstractions;

/// <summary>
/// An opaque handle to a cubemap render target resource.
/// </summary>
/// <remarks>
/// <para>
/// Cubemap render target handles represent off-screen framebuffers that render to all six
/// faces of a cubemap texture. These are used for:
/// </para>
/// <list type="bullet">
/// <item><description>Point light omnidirectional shadow maps</description></item>
/// <item><description>Environment map generation</description></item>
/// <item><description>IBL irradiance and specular convolution</description></item>
/// </list>
/// <para>
/// The handle includes the face size and depth information to support queries without
/// requiring backend access.
/// </para>
/// </remarks>
/// <param name="Id">The internal identifier for this cubemap render target resource.</param>
/// <param name="Size">The size of each cubemap face in pixels (faces are square).</param>
/// <param name="HasDepth">Whether the cubemap render target has a depth buffer.</param>
/// <param name="MipLevels">The number of mip levels (1 for shadow maps, more for IBL pre-filtering).</param>
public readonly record struct CubemapRenderTargetHandle(int Id, int Size = 0, bool HasDepth = true, int MipLevels = 1)
{
    /// <summary>
    /// An invalid cubemap render target handle representing no render target.
    /// </summary>
    public static readonly CubemapRenderTargetHandle Invalid = new(-1, 0);

    /// <summary>
    /// Gets whether this handle refers to a valid cubemap render target resource.
    /// </summary>
    /// <remarks>
    /// A valid handle has a non-negative ID. Note that a valid handle does not guarantee
    /// the resource still exists - it may have been disposed.
    /// </remarks>
    public bool IsValid => Id >= 0;

    /// <inheritdoc />
    public override string ToString() => IsValid
        ? $"CubemapRenderTarget({Id}, {Size}x{Size}x6, depth={HasDepth}, mips={MipLevels})"
        : "CubemapRenderTarget(Invalid)";
}
