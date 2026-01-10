namespace KeenEyes.Graphics.Abstractions;

/// <summary>
/// An opaque handle to an instance buffer resource for GPU instanced rendering.
/// </summary>
/// <remarks>
/// <para>
/// Instance buffer handles are used to reference GPU buffers that contain per-instance data
/// for instanced draw calls. Each instance buffer can hold data for multiple instances of
/// the same mesh to be rendered in a single draw call.
/// </para>
/// <para>
/// Handles are opaque identifiers that avoid exposing backend-specific resource types,
/// enabling portability across different graphics APIs (OpenGL, Vulkan, DirectX, etc.).
/// </para>
/// </remarks>
/// <param name="Id">The internal identifier for this instance buffer resource.</param>
public readonly record struct InstanceBufferHandle(int Id)
{
    /// <summary>
    /// An invalid instance buffer handle representing no buffer.
    /// </summary>
    public static readonly InstanceBufferHandle Invalid = new(-1);

    /// <summary>
    /// Gets whether this handle refers to a valid instance buffer resource.
    /// </summary>
    /// <remarks>
    /// A valid handle has a non-negative ID. Note that a valid handle does not guarantee
    /// the resource still exists - it may have been disposed.
    /// </remarks>
    public bool IsValid => Id >= 0;

    /// <inheritdoc />
    public override string ToString() => IsValid ? $"InstanceBuffer({Id})" : "InstanceBuffer(Invalid)";
}
