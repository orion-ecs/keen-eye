using System.Numerics;

namespace KeenEyes.Graphics.Abstractions;

/// <summary>
/// Abstraction layer for low-level graphics device operations.
/// </summary>
/// <remarks>
/// <para>
/// This interface abstracts GPU operations to enable:
/// <list type="bullet">
///   <item><description>Unit testing with mock implementations</description></item>
///   <item><description>Swapping graphics backends (OpenGL, Vulkan, DirectX, Metal, etc.)</description></item>
///   <item><description>Clean separation between rendering logic and GPU API</description></item>
/// </list>
/// </para>
/// <para>
/// This is a low-level interface intended for backend implementers.
/// Application code should typically use <see cref="IGraphicsContext"/> instead.
/// </para>
/// </remarks>
public interface IGraphicsDevice : IDisposable
{
    #region Buffer Operations

    /// <summary>
    /// Generates a new vertex array object.
    /// </summary>
    /// <returns>The VAO handle.</returns>
    uint GenVertexArray();

    /// <summary>
    /// Generates a new buffer object.
    /// </summary>
    /// <returns>The buffer handle.</returns>
    uint GenBuffer();

    /// <summary>
    /// Binds a vertex array object.
    /// </summary>
    /// <param name="vao">The VAO handle (0 to unbind).</param>
    void BindVertexArray(uint vao);

    /// <summary>
    /// Binds a buffer object.
    /// </summary>
    /// <param name="target">The buffer target.</param>
    /// <param name="buffer">The buffer handle.</param>
    void BindBuffer(BufferTarget target, uint buffer);

    /// <summary>
    /// Uploads data to a buffer.
    /// </summary>
    /// <param name="target">The buffer target.</param>
    /// <param name="data">The data to upload.</param>
    /// <param name="usage">The usage hint.</param>
    void BufferData(BufferTarget target, ReadOnlySpan<byte> data, BufferUsage usage);

    /// <summary>
    /// Deletes a vertex array object.
    /// </summary>
    /// <param name="vao">The VAO handle.</param>
    void DeleteVertexArray(uint vao);

    /// <summary>
    /// Deletes a buffer object.
    /// </summary>
    /// <param name="buffer">The buffer handle.</param>
    void DeleteBuffer(uint buffer);

    /// <summary>
    /// Enables a vertex attribute array.
    /// </summary>
    /// <param name="index">The attribute index.</param>
    void EnableVertexAttribArray(uint index);

    /// <summary>
    /// Specifies the layout of vertex attribute data.
    /// </summary>
    /// <param name="index">The attribute index.</param>
    /// <param name="size">Number of components (1-4).</param>
    /// <param name="type">The data type.</param>
    /// <param name="normalized">Whether to normalize integer data.</param>
    /// <param name="stride">Byte stride between vertices.</param>
    /// <param name="offset">Byte offset to the first component.</param>
    void VertexAttribPointer(uint index, int size, VertexAttribType type, bool normalized, uint stride, nuint offset);

    #endregion

    #region Texture Operations

    /// <summary>
    /// Generates a new texture object.
    /// </summary>
    /// <returns>The texture handle.</returns>
    uint GenTexture();

    /// <summary>
    /// Binds a texture object.
    /// </summary>
    /// <param name="target">The texture target.</param>
    /// <param name="texture">The texture handle.</param>
    void BindTexture(TextureTarget target, uint texture);

    /// <summary>
    /// Uploads 2D texture data.
    /// </summary>
    /// <param name="target">The texture target.</param>
    /// <param name="level">The mipmap level.</param>
    /// <param name="width">The texture width.</param>
    /// <param name="height">The texture height.</param>
    /// <param name="format">The pixel format.</param>
    /// <param name="data">The pixel data.</param>
    void TexImage2D(TextureTarget target, int level, int width, int height, PixelFormat format, ReadOnlySpan<byte> data);

    /// <summary>
    /// Sets a texture parameter (integer value).
    /// </summary>
    /// <param name="target">The texture target.</param>
    /// <param name="param">The parameter name.</param>
    /// <param name="value">The parameter value.</param>
    void TexParameter(TextureTarget target, TextureParam param, int value);

    /// <summary>
    /// Generates mipmaps for the bound texture.
    /// </summary>
    /// <param name="target">The texture target.</param>
    void GenerateMipmap(TextureTarget target);

    /// <summary>
    /// Deletes a texture object.
    /// </summary>
    /// <param name="texture">The texture handle.</param>
    void DeleteTexture(uint texture);

    /// <summary>
    /// Activates a texture unit.
    /// </summary>
    /// <param name="unit">The texture unit.</param>
    void ActiveTexture(TextureUnit unit);

    #endregion

    #region Shader Operations

    /// <summary>
    /// Creates a shader program.
    /// </summary>
    /// <returns>The program handle.</returns>
    uint CreateProgram();

    /// <summary>
    /// Creates a shader object.
    /// </summary>
    /// <param name="type">The shader type.</param>
    /// <returns>The shader handle.</returns>
    uint CreateShader(ShaderType type);

    /// <summary>
    /// Sets the source code for a shader.
    /// </summary>
    /// <param name="shader">The shader handle.</param>
    /// <param name="source">The shader source code.</param>
    void ShaderSource(uint shader, string source);

    /// <summary>
    /// Compiles a shader.
    /// </summary>
    /// <param name="shader">The shader handle.</param>
    void CompileShader(uint shader);

    /// <summary>
    /// Gets the compile status of a shader.
    /// </summary>
    /// <param name="shader">The shader handle.</param>
    /// <returns>True if compilation succeeded.</returns>
    bool GetShaderCompileStatus(uint shader);

    /// <summary>
    /// Gets the shader info log (errors/warnings).
    /// </summary>
    /// <param name="shader">The shader handle.</param>
    /// <returns>The info log string.</returns>
    string GetShaderInfoLog(uint shader);

    /// <summary>
    /// Attaches a shader to a program.
    /// </summary>
    /// <param name="program">The program handle.</param>
    /// <param name="shader">The shader handle.</param>
    void AttachShader(uint program, uint shader);

    /// <summary>
    /// Detaches a shader from a program.
    /// </summary>
    /// <param name="program">The program handle.</param>
    /// <param name="shader">The shader handle.</param>
    void DetachShader(uint program, uint shader);

    /// <summary>
    /// Links a shader program.
    /// </summary>
    /// <param name="program">The program handle.</param>
    void LinkProgram(uint program);

    /// <summary>
    /// Gets the link status of a program.
    /// </summary>
    /// <param name="program">The program handle.</param>
    /// <returns>True if linking succeeded.</returns>
    bool GetProgramLinkStatus(uint program);

    /// <summary>
    /// Gets the program info log (errors/warnings).
    /// </summary>
    /// <param name="program">The program handle.</param>
    /// <returns>The info log string.</returns>
    string GetProgramInfoLog(uint program);

    /// <summary>
    /// Deletes a shader object.
    /// </summary>
    /// <param name="shader">The shader handle.</param>
    void DeleteShader(uint shader);

    /// <summary>
    /// Deletes a shader program.
    /// </summary>
    /// <param name="program">The program handle.</param>
    void DeleteProgram(uint program);

    /// <summary>
    /// Activates a shader program for rendering.
    /// </summary>
    /// <param name="program">The program handle (0 to unbind).</param>
    void UseProgram(uint program);

    /// <summary>
    /// Gets the location of a uniform variable.
    /// </summary>
    /// <param name="program">The program handle.</param>
    /// <param name="name">The uniform name.</param>
    /// <returns>The location, or -1 if not found.</returns>
    int GetUniformLocation(uint program, string name);

    /// <summary>
    /// Sets a float uniform.
    /// </summary>
    /// <param name="location">The uniform location.</param>
    /// <param name="value">The float value.</param>
    void Uniform1(int location, float value);

    /// <summary>
    /// Sets an int uniform.
    /// </summary>
    /// <param name="location">The uniform location.</param>
    /// <param name="value">The integer value.</param>
    void Uniform1(int location, int value);

    /// <summary>
    /// Sets a vec2 uniform.
    /// </summary>
    /// <param name="location">The uniform location.</param>
    /// <param name="x">The X component.</param>
    /// <param name="y">The Y component.</param>
    void Uniform2(int location, float x, float y);

    /// <summary>
    /// Sets a vec3 uniform.
    /// </summary>
    /// <param name="location">The uniform location.</param>
    /// <param name="x">The X component.</param>
    /// <param name="y">The Y component.</param>
    /// <param name="z">The Z component.</param>
    void Uniform3(int location, float x, float y, float z);

    /// <summary>
    /// Sets a vec4 uniform.
    /// </summary>
    /// <param name="location">The uniform location.</param>
    /// <param name="x">The X component.</param>
    /// <param name="y">The Y component.</param>
    /// <param name="z">The Z component.</param>
    /// <param name="w">The W component.</param>
    void Uniform4(int location, float x, float y, float z, float w);

    /// <summary>
    /// Sets a mat4 uniform.
    /// </summary>
    /// <param name="location">The uniform location.</param>
    /// <param name="matrix">The 4x4 matrix.</param>
    void UniformMatrix4(int location, in Matrix4x4 matrix);

    #endregion

    #region Rendering Operations

    /// <summary>
    /// Sets the clear color.
    /// </summary>
    /// <param name="r">Red component (0-1).</param>
    /// <param name="g">Green component (0-1).</param>
    /// <param name="b">Blue component (0-1).</param>
    /// <param name="a">Alpha component (0-1).</param>
    void ClearColor(float r, float g, float b, float a);

    /// <summary>
    /// Clears the specified buffers.
    /// </summary>
    /// <param name="mask">The buffers to clear.</param>
    void Clear(ClearMask mask);

    /// <summary>
    /// Enables a render capability.
    /// </summary>
    /// <param name="cap">The capability to enable.</param>
    void Enable(RenderCapability cap);

    /// <summary>
    /// Disables a render capability.
    /// </summary>
    /// <param name="cap">The capability to disable.</param>
    void Disable(RenderCapability cap);

    /// <summary>
    /// Sets the face culling mode.
    /// </summary>
    /// <param name="mode">The faces to cull.</param>
    void CullFace(CullFaceMode mode);

    /// <summary>
    /// Sets the viewport.
    /// </summary>
    /// <param name="x">The left edge in pixels.</param>
    /// <param name="y">The bottom edge in pixels.</param>
    /// <param name="width">The width in pixels.</param>
    /// <param name="height">The height in pixels.</param>
    void Viewport(int x, int y, uint width, uint height);

    /// <summary>
    /// Sets the scissor rectangle for scissor testing.
    /// </summary>
    /// <param name="x">The left edge in pixels.</param>
    /// <param name="y">The bottom edge in pixels.</param>
    /// <param name="width">The width in pixels.</param>
    /// <param name="height">The height in pixels.</param>
    void Scissor(int x, int y, uint width, uint height);

    /// <summary>
    /// Sets the blend function.
    /// </summary>
    /// <param name="srcFactor">The source blend factor.</param>
    /// <param name="dstFactor">The destination blend factor.</param>
    void BlendFunc(BlendFactor srcFactor, BlendFactor dstFactor);

    /// <summary>
    /// Sets the depth comparison function.
    /// </summary>
    /// <param name="func">The depth function.</param>
    void DepthFunc(DepthFunction func);

    /// <summary>
    /// Draws indexed primitives.
    /// </summary>
    /// <param name="mode">The primitive type.</param>
    /// <param name="count">The number of indices.</param>
    /// <param name="type">The index data type.</param>
    void DrawElements(PrimitiveType mode, uint count, IndexType type);

    /// <summary>
    /// Draws non-indexed primitives.
    /// </summary>
    /// <param name="mode">The primitive type.</param>
    /// <param name="first">The starting vertex index.</param>
    /// <param name="count">The number of vertices.</param>
    void DrawArrays(PrimitiveType mode, int first, uint count);

    /// <summary>
    /// Sets the line width for line primitives.
    /// </summary>
    /// <param name="width">The line width in pixels.</param>
    void LineWidth(float width);

    /// <summary>
    /// Sets the point size for point primitives.
    /// </summary>
    /// <param name="size">The point size in pixels.</param>
    void PointSize(float size);

    #endregion
}
