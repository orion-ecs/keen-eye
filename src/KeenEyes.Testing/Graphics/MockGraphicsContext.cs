using System.Numerics;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Testing.Graphics;

/// <summary>
/// A mock implementation of <see cref="IGraphicsContext"/> for testing high-level
/// graphics operations without a real GPU.
/// </summary>
/// <remarks>
/// <para>
/// MockGraphicsContext tracks all resource creation, binding, and drawing operations,
/// enabling verification of rendering code without actual GPU calls.
/// </para>
/// <para>
/// Use the tracking collections (<see cref="Meshes"/>, <see cref="Textures"/>, etc.)
/// and draw call lists to verify rendering behavior.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var context = new MockGraphicsContext();
///
/// var mesh = context.CreateCube();
/// var shader = context.CreateShader("vertex", "fragment");
///
/// context.BindShader(shader);
/// context.DrawMesh(mesh);
///
/// context.MeshDrawCalls.Should().HaveCount(1);
/// context.BoundShader.Should().Be(shader);
/// </code>
/// </example>
public sealed class MockGraphicsContext : IGraphicsContext
{
    private int nextHandleId = 1;
    private bool disposed;
    private ShaderHandle boundShader;
    private MeshHandle boundMesh;
    private readonly Dictionary<int, TextureHandle> boundTextures = [];

    /// <summary>
    /// Default texture size for textures loaded via <see cref="LoadTexture"/> (default: 64).
    /// </summary>
    public int MockDefaultTextureSize { get; set; } = 64;

    /// <summary>
    /// Creates a new mock graphics context.
    /// </summary>
    public MockGraphicsContext()
    {
        // Create default resources
        LitShader = AllocateShaderHandle();
        Shaders[LitShader] = new MockShaderInfo("lit_vertex", "lit_fragment");

        UnlitShader = AllocateShaderHandle();
        Shaders[UnlitShader] = new MockShaderInfo("unlit_vertex", "unlit_fragment");

        SolidShader = AllocateShaderHandle();
        Shaders[SolidShader] = new MockShaderInfo("solid_vertex", "solid_fragment");

        PbrShader = AllocateShaderHandle();
        Shaders[PbrShader] = new MockShaderInfo("pbr_vertex", "pbr_fragment");

        PbrShadowShader = AllocateShaderHandle();
        Shaders[PbrShadowShader] = new MockShaderInfo("pbr_shadow_vertex", "pbr_shadow_fragment");

        InstancedLitShader = AllocateShaderHandle();
        Shaders[InstancedLitShader] = new MockShaderInfo("instanced_lit_vertex", "lit_fragment");

        InstancedUnlitShader = AllocateShaderHandle();
        Shaders[InstancedUnlitShader] = new MockShaderInfo("instanced_unlit_vertex", "unlit_fragment");

        InstancedSolidShader = AllocateShaderHandle();
        Shaders[InstancedSolidShader] = new MockShaderInfo("instanced_solid_vertex", "solid_fragment");

        WhiteTexture = AllocateTextureHandle(1, 1);
        Textures[WhiteTexture] = new MockTextureInfo(1, 1, null);
    }

    #region Resource Tracking

    /// <summary>
    /// Gets the dictionary of created meshes by handle.
    /// </summary>
    public Dictionary<MeshHandle, MockMeshInfo> Meshes { get; } = [];

    /// <summary>
    /// Gets the dictionary of created textures by handle.
    /// </summary>
    public Dictionary<TextureHandle, MockTextureInfo> Textures { get; } = [];

    /// <summary>
    /// Gets the dictionary of created shaders by handle.
    /// </summary>
    public Dictionary<ShaderHandle, MockShaderInfo> Shaders { get; } = [];

    /// <summary>
    /// Gets the dictionary of created instance buffers by handle.
    /// </summary>
    public Dictionary<InstanceBufferHandle, MockInstanceBufferInfo> InstanceBuffers { get; } = [];

    /// <summary>
    /// Gets the list of all mesh draw calls.
    /// </summary>
    public List<MeshDrawCall> MeshDrawCalls { get; } = [];

    /// <summary>
    /// Gets the list of all instanced mesh draw calls.
    /// </summary>
    public List<InstancedMeshDrawCall> InstancedMeshDrawCalls { get; } = [];

    /// <summary>
    /// Gets the currently bound shader.
    /// </summary>
    public ShaderHandle BoundShader => boundShader;

    /// <summary>
    /// Gets the currently bound mesh.
    /// </summary>
    public MeshHandle BoundMesh => boundMesh;

    /// <summary>
    /// Gets the dictionary of bound textures by unit.
    /// </summary>
    public IReadOnlyDictionary<int, TextureHandle> BoundTextures => boundTextures;

    /// <summary>
    /// Gets the current render state.
    /// </summary>
    public MockContextRenderState RenderState { get; } = new();

    /// <summary>
    /// Gets the dictionary of uniform values set on the current shader.
    /// </summary>
    public Dictionary<string, object> UniformValues { get; } = [];

    #endregion

    #region IGraphicsContext Properties

    /// <inheritdoc />
    public IWindow? Window { get; set; }

    /// <inheritdoc />
    public IGraphicsDevice? Device { get; set; }

    /// <inheritdoc />
    public bool IsInitialized { get; set; } = true;

    /// <inheritdoc />
    public int Width { get; set; } = 800;

    /// <inheritdoc />
    public int Height { get; set; } = 600;

    /// <inheritdoc />
    public ShaderHandle LitShader { get; }

    /// <inheritdoc />
    public ShaderHandle UnlitShader { get; }

    /// <inheritdoc />
    public ShaderHandle SolidShader { get; }

    /// <inheritdoc />
    public ShaderHandle PbrShader { get; }

    /// <inheritdoc />
    public ShaderHandle PbrShadowShader { get; }

    /// <inheritdoc />
    public TextureHandle WhiteTexture { get; }

    /// <inheritdoc />
    public ShaderHandle InstancedLitShader { get; }

    /// <inheritdoc />
    public ShaderHandle InstancedUnlitShader { get; }

    /// <inheritdoc />
    public ShaderHandle InstancedSolidShader { get; }

    #endregion

    #region Lifecycle

    /// <inheritdoc />
    public void ProcessEvents()
    {
        RenderState.ProcessEventsCount++;
    }

    /// <inheritdoc />
    public void SwapBuffers()
    {
        RenderState.SwapBuffersCount++;
    }

    #endregion

    #region Mesh Operations

    /// <inheritdoc />
    public MeshHandle CreateMesh(ReadOnlySpan<byte> vertices, int vertexCount, ReadOnlySpan<uint> indices)
    {
        var handle = AllocateMeshHandle();
        Meshes[handle] = new MockMeshInfo(vertexCount, indices.Length, vertices.ToArray(), indices.ToArray());
        return handle;
    }

    /// <inheritdoc />
    public MeshHandle CreateCube(float size = 1f)
    {
        var handle = AllocateMeshHandle();
        Meshes[handle] = new MockMeshInfo(24, 36, null, null) { IsCube = true, Size = size };
        return handle;
    }

    /// <inheritdoc />
    public MeshHandle CreateQuad(float width = 1f, float height = 1f)
    {
        var handle = AllocateMeshHandle();
        Meshes[handle] = new MockMeshInfo(4, 6, null, null) { IsQuad = true, Width = width, Height = height };
        return handle;
    }

    /// <inheritdoc />
    public void DeleteMesh(MeshHandle handle)
    {
        Meshes.Remove(handle);
        if (boundMesh == handle)
        {
            boundMesh = default;
        }
    }

    /// <inheritdoc />
    public void BindMesh(MeshHandle handle)
    {
        boundMesh = handle;
    }

    /// <inheritdoc />
    public void DrawMesh(MeshHandle handle)
    {
        MeshDrawCalls.Add(new MeshDrawCall(
            handle,
            boundShader,
            new Dictionary<int, TextureHandle>(boundTextures),
            new Dictionary<string, object>(UniformValues)));
    }

    #endregion

    #region Texture Operations

    /// <inheritdoc />
    public TextureHandle CreateTexture(int width, int height, ReadOnlySpan<byte> pixels)
    {
        var handle = AllocateTextureHandle(width, height);
        Textures[handle] = new MockTextureInfo(width, height, null) { Data = pixels.ToArray() };
        return handle;
    }

    /// <inheritdoc />
    public TextureHandle LoadTexture(string path)
    {
        // In mock context, we don't know dimensions - use 0,0 (callers should set MockDefaultTextureSize)
        var handle = AllocateTextureHandle(MockDefaultTextureSize, MockDefaultTextureSize);
        Textures[handle] = new MockTextureInfo(MockDefaultTextureSize, MockDefaultTextureSize, path);
        return handle;
    }

    /// <inheritdoc />
    public void DeleteTexture(TextureHandle handle)
    {
        Textures.Remove(handle);
        foreach (var unit in boundTextures.Where(kv => kv.Value == handle).Select(kv => kv.Key).ToList())
        {
            boundTextures.Remove(unit);
        }
    }

    /// <inheritdoc />
    public void BindTexture(TextureHandle handle, int unit = 0)
    {
        if (handle.Id == 0)
        {
            boundTextures.Remove(unit);
        }
        else
        {
            boundTextures[unit] = handle;
        }
    }

    /// <inheritdoc />
    public TextureHandle CreateCompressedTexture(
        int width,
        int height,
        CompressedTextureFormat format,
        ReadOnlySpan<ReadOnlyMemory<byte>> mipmaps)
    {
        var handle = AllocateTextureHandle(width, height);
        var textureInfo = new MockTextureInfo(width, height, null)
        {
            CompressedFormat = format,
            MipmapCount = mipmaps.Length
        };

        // Store the base level data if provided
        if (mipmaps.Length > 0)
        {
            textureInfo.Data = mipmaps[0].ToArray();
        }

        Textures[handle] = textureInfo;
        return handle;
    }

    #endregion

    #region Shader Operations

    /// <inheritdoc />
    public ShaderHandle CreateShader(string vertexSource, string fragmentSource)
    {
        var handle = AllocateShaderHandle();
        Shaders[handle] = new MockShaderInfo(vertexSource, fragmentSource);
        return handle;
    }

    /// <inheritdoc />
    public void DeleteShader(ShaderHandle handle)
    {
        // Don't delete default shaders
        if (handle == LitShader || handle == UnlitShader || handle == SolidShader ||
            handle == PbrShader || handle == PbrShadowShader)
        {
            return;
        }

        Shaders.Remove(handle);
        if (boundShader == handle)
        {
            boundShader = default;
        }
    }

    /// <inheritdoc />
    public void BindShader(ShaderHandle handle)
    {
        boundShader = handle;
        UniformValues.Clear();
    }

    /// <inheritdoc />
    public void SetUniform(string name, float value)
    {
        UniformValues[name] = value;
    }

    /// <inheritdoc />
    public void SetUniform(string name, int value)
    {
        UniformValues[name] = value;
    }

    /// <inheritdoc />
    public void SetUniform(string name, Vector2 value)
    {
        UniformValues[name] = value;
    }

    /// <inheritdoc />
    public void SetUniform(string name, Vector3 value)
    {
        UniformValues[name] = value;
    }

    /// <inheritdoc />
    public void SetUniform(string name, Vector4 value)
    {
        UniformValues[name] = value;
    }

    /// <inheritdoc />
    public void SetUniform(string name, in Matrix4x4 value)
    {
        UniformValues[name] = value;
    }

    #endregion

    #region Instance Buffer Operations

    /// <inheritdoc />
    public InstanceBufferHandle CreateInstanceBuffer(int maxInstances)
    {
        var handle = AllocateInstanceBufferHandle();
        InstanceBuffers[handle] = new MockInstanceBufferInfo(maxInstances);
        return handle;
    }

    /// <inheritdoc />
    public void UpdateInstanceBuffer(InstanceBufferHandle buffer, ReadOnlySpan<InstanceData> data)
    {
        if (InstanceBuffers.TryGetValue(buffer, out var info))
        {
            info.Data = data.ToArray();
            info.CurrentInstanceCount = data.Length;
        }
    }

    /// <inheritdoc />
    public void DeleteInstanceBuffer(InstanceBufferHandle buffer)
    {
        InstanceBuffers.Remove(buffer);
    }

    /// <inheritdoc />
    public void DrawMeshInstanced(MeshHandle mesh, InstanceBufferHandle instances, int instanceCount)
    {
        InstancedMeshDrawCalls.Add(new InstancedMeshDrawCall(
            mesh,
            instances,
            instanceCount,
            boundShader,
            new Dictionary<int, TextureHandle>(boundTextures),
            new Dictionary<string, object>(UniformValues)));
    }

    private InstanceBufferHandle AllocateInstanceBufferHandle()
    {
        return new InstanceBufferHandle(nextHandleId++);
    }

    #endregion

    #region Render State

    /// <inheritdoc />
    public void SetClearColor(Vector4 color)
    {
        RenderState.ClearColor = color;
    }

    /// <inheritdoc />
    public void Clear(ClearMask mask)
    {
        RenderState.LastClearMask = mask;
        RenderState.ClearCount++;
    }

    /// <inheritdoc />
    public void SetViewport(int x, int y, int width, int height)
    {
        RenderState.Viewport = (x, y, width, height);
    }

    /// <inheritdoc />
    public void SetDepthTest(bool enabled)
    {
        RenderState.DepthTestEnabled = enabled;
    }

    /// <inheritdoc />
    public void SetBlending(bool enabled)
    {
        RenderState.BlendingEnabled = enabled;
    }

    /// <inheritdoc />
    public void SetCulling(bool enabled, CullFaceMode mode = CullFaceMode.Back)
    {
        RenderState.CullingEnabled = enabled;
        RenderState.CullFaceMode = mode;
    }

    #endregion

    #region Render Target Operations

    /// <summary>
    /// Gets the dictionary of created render targets by handle.
    /// </summary>
    public Dictionary<RenderTargetHandle, MockRenderTargetInfo> RenderTargets { get; } = [];

    /// <summary>
    /// Gets the dictionary of created cubemap render targets by handle.
    /// </summary>
    public Dictionary<CubemapRenderTargetHandle, MockCubemapRenderTargetInfo> CubemapRenderTargets { get; } = [];

    /// <summary>
    /// Gets the currently bound render target, or null if rendering to default framebuffer.
    /// </summary>
    public RenderTargetHandle? BoundRenderTarget { get; private set; }

    /// <summary>
    /// Gets the currently bound cubemap render target info, or null if not bound.
    /// </summary>
    public (CubemapRenderTargetHandle Handle, CubemapFace Face, int MipLevel)? BoundCubemapRenderTarget { get; private set; }

    /// <inheritdoc />
    public RenderTargetHandle CreateRenderTarget(int width, int height, RenderTargetFormat format)
    {
        var handle = new RenderTargetHandle(nextHandleId++, width, height, format);
        RenderTargets[handle] = new MockRenderTargetInfo(width, height, format);
        return handle;
    }

    /// <inheritdoc />
    public RenderTargetHandle CreateDepthOnlyRenderTarget(int width, int height)
    {
        var handle = new RenderTargetHandle(nextHandleId++, width, height, RenderTargetFormat.Depth32F);
        RenderTargets[handle] = new MockRenderTargetInfo(width, height, RenderTargetFormat.Depth32F) { IsDepthOnly = true };
        return handle;
    }

    /// <inheritdoc />
    public CubemapRenderTargetHandle CreateCubemapRenderTarget(int size, bool withDepth, int mipLevels = 1)
    {
        var handle = new CubemapRenderTargetHandle(nextHandleId++, size, withDepth, mipLevels);
        CubemapRenderTargets[handle] = new MockCubemapRenderTargetInfo(size, withDepth, mipLevels);
        return handle;
    }

    /// <inheritdoc />
    public void BindRenderTarget(RenderTargetHandle target)
    {
        BoundRenderTarget = target.IsValid ? target : null;
        BoundCubemapRenderTarget = null;
    }

    /// <inheritdoc />
    public void BindCubemapRenderTarget(CubemapRenderTargetHandle target, CubemapFace face, int mipLevel = 0)
    {
        BoundCubemapRenderTarget = target.IsValid ? (target, face, mipLevel) : null;
        BoundRenderTarget = null;
    }

    /// <inheritdoc />
    public void UnbindRenderTarget()
    {
        BoundRenderTarget = null;
        BoundCubemapRenderTarget = null;
    }

    /// <inheritdoc />
    public TextureHandle GetRenderTargetColorTexture(RenderTargetHandle target)
    {
        if (RenderTargets.TryGetValue(target, out var info) && !info.IsDepthOnly)
        {
            return info.ColorTexture;
        }

        return TextureHandle.Invalid;
    }

    /// <inheritdoc />
    public TextureHandle GetRenderTargetDepthTexture(RenderTargetHandle target)
    {
        if (RenderTargets.TryGetValue(target, out var info))
        {
            return info.DepthTexture;
        }

        return TextureHandle.Invalid;
    }

    /// <inheritdoc />
    public TextureHandle GetCubemapRenderTargetTexture(CubemapRenderTargetHandle target)
    {
        if (CubemapRenderTargets.TryGetValue(target, out var info))
        {
            return info.CubemapTexture;
        }

        return TextureHandle.Invalid;
    }

    /// <inheritdoc />
    public void DeleteRenderTarget(RenderTargetHandle target)
    {
        RenderTargets.Remove(target);
        if (BoundRenderTarget == target)
        {
            BoundRenderTarget = null;
        }
    }

    /// <inheritdoc />
    public void DeleteCubemapRenderTarget(CubemapRenderTargetHandle target)
    {
        CubemapRenderTargets.Remove(target);
        if (BoundCubemapRenderTarget?.Handle == target)
        {
            BoundCubemapRenderTarget = null;
        }
    }

    #endregion

    #region Test Control

    /// <summary>
    /// Resets all tracking state.
    /// </summary>
    public void Reset()
    {
        // Clear non-default resources
        var defaultShaders = new[] { LitShader, UnlitShader, SolidShader, PbrShader, PbrShadowShader };
        foreach (var shader in Shaders.Keys.Except(defaultShaders).ToList())
        {
            Shaders.Remove(shader);
        }

        var defaultTextures = new[] { WhiteTexture };
        foreach (var texture in Textures.Keys.Except(defaultTextures).ToList())
        {
            Textures.Remove(texture);
        }

        Meshes.Clear();
        InstanceBuffers.Clear();
        MeshDrawCalls.Clear();
        InstancedMeshDrawCalls.Clear();
        UniformValues.Clear();
        RenderTargets.Clear();
        CubemapRenderTargets.Clear();
        boundTextures.Clear();
        boundShader = default;
        boundMesh = default;
        BoundRenderTarget = null;
        BoundCubemapRenderTarget = null;
        RenderState.Reset();
    }

    /// <summary>
    /// Clears only the draw calls, keeping resources.
    /// </summary>
    public void ClearDrawCalls()
    {
        MeshDrawCalls.Clear();
        InstancedMeshDrawCalls.Clear();
    }

    private MeshHandle AllocateMeshHandle()
    {
        return new MeshHandle(nextHandleId++);
    }

    private TextureHandle AllocateTextureHandle(int width, int height)
    {
        return new TextureHandle(nextHandleId++, width, height);
    }

    private ShaderHandle AllocateShaderHandle()
    {
        return new ShaderHandle(nextHandleId++);
    }

    #endregion

    /// <inheritdoc />
    public void Dispose()
    {
        if (!disposed)
        {
            disposed = true;
            Reset();
        }
    }
}

#region Supporting Types

/// <summary>
/// Information about a created mesh.
/// </summary>
/// <param name="VertexCount">The number of vertices.</param>
/// <param name="IndexCount">The number of indices.</param>
/// <param name="VertexData">The raw vertex data.</param>
/// <param name="IndexData">The raw index data.</param>
public sealed class MockMeshInfo(int VertexCount, int IndexCount, byte[]? VertexData, uint[]? IndexData)
{
    /// <summary>
    /// Gets the number of vertices.
    /// </summary>
    public int VertexCount { get; } = VertexCount;

    /// <summary>
    /// Gets the number of indices.
    /// </summary>
    public int IndexCount { get; } = IndexCount;

    /// <summary>
    /// Gets the raw vertex data.
    /// </summary>
    public byte[]? VertexData { get; } = VertexData;

    /// <summary>
    /// Gets the raw index data.
    /// </summary>
    public uint[]? IndexData { get; } = IndexData;

    /// <summary>
    /// Gets or sets whether this is a cube primitive.
    /// </summary>
    public bool IsCube { get; set; }

    /// <summary>
    /// Gets or sets whether this is a quad primitive.
    /// </summary>
    public bool IsQuad { get; set; }

    /// <summary>
    /// Gets or sets the size (for cubes).
    /// </summary>
    public float Size { get; set; }

    /// <summary>
    /// Gets or sets the width (for quads).
    /// </summary>
    public float Width { get; set; }

    /// <summary>
    /// Gets or sets the height (for quads).
    /// </summary>
    public float Height { get; set; }
}

/// <summary>
/// Information about a created texture.
/// </summary>
/// <param name="Width">The texture width.</param>
/// <param name="Height">The texture height.</param>
/// <param name="SourcePath">The source file path, if loaded from file.</param>
public sealed class MockTextureInfo(int Width, int Height, string? SourcePath)
{
    /// <summary>
    /// Gets the texture width.
    /// </summary>
    public int Width { get; } = Width;

    /// <summary>
    /// Gets the texture height.
    /// </summary>
    public int Height { get; } = Height;

    /// <summary>
    /// Gets the source file path.
    /// </summary>
    public string? SourcePath { get; } = SourcePath;

    /// <summary>
    /// Gets or sets the raw pixel data.
    /// </summary>
    public byte[]? Data { get; set; }

    /// <summary>
    /// Gets or sets the compressed texture format, if this is a compressed texture.
    /// </summary>
    public CompressedTextureFormat? CompressedFormat { get; set; }

    /// <summary>
    /// Gets or sets the number of mipmap levels.
    /// </summary>
    public int MipmapCount { get; set; }
}

/// <summary>
/// Information about a created shader.
/// </summary>
/// <param name="VertexSource">The vertex shader source.</param>
/// <param name="FragmentSource">The fragment shader source.</param>
public sealed record MockShaderInfo(string? VertexSource, string? FragmentSource);

/// <summary>
/// A recorded mesh draw call.
/// </summary>
/// <param name="Mesh">The mesh that was drawn.</param>
/// <param name="Shader">The shader that was bound.</param>
/// <param name="Textures">The textures that were bound.</param>
/// <param name="Uniforms">The uniform values that were set.</param>
public sealed record MeshDrawCall(
    MeshHandle Mesh,
    ShaderHandle Shader,
    Dictionary<int, TextureHandle> Textures,
    Dictionary<string, object> Uniforms);

/// <summary>
/// A recorded instanced mesh draw call.
/// </summary>
/// <param name="Mesh">The mesh that was drawn.</param>
/// <param name="InstanceBuffer">The instance buffer used.</param>
/// <param name="InstanceCount">The number of instances drawn.</param>
/// <param name="Shader">The shader that was bound.</param>
/// <param name="Textures">The textures that were bound.</param>
/// <param name="Uniforms">The uniform values that were set.</param>
public sealed record InstancedMeshDrawCall(
    MeshHandle Mesh,
    InstanceBufferHandle InstanceBuffer,
    int InstanceCount,
    ShaderHandle Shader,
    Dictionary<int, TextureHandle> Textures,
    Dictionary<string, object> Uniforms);

/// <summary>
/// Information about a created instance buffer.
/// </summary>
/// <param name="MaxInstances">The maximum number of instances the buffer can hold.</param>
public sealed class MockInstanceBufferInfo(int MaxInstances)
{
    /// <summary>
    /// Gets the maximum number of instances.
    /// </summary>
    public int MaxInstances { get; } = MaxInstances;

    /// <summary>
    /// Gets or sets the current number of instances stored.
    /// </summary>
    public int CurrentInstanceCount { get; set; }

    /// <summary>
    /// Gets or sets the instance data.
    /// </summary>
    public InstanceData[]? Data { get; set; }
}

/// <summary>
/// Tracks render state for the mock context.
/// </summary>
public sealed class MockContextRenderState
{
    /// <summary>
    /// Gets or sets the clear color.
    /// </summary>
    public Vector4 ClearColor { get; set; }

    /// <summary>
    /// Gets or sets the last clear mask.
    /// </summary>
    public ClearMask LastClearMask { get; set; }

    /// <summary>
    /// Gets the number of clear calls.
    /// </summary>
    public int ClearCount { get; set; }

    /// <summary>
    /// Gets or sets the viewport.
    /// </summary>
    public (int X, int Y, int Width, int Height) Viewport { get; set; }

    /// <summary>
    /// Gets or sets whether depth testing is enabled.
    /// </summary>
    public bool DepthTestEnabled { get; set; }

    /// <summary>
    /// Gets or sets whether blending is enabled.
    /// </summary>
    public bool BlendingEnabled { get; set; }

    /// <summary>
    /// Gets or sets whether culling is enabled.
    /// </summary>
    public bool CullingEnabled { get; set; }

    /// <summary>
    /// Gets or sets the cull face mode.
    /// </summary>
    public CullFaceMode CullFaceMode { get; set; }

    /// <summary>
    /// Gets the number of ProcessEvents calls.
    /// </summary>
    public int ProcessEventsCount { get; set; }

    /// <summary>
    /// Gets the number of SwapBuffers calls.
    /// </summary>
    public int SwapBuffersCount { get; set; }

    /// <summary>
    /// Resets the render state.
    /// </summary>
    public void Reset()
    {
        ClearColor = Vector4.Zero;
        LastClearMask = default;
        ClearCount = 0;
        Viewport = default;
        DepthTestEnabled = false;
        BlendingEnabled = false;
        CullingEnabled = false;
        CullFaceMode = default;
        ProcessEventsCount = 0;
        SwapBuffersCount = 0;
    }
}

/// <summary>
/// Information about a created render target.
/// </summary>
/// <param name="Width">The width in pixels.</param>
/// <param name="Height">The height in pixels.</param>
/// <param name="Format">The render target format.</param>
public sealed class MockRenderTargetInfo(int Width, int Height, RenderTargetFormat Format)
{
    private static int nextTextureId = 10000;

    /// <summary>
    /// Gets the width in pixels.
    /// </summary>
    public int Width { get; } = Width;

    /// <summary>
    /// Gets the height in pixels.
    /// </summary>
    public int Height { get; } = Height;

    /// <summary>
    /// Gets the render target format.
    /// </summary>
    public RenderTargetFormat Format { get; } = Format;

    /// <summary>
    /// Gets or sets whether this is a depth-only render target.
    /// </summary>
    public bool IsDepthOnly { get; set; }

    /// <summary>
    /// Gets the color texture handle.
    /// </summary>
    public TextureHandle ColorTexture { get; } = new TextureHandle(System.Threading.Interlocked.Increment(ref nextTextureId), Width, Height);

    /// <summary>
    /// Gets the depth texture handle.
    /// </summary>
    public TextureHandle DepthTexture { get; } = new TextureHandle(System.Threading.Interlocked.Increment(ref nextTextureId), Width, Height);
}

/// <summary>
/// Information about a created cubemap render target.
/// </summary>
/// <param name="Size">The size of each face in pixels.</param>
/// <param name="HasDepth">Whether the target has a depth buffer.</param>
/// <param name="MipLevels">The number of mip levels.</param>
public sealed class MockCubemapRenderTargetInfo(int Size, bool HasDepth, int MipLevels)
{
    private static int nextTextureId = 20000;

    /// <summary>
    /// Gets the size of each face in pixels.
    /// </summary>
    public int Size { get; } = Size;

    /// <summary>
    /// Gets whether the target has a depth buffer.
    /// </summary>
    public bool HasDepth { get; } = HasDepth;

    /// <summary>
    /// Gets the number of mip levels.
    /// </summary>
    public int MipLevels { get; } = MipLevels;

    /// <summary>
    /// Gets the cubemap texture handle.
    /// </summary>
    public TextureHandle CubemapTexture { get; } = new TextureHandle(System.Threading.Interlocked.Increment(ref nextTextureId), Size, Size);
}

#endregion
