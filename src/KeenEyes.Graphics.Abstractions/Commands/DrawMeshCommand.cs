using System.Numerics;

namespace KeenEyes.Graphics.Abstractions;

/// <summary>
/// Command to draw a mesh with the specified shader, texture, and transform.
/// </summary>
/// <param name="Mesh">The mesh to draw.</param>
/// <param name="Shader">The shader program to use.</param>
/// <param name="Texture">The texture to bind (use TextureHandle.Invalid for no texture).</param>
/// <param name="Transform">The model transformation matrix.</param>
/// <param name="SortKey">The sort key for batching and ordering.</param>
/// <remarks>
/// <para>
/// The sort key determines the execution order of draw commands:
/// <list type="bullet">
///   <item><description>For opaque objects: sort front-to-back to maximize depth test rejection</description></item>
///   <item><description>For transparent objects: sort back-to-front for correct blending</description></item>
///   <item><description>Group by material/shader to minimize state changes</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var draw = new DrawMeshCommand(
///     meshHandle,
///     shaderHandle,
///     textureHandle,
///     Matrix4x4.CreateTranslation(position),
///     sortKey);
/// </code>
/// </example>
public readonly record struct DrawMeshCommand(
    MeshHandle Mesh,
    ShaderHandle Shader,
    TextureHandle Texture,
    Matrix4x4 Transform,
    ulong SortKey) : IRenderCommand
{
    /// <summary>
    /// Creates a draw command with automatic sort key based on shader and depth.
    /// </summary>
    /// <param name="mesh">The mesh to draw.</param>
    /// <param name="shader">The shader program to use.</param>
    /// <param name="texture">The texture to bind.</param>
    /// <param name="transform">The model transformation matrix.</param>
    /// <param name="depth">The depth value for sorting (camera distance).</param>
    /// <returns>A draw command with computed sort key.</returns>
    public static DrawMeshCommand Create(
        MeshHandle mesh,
        ShaderHandle shader,
        TextureHandle texture,
        Matrix4x4 transform,
        float depth)
    {
        // Sort key format: [16 bits: shader ID][16 bits: texture ID][32 bits: depth as uint]
        // This groups draws by shader, then texture, then depth
        var depthBits = BitConverter.SingleToUInt32Bits(depth);
        var sortKey = ((ulong)(uint)shader.Id << 48) |
                      ((ulong)(uint)texture.Id << 32) |
                      depthBits;

        return new DrawMeshCommand(mesh, shader, texture, transform, sortKey);
    }

    /// <summary>
    /// Creates a draw command with identity transform.
    /// </summary>
    /// <param name="mesh">The mesh to draw.</param>
    /// <param name="shader">The shader program to use.</param>
    /// <param name="texture">The texture to bind.</param>
    /// <returns>A draw command with identity transform.</returns>
    public static DrawMeshCommand CreateSimple(
        MeshHandle mesh,
        ShaderHandle shader,
        TextureHandle texture) =>
        new(mesh, shader, texture, Matrix4x4.Identity, (ulong)shader.Id << 48);
}
