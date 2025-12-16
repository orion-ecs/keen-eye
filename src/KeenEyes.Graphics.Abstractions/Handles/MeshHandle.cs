namespace KeenEyes.Graphics.Abstractions;

/// <summary>
/// An opaque handle to a mesh resource.
/// </summary>
/// <remarks>
/// <para>
/// Mesh handles are returned by <see cref="IResourceManager{THandle}"/> when creating meshes
/// and must be used to reference the mesh in draw calls and resource management operations.
/// </para>
/// <para>
/// Handles are opaque identifiers that avoid exposing backend-specific resource types,
/// enabling portability across different graphics APIs (OpenGL, Vulkan, DirectX, etc.).
/// </para>
/// </remarks>
/// <param name="Id">The internal identifier for this mesh resource.</param>
public readonly record struct MeshHandle(int Id)
{
    /// <summary>
    /// An invalid mesh handle representing no mesh.
    /// </summary>
    public static readonly MeshHandle Invalid = new(-1);

    /// <summary>
    /// Gets whether this handle refers to a valid mesh resource.
    /// </summary>
    /// <remarks>
    /// A valid handle has a non-negative ID. Note that a valid handle does not guarantee
    /// the resource still exists - it may have been disposed.
    /// </remarks>
    public bool IsValid => Id >= 0;

    /// <inheritdoc />
    public override string ToString() => IsValid ? $"Mesh({Id})" : "Mesh(Invalid)";
}
