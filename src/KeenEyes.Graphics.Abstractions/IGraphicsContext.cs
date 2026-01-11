using System.Numerics;

namespace KeenEyes.Graphics.Abstractions;

/// <summary>
/// High-level graphics API for rendering operations.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides the primary application-level API for graphics operations
/// in KeenEyes. It abstracts resource management (meshes, textures, shaders) and
/// provides rendering control without exposing low-level GPU details.
/// </para>
/// <para>
/// Unlike <see cref="IGraphicsDevice"/>, which exposes raw GPU operations,
/// <see cref="IGraphicsContext"/> uses opaque handles (<see cref="MeshHandle"/>,
/// <see cref="TextureHandle"/>, <see cref="ShaderHandle"/>) to reference resources.
/// </para>
/// <para>
/// The graphics context is initialized automatically when the window loads.
/// Use <see cref="ILoopProvider"/> (from <c>SilkWindowPlugin</c>) for the main
/// application loop.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create resources using opaque handles
/// var mesh = context.CreateMesh(vertices, indices);
/// var texture = context.CreateTexture(width, height, pixels);
/// var shader = context.CreateShader(vertexSource, fragmentSource);
///
/// // Render using handles
/// context.BindShader(shader);
/// context.BindTexture(texture);
/// context.DrawMesh(mesh);
/// </code>
/// </example>
public interface IGraphicsContext : IDisposable
{
    #region State

    /// <summary>
    /// Gets the associated window.
    /// </summary>
    IWindow? Window { get; }

    /// <summary>
    /// Gets the low-level graphics device.
    /// </summary>
    IGraphicsDevice? Device { get; }

    /// <summary>
    /// Gets whether the graphics context is initialized and ready for use.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Gets the current window width in pixels.
    /// </summary>
    int Width { get; }

    /// <summary>
    /// Gets the current window height in pixels.
    /// </summary>
    int Height { get; }

    #endregion

    #region Default Resources

    /// <summary>
    /// Gets the default lit shader handle.
    /// </summary>
    ShaderHandle LitShader { get; }

    /// <summary>
    /// Gets the default unlit shader handle.
    /// </summary>
    ShaderHandle UnlitShader { get; }

    /// <summary>
    /// Gets the default solid color shader handle.
    /// </summary>
    /// <remarks>
    /// The solid shader is used for rendering without lighting calculations.
    /// It applies a simple color without any lighting effects.
    /// </remarks>
    ShaderHandle SolidShader { get; }

    /// <summary>
    /// Gets the PBR (Physically Based Rendering) shader handle.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The PBR shader implements the metallic-roughness workflow with Cook-Torrance BRDF:
    /// </para>
    /// <list type="bullet">
    /// <item><description>GGX/Trowbridge-Reitz normal distribution</description></item>
    /// <item><description>Schlick-GGX geometry function</description></item>
    /// <item><description>Fresnel-Schlick approximation</description></item>
    /// <item><description>Support for up to 8 lights (directional, point, spot)</description></item>
    /// </list>
    /// <para>
    /// Required texture slots (bind to corresponding units):
    /// 0 = Base Color, 1 = Normal, 2 = MetallicRoughness (G=roughness, B=metallic),
    /// 3 = Occlusion (R channel), 4 = Emissive
    /// </para>
    /// </remarks>
    ShaderHandle PbrShader { get; }

    /// <summary>
    /// Gets a 1x1 white texture for solid color rendering.
    /// </summary>
    TextureHandle WhiteTexture { get; }

    /// <summary>
    /// Gets the instanced lit shader handle.
    /// </summary>
    /// <remarks>
    /// Use this shader for instanced rendering with lighting. The model matrix is read
    /// from per-instance vertex attributes instead of a uniform.
    /// </remarks>
    ShaderHandle InstancedLitShader { get; }

    /// <summary>
    /// Gets the instanced unlit shader handle.
    /// </summary>
    /// <remarks>
    /// Use this shader for instanced rendering without lighting. The model matrix is read
    /// from per-instance vertex attributes instead of a uniform.
    /// </remarks>
    ShaderHandle InstancedUnlitShader { get; }

    /// <summary>
    /// Gets the instanced solid color shader handle.
    /// </summary>
    /// <remarks>
    /// Use this shader for instanced solid color rendering. The model matrix is read
    /// from per-instance vertex attributes instead of a uniform.
    /// </remarks>
    ShaderHandle InstancedSolidShader { get; }

    #endregion

    #region Lifecycle Control

    /// <summary>
    /// Processes pending window events without blocking.
    /// </summary>
    void ProcessEvents();

    /// <summary>
    /// Swaps the front and back buffers.
    /// </summary>
    void SwapBuffers();

    #endregion

    #region Mesh Operations

    /// <summary>
    /// Creates a mesh from vertex and index data.
    /// </summary>
    /// <param name="vertices">The vertex data as a byte span.</param>
    /// <param name="vertexCount">The number of vertices.</param>
    /// <param name="indices">The index data.</param>
    /// <returns>The mesh handle.</returns>
    MeshHandle CreateMesh(ReadOnlySpan<byte> vertices, int vertexCount, ReadOnlySpan<uint> indices);

    /// <summary>
    /// Creates a cube mesh.
    /// </summary>
    /// <param name="size">The size of the cube (default: 1).</param>
    /// <returns>The mesh handle.</returns>
    MeshHandle CreateCube(float size = 1f);

    /// <summary>
    /// Creates a quad mesh (plane).
    /// </summary>
    /// <param name="width">The width of the quad (default: 1).</param>
    /// <param name="height">The height of the quad (default: 1).</param>
    /// <returns>The mesh handle.</returns>
    MeshHandle CreateQuad(float width = 1f, float height = 1f);

    /// <summary>
    /// Deletes a mesh resource.
    /// </summary>
    /// <param name="handle">The mesh handle.</param>
    void DeleteMesh(MeshHandle handle);

    /// <summary>
    /// Binds a mesh for rendering.
    /// </summary>
    /// <param name="handle">The mesh handle.</param>
    void BindMesh(MeshHandle handle);

    /// <summary>
    /// Draws the currently bound mesh.
    /// </summary>
    /// <param name="handle">The mesh handle.</param>
    void DrawMesh(MeshHandle handle);

    #endregion

    #region Texture Operations

    /// <summary>
    /// Creates a 2D texture from pixel data.
    /// </summary>
    /// <param name="width">The texture width in pixels.</param>
    /// <param name="height">The texture height in pixels.</param>
    /// <param name="pixels">The pixel data (RGBA format).</param>
    /// <returns>The texture handle.</returns>
    TextureHandle CreateTexture(int width, int height, ReadOnlySpan<byte> pixels);

    /// <summary>
    /// Creates a texture from a file.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>The texture handle.</returns>
    TextureHandle LoadTexture(string path);

    /// <summary>
    /// Creates a GPU-compressed texture from pre-compressed data (e.g., DDS with BC/DXT formats).
    /// </summary>
    /// <param name="width">The texture width in pixels.</param>
    /// <param name="height">The texture height in pixels.</param>
    /// <param name="format">The compressed texture format.</param>
    /// <param name="mipmaps">The mipmap chain data, from largest (level 0) to smallest.</param>
    /// <returns>The texture handle.</returns>
    /// <remarks>
    /// <para>
    /// Compressed textures remain compressed in GPU memory, reducing VRAM usage
    /// and memory bandwidth compared to uncompressed textures.
    /// </para>
    /// <para>
    /// Each mipmap level should contain the compressed block data for that level.
    /// The first element is the base texture (level 0), and subsequent elements
    /// are progressively smaller mipmap levels.
    /// </para>
    /// </remarks>
    TextureHandle CreateCompressedTexture(
        int width,
        int height,
        CompressedTextureFormat format,
        ReadOnlySpan<ReadOnlyMemory<byte>> mipmaps);

    /// <summary>
    /// Deletes a texture resource.
    /// </summary>
    /// <param name="handle">The texture handle.</param>
    void DeleteTexture(TextureHandle handle);

    /// <summary>
    /// Binds a texture to a texture unit.
    /// </summary>
    /// <param name="handle">The texture handle.</param>
    /// <param name="unit">The texture unit (default: 0).</param>
    void BindTexture(TextureHandle handle, int unit = 0);

    #endregion

    #region Shader Operations

    /// <summary>
    /// Creates a shader program from vertex and fragment source.
    /// </summary>
    /// <param name="vertexSource">The vertex shader source code.</param>
    /// <param name="fragmentSource">The fragment shader source code.</param>
    /// <returns>The shader handle.</returns>
    ShaderHandle CreateShader(string vertexSource, string fragmentSource);

    /// <summary>
    /// Deletes a shader program.
    /// </summary>
    /// <param name="handle">The shader handle.</param>
    void DeleteShader(ShaderHandle handle);

    /// <summary>
    /// Binds a shader program for rendering.
    /// </summary>
    /// <param name="handle">The shader handle.</param>
    void BindShader(ShaderHandle handle);

    /// <summary>
    /// Sets a float uniform on the currently bound shader.
    /// </summary>
    /// <param name="name">The uniform name.</param>
    /// <param name="value">The float value.</param>
    void SetUniform(string name, float value);

    /// <summary>
    /// Sets an integer uniform on the currently bound shader.
    /// </summary>
    /// <param name="name">The uniform name.</param>
    /// <param name="value">The integer value.</param>
    void SetUniform(string name, int value);

    /// <summary>
    /// Sets a Vector2 uniform on the currently bound shader.
    /// </summary>
    /// <param name="name">The uniform name.</param>
    /// <param name="value">The Vector2 value.</param>
    void SetUniform(string name, Vector2 value);

    /// <summary>
    /// Sets a Vector3 uniform on the currently bound shader.
    /// </summary>
    /// <param name="name">The uniform name.</param>
    /// <param name="value">The Vector3 value.</param>
    void SetUniform(string name, Vector3 value);

    /// <summary>
    /// Sets a Vector4 uniform on the currently bound shader.
    /// </summary>
    /// <param name="name">The uniform name.</param>
    /// <param name="value">The Vector4 value.</param>
    void SetUniform(string name, Vector4 value);

    /// <summary>
    /// Sets a Matrix4x4 uniform on the currently bound shader.
    /// </summary>
    /// <param name="name">The uniform name.</param>
    /// <param name="value">The Matrix4x4 value.</param>
    void SetUniform(string name, in Matrix4x4 value);

    #endregion

    #region Instance Buffer Operations

    /// <summary>
    /// Creates an instance buffer for GPU instanced rendering.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Instance buffers store per-instance data (model matrices, color tints) for rendering
    /// multiple instances of the same mesh with a single draw call. This dramatically reduces
    /// CPU overhead when rendering many similar objects.
    /// </para>
    /// <para>
    /// The buffer is allocated on the GPU with space for <paramref name="maxInstances"/> instances.
    /// Use <see cref="UpdateInstanceBuffer"/> to upload instance data before drawing.
    /// </para>
    /// </remarks>
    /// <param name="maxInstances">The maximum number of instances this buffer can hold.</param>
    /// <returns>The instance buffer handle.</returns>
    InstanceBufferHandle CreateInstanceBuffer(int maxInstances);

    /// <summary>
    /// Updates the instance data in an instance buffer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This uploads instance data to the GPU. Call this before drawing to update
    /// the positions, rotations, and color tints of instances.
    /// </para>
    /// <para>
    /// For best performance, try to batch updates and minimize calls per frame.
    /// The data span length must not exceed the buffer's maximum capacity.
    /// </para>
    /// </remarks>
    /// <param name="buffer">The instance buffer handle.</param>
    /// <param name="data">The instance data to upload.</param>
    void UpdateInstanceBuffer(InstanceBufferHandle buffer, ReadOnlySpan<InstanceData> data);

    /// <summary>
    /// Deletes an instance buffer and releases its GPU resources.
    /// </summary>
    /// <param name="buffer">The instance buffer handle.</param>
    void DeleteInstanceBuffer(InstanceBufferHandle buffer);

    /// <summary>
    /// Draws multiple instances of a mesh using instanced rendering.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This performs a single GPU draw call for all instances, where each instance
    /// uses data from the instance buffer (model matrix, color tint).
    /// </para>
    /// <para>
    /// The mesh must be bound before calling this method. The instance buffer must have
    /// been updated with at least <paramref name="instanceCount"/> instances of data.
    /// </para>
    /// </remarks>
    /// <param name="mesh">The mesh to draw.</param>
    /// <param name="instances">The instance buffer containing per-instance data.</param>
    /// <param name="instanceCount">The number of instances to draw.</param>
    void DrawMeshInstanced(MeshHandle mesh, InstanceBufferHandle instances, int instanceCount);

    #endregion

    #region Render State

    /// <summary>
    /// Sets the clear color.
    /// </summary>
    /// <param name="color">The clear color (RGBA).</param>
    void SetClearColor(Vector4 color);

    /// <summary>
    /// Clears the specified buffers.
    /// </summary>
    /// <param name="mask">The buffers to clear.</param>
    void Clear(ClearMask mask);

    /// <summary>
    /// Sets the viewport.
    /// </summary>
    /// <param name="x">The left edge in pixels.</param>
    /// <param name="y">The bottom edge in pixels.</param>
    /// <param name="width">The width in pixels.</param>
    /// <param name="height">The height in pixels.</param>
    void SetViewport(int x, int y, int width, int height);

    /// <summary>
    /// Enables or disables depth testing.
    /// </summary>
    /// <param name="enabled">True to enable depth testing.</param>
    void SetDepthTest(bool enabled);

    /// <summary>
    /// Enables or disables alpha blending.
    /// </summary>
    /// <param name="enabled">True to enable blending.</param>
    void SetBlending(bool enabled);

    /// <summary>
    /// Enables or disables face culling.
    /// </summary>
    /// <param name="enabled">True to enable culling.</param>
    /// <param name="mode">The face culling mode.</param>
    void SetCulling(bool enabled, CullFaceMode mode = CullFaceMode.Back);

    #endregion

    #region Render Target Operations

    /// <summary>
    /// Creates a render target (off-screen framebuffer) for rendering.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Render targets allow rendering to off-screen textures for effects like:
    /// shadow mapping, post-processing, environment map generation, and more.
    /// </para>
    /// <para>
    /// The returned handle can be used to bind the render target for rendering
    /// and to retrieve the color/depth textures for use in subsequent passes.
    /// </para>
    /// </remarks>
    /// <param name="width">The render target width in pixels.</param>
    /// <param name="height">The render target height in pixels.</param>
    /// <param name="format">The render target format.</param>
    /// <returns>A handle to the created render target.</returns>
    RenderTargetHandle CreateRenderTarget(int width, int height, RenderTargetFormat format);

    /// <summary>
    /// Creates a depth-only render target for shadow mapping.
    /// </summary>
    /// <remarks>
    /// Depth-only render targets have no color attachment, making them optimal
    /// for shadow map generation where only depth values are needed.
    /// </remarks>
    /// <param name="width">The render target width in pixels.</param>
    /// <param name="height">The render target height in pixels.</param>
    /// <returns>A handle to the created depth-only render target.</returns>
    RenderTargetHandle CreateDepthOnlyRenderTarget(int width, int height);

    /// <summary>
    /// Creates a cubemap render target for omnidirectional rendering.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Cubemap render targets are used for:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Point light shadow maps (omnidirectional shadows)</description></item>
    /// <item><description>Environment map generation</description></item>
    /// <item><description>IBL irradiance and specular convolution</description></item>
    /// </list>
    /// </remarks>
    /// <param name="size">The size of each cubemap face in pixels (faces are square).</param>
    /// <param name="withDepth">Whether to include a depth buffer.</param>
    /// <param name="mipLevels">The number of mip levels (1 for shadow maps, more for IBL).</param>
    /// <returns>A handle to the created cubemap render target.</returns>
    CubemapRenderTargetHandle CreateCubemapRenderTarget(int size, bool withDepth, int mipLevels = 1);

    /// <summary>
    /// Binds a render target for rendering.
    /// </summary>
    /// <remarks>
    /// All subsequent draw calls will render to this target until
    /// <see cref="UnbindRenderTarget"/> is called or another target is bound.
    /// </remarks>
    /// <param name="target">The render target to bind.</param>
    void BindRenderTarget(RenderTargetHandle target);

    /// <summary>
    /// Binds a specific face of a cubemap render target for rendering.
    /// </summary>
    /// <param name="target">The cubemap render target to bind.</param>
    /// <param name="face">The cubemap face to render to.</param>
    /// <param name="mipLevel">The mip level to render to (default: 0).</param>
    void BindCubemapRenderTarget(CubemapRenderTargetHandle target, CubemapFace face, int mipLevel = 0);

    /// <summary>
    /// Unbinds the current render target and restores rendering to the default framebuffer.
    /// </summary>
    void UnbindRenderTarget();

    /// <summary>
    /// Gets the color texture from a render target.
    /// </summary>
    /// <remarks>
    /// The returned texture handle can be bound to a shader for sampling
    /// the render target's color output in subsequent passes.
    /// </remarks>
    /// <param name="target">The render target.</param>
    /// <returns>A texture handle for the color attachment, or Invalid if depth-only.</returns>
    TextureHandle GetRenderTargetColorTexture(RenderTargetHandle target);

    /// <summary>
    /// Gets the depth texture from a render target.
    /// </summary>
    /// <remarks>
    /// The returned texture handle can be bound to a shader for sampling
    /// the render target's depth output in subsequent passes (e.g., shadow mapping).
    /// </remarks>
    /// <param name="target">The render target.</param>
    /// <returns>A texture handle for the depth attachment.</returns>
    TextureHandle GetRenderTargetDepthTexture(RenderTargetHandle target);

    /// <summary>
    /// Gets the cubemap texture from a cubemap render target.
    /// </summary>
    /// <param name="target">The cubemap render target.</param>
    /// <returns>A texture handle for the cubemap.</returns>
    TextureHandle GetCubemapRenderTargetTexture(CubemapRenderTargetHandle target);

    /// <summary>
    /// Deletes a render target and its associated resources.
    /// </summary>
    /// <param name="target">The render target to delete.</param>
    void DeleteRenderTarget(RenderTargetHandle target);

    /// <summary>
    /// Deletes a cubemap render target and its associated resources.
    /// </summary>
    /// <param name="target">The cubemap render target to delete.</param>
    void DeleteCubemapRenderTarget(CubemapRenderTargetHandle target);

    #endregion
}
