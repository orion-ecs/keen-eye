namespace KeenEyes.Graphics.Abstractions;

/// <summary>
/// An opaque handle to a shader program resource.
/// </summary>
/// <remarks>
/// <para>
/// Shader handles are returned by <see cref="IResourceManager{THandle}"/> when creating shaders
/// and must be used to reference the shader in draw calls and resource management operations.
/// </para>
/// <para>
/// Handles are opaque identifiers that avoid exposing backend-specific resource types,
/// enabling portability across different graphics APIs (OpenGL, Vulkan, DirectX, etc.).
/// </para>
/// </remarks>
/// <param name="Id">The internal identifier for this shader resource.</param>
public readonly record struct ShaderHandle(int Id)
{
    /// <summary>
    /// An invalid shader handle representing no shader.
    /// </summary>
    public static readonly ShaderHandle Invalid = new(-1);

    /// <summary>
    /// Gets whether this handle refers to a valid shader resource.
    /// </summary>
    /// <remarks>
    /// A valid handle has a non-negative ID. Note that a valid handle does not guarantee
    /// the resource still exists - it may have been disposed.
    /// </remarks>
    public bool IsValid => Id >= 0;

    /// <inheritdoc />
    public override string ToString() => IsValid ? $"Shader({Id})" : "Shader(Invalid)";
}
