using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;

using KeenEyes.Graphics.Abstractions;
using KeenEyes.Graphics.Silk.Resources;

namespace KeenEyes.Graphics.Silk.Rendering2D;

/// <summary>
/// OpenGL implementation of <see cref="I2DRenderer"/> for Silk.NET.
/// </summary>
/// <remarks>
/// <para>
/// This renderer uses a batched approach where quads are accumulated in a vertex buffer
/// and flushed to the GPU when the batch is full or when a state change occurs.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "Requires real GPU context")]
public sealed class Silk2DRenderer : I2DRenderer
{
    private const int MaxQuads = 10000;
    private const int VerticesPerQuad = 4;
    private const int IndicesPerQuad = 6;
    private const int MaxVertices = MaxQuads * VerticesPerQuad;
    private const int MaxIndices = MaxQuads * IndicesPerQuad;

    private readonly IGraphicsDevice device;
    private readonly TextureManager textureManager;
    private readonly Vertex2D[] vertices;
    private readonly Stack<Rectangle> clipStack = new();

    private uint vao;
    private uint vbo;
    private uint ebo;
    private uint shaderProgram;
    private int projectionLocation;
    private int textureLocation;
    private uint whiteTexture;

    private int vertexCount;
    private int indexCount;
    private uint currentTexture;
    private bool isBatching;
    private Matrix4x4 projection;
    private Vector2 screenSize;
    private bool disposed;

    /// <summary>
    /// Creates a new 2D renderer.
    /// </summary>
    /// <param name="device">The graphics device.</param>
    /// <param name="textureManager">The texture manager.</param>
    /// <param name="screenWidth">Initial screen width.</param>
    /// <param name="screenHeight">Initial screen height.</param>
    internal Silk2DRenderer(IGraphicsDevice device, TextureManager textureManager, float screenWidth, float screenHeight)
    {
        this.device = device ?? throw new ArgumentNullException(nameof(device));
        this.textureManager = textureManager ?? throw new ArgumentNullException(nameof(textureManager));
        vertices = new Vertex2D[MaxVertices];
        screenSize = new Vector2(screenWidth, screenHeight);

        InitializeBuffers();
        InitializeShaders();
        CreateWhiteTexture();
        UpdateProjection();
    }

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

    private void InitializeBuffers()
    {
        vao = device.GenVertexArray();
        vbo = device.GenBuffer();
        ebo = device.GenBuffer();

        device.BindVertexArray(vao);

        // Vertex buffer
        device.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        var vertexSize = Marshal.SizeOf<Vertex2D>();
        device.BufferData(BufferTarget.ArrayBuffer, new byte[MaxVertices * vertexSize], BufferUsage.DynamicDraw);

        // Index buffer - generate indices for quads
        var indices = new ushort[MaxIndices];
        for (int i = 0, v = 0; i < MaxIndices; i += 6, v += 4)
        {
            indices[i + 0] = (ushort)(v + 0);
            indices[i + 1] = (ushort)(v + 1);
            indices[i + 2] = (ushort)(v + 2);
            indices[i + 3] = (ushort)(v + 2);
            indices[i + 4] = (ushort)(v + 3);
            indices[i + 5] = (ushort)(v + 0);
        }

        device.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
        device.BufferData(BufferTarget.ElementArrayBuffer, MemoryMarshal.AsBytes(indices.AsSpan()), BufferUsage.StaticDraw);

        // Vertex attributes
        // Position (vec2)
        device.EnableVertexAttribArray(0);
        device.VertexAttribPointer(0, 2, VertexAttribType.Float, false, (uint)vertexSize, 0);

        // TexCoord (vec2)
        device.EnableVertexAttribArray(1);
        device.VertexAttribPointer(1, 2, VertexAttribType.Float, false, (uint)vertexSize, 8);

        // Color (vec4)
        device.EnableVertexAttribArray(2);
        device.VertexAttribPointer(2, 4, VertexAttribType.Float, false, (uint)vertexSize, 16);

        device.BindVertexArray(0);
    }

    private void InitializeShaders()
    {
        var vertexShader = device.CreateShader(Abstractions.ShaderType.Vertex);
        device.ShaderSource(vertexShader, Shaders2D.VertexShader);
        device.CompileShader(vertexShader);

        if (!device.GetShaderCompileStatus(vertexShader))
        {
            var log = device.GetShaderInfoLog(vertexShader);
            throw new InvalidOperationException($"Failed to compile 2D vertex shader: {log}");
        }

        var fragmentShader = device.CreateShader(Abstractions.ShaderType.Fragment);
        device.ShaderSource(fragmentShader, Shaders2D.FragmentShader);
        device.CompileShader(fragmentShader);

        if (!device.GetShaderCompileStatus(fragmentShader))
        {
            var log = device.GetShaderInfoLog(fragmentShader);
            throw new InvalidOperationException($"Failed to compile 2D fragment shader: {log}");
        }

        shaderProgram = device.CreateProgram();
        device.AttachShader(shaderProgram, vertexShader);
        device.AttachShader(shaderProgram, fragmentShader);
        device.LinkProgram(shaderProgram);

        if (!device.GetProgramLinkStatus(shaderProgram))
        {
            var log = device.GetProgramInfoLog(shaderProgram);
            throw new InvalidOperationException($"Failed to link 2D shader program: {log}");
        }

        device.DetachShader(shaderProgram, vertexShader);
        device.DetachShader(shaderProgram, fragmentShader);
        device.DeleteShader(vertexShader);
        device.DeleteShader(fragmentShader);

        projectionLocation = device.GetUniformLocation(shaderProgram, "uProjection");
        textureLocation = device.GetUniformLocation(shaderProgram, "uTexture");
    }

    private void CreateWhiteTexture()
    {
        // Create a 1x1 white texture for solid color rendering
        whiteTexture = device.GenTexture();
        device.BindTexture(Abstractions.TextureTarget.Texture2D, whiteTexture);

        byte[] whitePixel = [255, 255, 255, 255];
        device.TexImage2D(Abstractions.TextureTarget.Texture2D, 0, 1, 1, Abstractions.PixelFormat.RGBA, whitePixel);
        device.TexParameter(Abstractions.TextureTarget.Texture2D, TextureParam.MinFilter, (int)TextureMinFilter.Nearest);
        device.TexParameter(Abstractions.TextureTarget.Texture2D, TextureParam.MagFilter, (int)TextureMagFilter.Nearest);

        device.BindTexture(Abstractions.TextureTarget.Texture2D, 0);
    }

    private void UpdateProjection()
    {
        // Orthographic projection: (0,0) at top-left, Y increasing downward
        projection = Matrix4x4.CreateOrthographicOffCenter(0, screenSize.X, screenSize.Y, 0, -1, 1);
    }

    #region I2DRenderer Implementation

    /// <inheritdoc />
    public void Begin()
    {
        Begin(projection);
    }

    /// <inheritdoc />
    public void Begin(in Matrix4x4 projection)
    {
        if (isBatching)
        {
            throw new InvalidOperationException("Begin() called while already batching. Call End() first.");
        }

        isBatching = true;
        vertexCount = 0;
        indexCount = 0;
        currentTexture = whiteTexture;

        device.UseProgram(shaderProgram);
        device.UniformMatrix4(projectionLocation, projection);
        device.Uniform1(textureLocation, 0);

        device.Enable(RenderCapability.Blend);
        device.BlendFunc(BlendFactor.SrcAlpha, BlendFactor.OneMinusSrcAlpha);
        device.Disable(RenderCapability.DepthTest);
    }

    /// <inheritdoc />
    public void End()
    {
        if (!isBatching)
        {
            return;
        }

        Flush();
        isBatching = false;
        clipStack.Clear();
        device.Disable(RenderCapability.ScissorTest);
    }

    /// <inheritdoc />
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
        device.BlendFunc(BlendFactor.SrcAlpha, BlendFactor.OneMinusSrcAlpha);

        device.BindVertexArray(vao);
        device.BindBuffer(BufferTarget.ArrayBuffer, vbo);

        // Upload vertex data
        var vertexData = MemoryMarshal.AsBytes(vertices.AsSpan(0, vertexCount));
        device.BufferData(BufferTarget.ArrayBuffer, vertexData, BufferUsage.DynamicDraw);

        // Bind texture
        device.ActiveTexture(Abstractions.TextureUnit.Texture0);
        device.BindTexture(Abstractions.TextureTarget.Texture2D, currentTexture);

        // Draw
        device.DrawElements(Abstractions.PrimitiveType.Triangles, (uint)indexCount, IndexType.UnsignedShort);

        device.BindVertexArray(0);

        vertexCount = 0;
        indexCount = 0;
    }

    /// <inheritdoc />
    public void FillRect(float x, float y, float width, float height, Vector4 color)
    {
        EnsureCapacity(4, 6);
        SetTexture(whiteTexture);

        AddQuad(x, y, width, height, 0, 0, 1, 1, color);
    }

    /// <inheritdoc />
    public void FillRect(in Rectangle rect, Vector4 color)
    {
        FillRect(rect.X, rect.Y, rect.Width, rect.Height, color);
    }

    /// <inheritdoc />
    public void DrawRect(float x, float y, float width, float height, Vector4 color, float thickness = 1f)
    {
        // Draw as 4 filled rectangles (top, bottom, left, right)
        FillRect(x, y, width, thickness, color); // Top
        FillRect(x, y + height - thickness, width, thickness, color); // Bottom
        FillRect(x, y + thickness, thickness, height - thickness * 2, color); // Left
        FillRect(x + width - thickness, y + thickness, thickness, height - thickness * 2, color); // Right
    }

    /// <inheritdoc />
    public void DrawRect(in Rectangle rect, Vector4 color, float thickness = 1f)
    {
        DrawRect(rect.X, rect.Y, rect.Width, rect.Height, color, thickness);
    }

    /// <inheritdoc />
    public void FillRoundedRect(float x, float y, float width, float height, float radius, Vector4 color)
    {
        // TODO: Implement proper rounded corners with custom index buffer or SDF shader
        // For now, draw as a regular rectangle since the pre-generated index buffer
        // only supports quads (not arbitrary triangles for rounded corners)
        FillRect(x, y, width, height, color);
    }

    /// <inheritdoc />
    public void DrawRoundedRect(float x, float y, float width, float height, float radius, Vector4 color, float thickness = 1f)
    {
        // Draw outer rounded rect minus inner rounded rect
        // For simplicity, draw as regular rect outline for now
        DrawRect(x, y, width, height, color, thickness);
    }

    /// <inheritdoc />
    public void DrawLine(float x1, float y1, float x2, float y2, Vector4 color, float thickness = 1f)
    {
        var dx = x2 - x1;
        var dy = y2 - y1;
        var len = MathF.Sqrt(dx * dx + dy * dy);

        if (len < 0.0001f)
        {
            return;
        }

        // Perpendicular vector
        var nx = -dy / len * thickness * 0.5f;
        var ny = dx / len * thickness * 0.5f;

        EnsureCapacity(4, 6);
        SetTexture(whiteTexture);

        vertices[vertexCount++] = new Vertex2D(new Vector2(x1 + nx, y1 + ny), Vector2.Zero, color);
        vertices[vertexCount++] = new Vertex2D(new Vector2(x1 - nx, y1 - ny), Vector2.Zero, color);
        vertices[vertexCount++] = new Vertex2D(new Vector2(x2 - nx, y2 - ny), Vector2.Zero, color);
        vertices[vertexCount++] = new Vertex2D(new Vector2(x2 + nx, y2 + ny), Vector2.Zero, color);
        indexCount += 6;
    }

    /// <inheritdoc />
    public void DrawLine(Vector2 start, Vector2 end, Vector4 color, float thickness = 1f)
    {
        DrawLine(start.X, start.Y, end.X, end.Y, color, thickness);
    }

    /// <inheritdoc />
    public void DrawLineStrip(ReadOnlySpan<Vector2> points, Vector4 color, float thickness = 1f)
    {
        for (int i = 0; i < points.Length - 1; i++)
        {
            DrawLine(points[i], points[i + 1], color, thickness);
        }
    }

    /// <inheritdoc />
    public void DrawPolygon(ReadOnlySpan<Vector2> points, Vector4 color, float thickness = 1f)
    {
        if (points.Length < 3)
        {
            return;
        }

        for (int i = 0; i < points.Length; i++)
        {
            DrawLine(points[i], points[(i + 1) % points.Length], color, thickness);
        }
    }

    /// <inheritdoc />
    public void FillCircle(float centerX, float centerY, float radius, Vector4 color, int segments = 32)
    {
        FillEllipse(centerX, centerY, radius, radius, color, segments);
    }

    /// <inheritdoc />
    public void DrawCircle(float centerX, float centerY, float radius, Vector4 color, float thickness = 1f, int segments = 32)
    {
        DrawEllipse(centerX, centerY, radius, radius, color, thickness, segments);
    }

    /// <inheritdoc />
    public void FillEllipse(float centerX, float centerY, float radiusX, float radiusY, Vector4 color, int segments = 32)
    {
        SetTexture(whiteTexture);

        for (int i = 0; i < segments; i++)
        {
            float angle1 = MathF.PI * 2 * i / segments;
            float angle2 = MathF.PI * 2 * (i + 1) / segments;

            float x1 = centerX + MathF.Cos(angle1) * radiusX;
            float y1 = centerY + MathF.Sin(angle1) * radiusY;
            float x2 = centerX + MathF.Cos(angle2) * radiusX;
            float y2 = centerY + MathF.Sin(angle2) * radiusY;

            EnsureCapacity(3, 3);

            vertices[vertexCount++] = new Vertex2D(new Vector2(centerX, centerY), Vector2.Zero, color);
            vertices[vertexCount++] = new Vertex2D(new Vector2(x1, y1), Vector2.Zero, color);
            vertices[vertexCount++] = new Vertex2D(new Vector2(x2, y2), Vector2.Zero, color);
            indexCount += 3;
        }
    }

    /// <inheritdoc />
    public void DrawEllipse(float centerX, float centerY, float radiusX, float radiusY, Vector4 color, float thickness = 1f, int segments = 32)
    {
        Span<Vector2> points = stackalloc Vector2[segments];

        for (int i = 0; i < segments; i++)
        {
            float angle = MathF.PI * 2 * i / segments;
            points[i] = new Vector2(
                centerX + MathF.Cos(angle) * radiusX,
                centerY + MathF.Sin(angle) * radiusY);
        }

        DrawPolygon(points, color, thickness);
    }

    /// <inheritdoc />
    public void DrawTexture(TextureHandle texture, float x, float y, Vector4? tint = null)
    {
        if (!texture.IsValid)
        {
            return;
        }

        var texData = textureManager.GetTexture(texture.Id);
        if (texData is null)
        {
            return;
        }

        DrawTexture(texture, x, y, texData.Width, texData.Height, tint);
    }

    /// <inheritdoc />
    public void DrawTexture(TextureHandle texture, float x, float y, float width, float height, Vector4? tint = null)
    {
        if (!texture.IsValid)
        {
            return;
        }

        var texData = textureManager.GetTexture(texture.Id);
        if (texData is null)
        {
            return;
        }

        EnsureCapacity(4, 6);
        SetTexture(texData.Handle);

        var color = tint ?? Vector4.One;
        AddQuad(x, y, width, height, 0, 0, 1, 1, color);
    }

    /// <inheritdoc />
    public void DrawTextureRegion(TextureHandle texture, in Rectangle destRect, in Rectangle sourceRect, Vector4? tint = null)
    {
        if (!texture.IsValid)
        {
            return;
        }

        var texData = textureManager.GetTexture(texture.Id);
        if (texData is null)
        {
            return;
        }

        EnsureCapacity(4, 6);
        SetTexture(texData.Handle);

        var color = tint ?? Vector4.One;
        AddQuad(destRect.X, destRect.Y, destRect.Width, destRect.Height,
            sourceRect.X, sourceRect.Y, sourceRect.Width, sourceRect.Height, color);
    }

    /// <inheritdoc />
    public void DrawTextureRotated(TextureHandle texture, in Rectangle destRect, float rotation, Vector2 origin, Vector4? tint = null)
    {
        // Simplified: just draw without rotation for now
        DrawTextureRegion(texture, destRect, new Rectangle(0, 0, 1, 1), tint);
    }

    /// <inheritdoc />
    public void PushClip(in Rectangle rect)
    {
        Flush(); // Flush before changing scissor

        var clip = clipStack.Count > 0
            ? clipStack.Peek().Intersection(rect)
            : rect;

        clipStack.Push(clip);
        ApplyClip(clip);
    }

    /// <inheritdoc />
    public void PopClip()
    {
        if (clipStack.Count == 0)
        {
            return;
        }

        Flush(); // Flush before changing scissor
        clipStack.Pop();

        if (clipStack.Count > 0)
        {
            ApplyClip(clipStack.Peek());
        }
        else
        {
            device.Disable(RenderCapability.ScissorTest);
        }
    }

    /// <inheritdoc />
    public void ClearClip()
    {
        Flush();
        clipStack.Clear();
        device.Disable(RenderCapability.ScissorTest);
    }

    private void ApplyClip(in Rectangle rect)
    {
        device.Enable(RenderCapability.ScissorTest);
        // OpenGL scissor uses bottom-left origin, so flip Y
        int y = (int)(screenSize.Y - rect.Y - rect.Height);
        device.Scissor((int)rect.X, y, (uint)rect.Width, (uint)rect.Height);
    }

    /// <inheritdoc />
    public void SetBatchHint(int count)
    {
        // No-op for this implementation
    }

    #endregion

    private void EnsureCapacity(int vertexCount, int indexCount)
    {
        if (this.vertexCount + vertexCount > MaxVertices || this.indexCount + indexCount > MaxIndices)
        {
            Flush();
        }
    }

    private void SetTexture(uint texture)
    {
        if (currentTexture != texture)
        {
            Flush();
            currentTexture = texture;
        }
    }

    private void AddQuad(float x, float y, float width, float height,
        float u, float v, float uWidth, float vHeight, Vector4 color)
    {
        vertices[vertexCount++] = new Vertex2D(new Vector2(x, y), new Vector2(u, v), color);
        vertices[vertexCount++] = new Vertex2D(new Vector2(x + width, y), new Vector2(u + uWidth, v), color);
        vertices[vertexCount++] = new Vertex2D(new Vector2(x + width, y + height), new Vector2(u + uWidth, v + vHeight), color);
        vertices[vertexCount++] = new Vertex2D(new Vector2(x, y + height), new Vector2(u, v + vHeight), color);
        indexCount += 6;
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

        if (whiteTexture != 0)
        {
            device.DeleteTexture(whiteTexture);
        }
    }

    /// <summary>
    /// Vertex structure for 2D rendering.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private readonly struct Vertex2D(Vector2 position, Vector2 texCoord, Vector4 color)
    {
        public readonly Vector2 Position = position;
        public readonly Vector2 TexCoord = texCoord;
        public readonly Vector4 Color = color;
    }
}
