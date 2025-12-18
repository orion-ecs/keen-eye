using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;

using FontStashSharp;
using FontStashSharp.Interfaces;

using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graphics.Silk.Text;

/// <summary>
/// OpenGL renderer implementation for FontStashSharp.
/// </summary>
/// <remarks>
/// <para>
/// This renderer receives vertex data from FontStashSharp and batches draw calls
/// to OpenGL for efficient text rendering. Uses FontStashSharp's native vertex format
/// directly without conversion.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "Requires real GPU context")]
internal sealed class FontStashRenderer : IFontStashRenderer2, IDisposable
{
    private const int MaxQuads = 10000;
    private const int VerticesPerQuad = 4;
    private const int IndicesPerQuad = 6;
    private const int MaxVertices = MaxQuads * VerticesPerQuad;
    private const int MaxIndices = MaxQuads * IndicesPerQuad;

    private readonly IGraphicsDevice device;
    // Use FontStashSharp's native vertex format directly
    private readonly VertexPositionColorTexture[] vertices;

    private uint vao;
    private uint vbo;
    private uint ebo;
    private uint shaderProgram;
    private int projectionLocation;
    private int textureLocation;

    private int vertexCount;
    private int indexCount;
    private uint currentTexture;
    private bool isBatching;
    private Matrix4x4 projection;
    private Vector2 screenSize;
    private bool disposed;

    /// <summary>
    /// Creates a new FontStashSharp renderer.
    /// </summary>
    /// <param name="device">The graphics device.</param>
    /// <param name="textureManager">The texture manager for FontStashSharp.</param>
    /// <param name="screenWidth">Initial screen width.</param>
    /// <param name="screenHeight">Initial screen height.</param>
    public FontStashRenderer(IGraphicsDevice device, ITexture2DManager textureManager, float screenWidth, float screenHeight)
    {
        this.device = device ?? throw new ArgumentNullException(nameof(device));
        TextureManager = textureManager ?? throw new ArgumentNullException(nameof(textureManager));
        vertices = new VertexPositionColorTexture[MaxVertices];
        screenSize = new Vector2(screenWidth, screenHeight);

        InitializeBuffers();
        InitializeShaders();
        UpdateProjection();
    }

    /// <inheritdoc />
    public ITexture2DManager TextureManager { get; }

    /// <summary>
    /// Updates the screen size for projection matrix calculation.
    /// </summary>
    /// <param name="width">The new screen width.</param>
    /// <param name="height">The new screen height.</param>
    public void SetScreenSize(float width, float height)
    {
        screenSize = new Vector2(width, height);
        UpdateProjection();
    }

    /// <summary>
    /// Begins a text rendering batch.
    /// </summary>
    public void Begin()
    {
        Begin(projection);
    }

    /// <summary>
    /// Begins a text rendering batch with a custom projection matrix.
    /// </summary>
    /// <param name="customProjection">The projection matrix to use.</param>
    public void Begin(in Matrix4x4 customProjection)
    {
        if (isBatching)
        {
            throw new InvalidOperationException("Begin() called while already batching. Call End() first.");
        }

        isBatching = true;
        vertexCount = 0;
        indexCount = 0;
        currentTexture = 0;

        device.UseProgram(shaderProgram);
        device.UniformMatrix4(projectionLocation, customProjection);
        device.Uniform1(textureLocation, 0);

        device.Enable(RenderCapability.Blend);
        // FontStashSharp uses premultiplied alpha
        device.BlendFunc(BlendFactor.One, BlendFactor.OneMinusSrcAlpha);
        device.Disable(RenderCapability.DepthTest);
    }

    /// <summary>
    /// Ends the text rendering batch and flushes remaining vertices.
    /// </summary>
    public void End()
    {
        if (!isBatching)
        {
            return;
        }

        Flush();
        isBatching = false;
    }

    /// <summary>
    /// Flushes the vertex buffer to the GPU.
    /// </summary>
    public void Flush()
    {
        if (vertexCount == 0)
        {
            return;
        }

        // Ensure our shader and state are active (may have been changed by other renderers)
        device.UseProgram(shaderProgram);
        device.UniformMatrix4(projectionLocation, projection);
        device.Uniform1(textureLocation, 0);
        device.Enable(RenderCapability.Blend);
        // FontStashSharp uses premultiplied alpha
        device.BlendFunc(BlendFactor.One, BlendFactor.OneMinusSrcAlpha);

        device.BindVertexArray(vao);
        device.BindBuffer(BufferTarget.ArrayBuffer, vbo);

        // Upload vertex data
        var vertexData = MemoryMarshal.AsBytes(vertices.AsSpan(0, vertexCount));
        device.BufferData(BufferTarget.ArrayBuffer, vertexData, BufferUsage.DynamicDraw);

        // Bind texture
        device.ActiveTexture(TextureUnit.Texture0);
        device.BindTexture(TextureTarget.Texture2D, currentTexture);

        // Draw
        device.DrawElements(PrimitiveType.Triangles, (uint)indexCount, IndexType.UnsignedShort);

        device.BindVertexArray(0);

        vertexCount = 0;
        indexCount = 0;
    }

    #region IFontStashRenderer2 Implementation

    /// <inheritdoc />
    public void DrawQuad(
        object texture,
        ref FontStashSharp.Interfaces.VertexPositionColorTexture topLeft,
        ref FontStashSharp.Interfaces.VertexPositionColorTexture topRight,
        ref FontStashSharp.Interfaces.VertexPositionColorTexture bottomLeft,
        ref FontStashSharp.Interfaces.VertexPositionColorTexture bottomRight)
    {
        var fontStashTexture = (FontStashTexture)texture;
        var textureHandle = fontStashTexture.TextureHandle;

        // Check if we need to change texture
        if (currentTexture != textureHandle && currentTexture != 0)
        {
            Flush();
        }

        currentTexture = textureHandle;

        // Ensure capacity
        if (vertexCount + VerticesPerQuad > MaxVertices || indexCount + IndicesPerQuad > MaxIndices)
        {
            Flush();
            currentTexture = textureHandle;
        }

        // Add vertices in same order as sample: topLeft, topRight, bottomLeft, bottomRight
        // Use the vertices directly without conversion
        vertices[vertexCount++] = topLeft;
        vertices[vertexCount++] = topRight;
        vertices[vertexCount++] = bottomLeft;
        vertices[vertexCount++] = bottomRight;
        indexCount += IndicesPerQuad;
    }

    #endregion

    private void InitializeBuffers()
    {
        vao = device.GenVertexArray();
        vbo = device.GenBuffer();
        ebo = device.GenBuffer();

        device.BindVertexArray(vao);

        // Vertex buffer - use FontStashSharp's VertexPositionColorTexture layout
        // Layout: Position (vec3, 12 bytes) + Color (4 bytes RGBA) + TexCoord (vec2, 8 bytes) = 24 bytes
        device.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        var vertexSize = Marshal.SizeOf<VertexPositionColorTexture>();
        device.BufferData(BufferTarget.ArrayBuffer, new byte[MaxVertices * vertexSize], BufferUsage.DynamicDraw);

        // Index buffer - generate indices for quads (matching FontStashSharp sample order)
        // Vertices are: 0=topLeft, 1=topRight, 2=bottomLeft, 3=bottomRight
        // Triangles: (0,1,2) and (3,2,1)
        var indices = new ushort[MaxIndices];
        for (int i = 0, v = 0; i < MaxIndices; i += 6, v += 4)
        {
            indices[i + 0] = (ushort)(v + 0);
            indices[i + 1] = (ushort)(v + 1);
            indices[i + 2] = (ushort)(v + 2);
            indices[i + 3] = (ushort)(v + 3);
            indices[i + 4] = (ushort)(v + 2);
            indices[i + 5] = (ushort)(v + 1);
        }

        device.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
        device.BufferData(BufferTarget.ElementArrayBuffer, MemoryMarshal.AsBytes(indices.AsSpan()), BufferUsage.StaticDraw);

        // Vertex attributes matching FontStashSharp sample
        // Position (vec3 at offset 0)
        device.EnableVertexAttribArray(0);
        device.VertexAttribPointer(0, 3, VertexAttribType.Float, false, (uint)vertexSize, 0);

        // Color (4 bytes UnsignedByte normalized at offset 12)
        device.EnableVertexAttribArray(1);
        device.VertexAttribPointer(1, 4, VertexAttribType.UnsignedByte, true, (uint)vertexSize, 12);

        // TexCoord (vec2 at offset 16)
        device.EnableVertexAttribArray(2);
        device.VertexAttribPointer(2, 2, VertexAttribType.Float, false, (uint)vertexSize, 16);

        device.BindVertexArray(0);
    }

    private void InitializeShaders()
    {
        var vertexShader = device.CreateShader(Abstractions.ShaderType.Vertex);
        device.ShaderSource(vertexShader, ShadersText.VertexShader);
        device.CompileShader(vertexShader);

        if (!device.GetShaderCompileStatus(vertexShader))
        {
            var log = device.GetShaderInfoLog(vertexShader);
            throw new InvalidOperationException($"Failed to compile text vertex shader: {log}");
        }

        var fragmentShader = device.CreateShader(Abstractions.ShaderType.Fragment);
        device.ShaderSource(fragmentShader, ShadersText.FragmentShader);
        device.CompileShader(fragmentShader);

        if (!device.GetShaderCompileStatus(fragmentShader))
        {
            var log = device.GetShaderInfoLog(fragmentShader);
            throw new InvalidOperationException($"Failed to compile text fragment shader: {log}");
        }

        shaderProgram = device.CreateProgram();
        device.AttachShader(shaderProgram, vertexShader);
        device.AttachShader(shaderProgram, fragmentShader);
        device.LinkProgram(shaderProgram);

        if (!device.GetProgramLinkStatus(shaderProgram))
        {
            var log = device.GetProgramInfoLog(shaderProgram);
            throw new InvalidOperationException($"Failed to link text shader program: {log}");
        }

        device.DetachShader(shaderProgram, vertexShader);
        device.DetachShader(shaderProgram, fragmentShader);
        device.DeleteShader(vertexShader);
        device.DeleteShader(fragmentShader);

        projectionLocation = device.GetUniformLocation(shaderProgram, "uProjection");
        textureLocation = device.GetUniformLocation(shaderProgram, "uTexture");
    }

    private void UpdateProjection()
    {
        // Orthographic projection: (0,0) at top-left, Y increasing downward
        projection = Matrix4x4.CreateOrthographicOffCenter(0, screenSize.X, screenSize.Y, 0, -1, 1);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        if (shaderProgram != 0)
        {
            device.DeleteProgram(shaderProgram);
        }

        if (vao != 0)
        {
            device.DeleteVertexArray(vao);
        }

        if (vbo != 0)
        {
            device.DeleteBuffer(vbo);
        }

        if (ebo != 0)
        {
            device.DeleteBuffer(ebo);
        }
    }
}
