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
public interface IGraphicsContext : ILoopProvider, IDisposable
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

    // IsInitialized is inherited from ILoopProvider

    /// <summary>
    /// Gets whether the window should close.
    /// </summary>
    bool ShouldClose { get; }

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
    /// Gets a 1x1 white texture for solid color rendering.
    /// </summary>
    TextureHandle WhiteTexture { get; }

    #endregion

    #region Lifecycle Events (inherited from ILoopProvider)

    // The following events are inherited from ILoopProvider:
    // - OnReady: Raised when the window is loaded and ready
    // - OnUpdate: Raised each frame for updates (float deltaTime)
    // - OnRender: Raised each frame for rendering (float deltaTime)
    // - OnResize: Raised when the window is resized (int width, int height)
    // - OnClosing: Raised when the window is closing

    #endregion

    #region Lifecycle Control

    // Initialize() and Run() are inherited from ILoopProvider
    // IsInitialized is inherited from ILoopProvider

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
}
