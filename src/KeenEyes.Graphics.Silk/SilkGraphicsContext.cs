using System.Numerics;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Graphics.Silk.Rendering2D;
using KeenEyes.Graphics.Silk.Resources;
using KeenEyes.Graphics.Silk.Shaders;
using KeenEyes.Graphics.Silk.Text;
using KeenEyes.Platform.Silk;
using StbImageSharp;

namespace KeenEyes.Graphics.Silk;

/// <summary>
/// Silk.NET OpenGL implementation of <see cref="IGraphicsContext"/>.
/// </summary>
/// <remarks>
/// <para>
/// This context provides a complete graphics API using Silk.NET and OpenGL as the backend.
/// It manages GPU resources (meshes, textures, shaders) and provides rendering operations.
/// </para>
/// <para>
/// This context requires <see cref="SilkWindowPlugin"/> to be installed first. The window
/// plugin provides the main loop via <see cref="ILoopProvider"/>. Graphics subscribes to
/// window lifecycle events internally to initialize the device when the window loads.
/// </para>
/// <para>
/// Usage: Install <see cref="SilkWindowPlugin"/> first, then <see cref="SilkGraphicsPlugin"/>.
/// Use <see cref="ILoopProvider"/> (from the window plugin) for the main application loop.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Install window plugin first (provides ILoopProvider)
/// world.InstallPlugin(new SilkWindowPlugin(windowConfig));
/// world.InstallPlugin(new SilkGraphicsPlugin(graphicsConfig));
///
/// var graphics = world.GetExtension&lt;IGraphicsContext&gt;();
///
/// // Use WorldRunnerBuilder for the main loop
/// world.CreateRunner()
///     .OnReady(() =&gt;
///     {
///         // Create resources here
///         var mesh = graphics.CreateMesh(vertices, vertexCount, indices);
///     })
///     .OnRender(deltaTime =&gt;
///     {
///         graphics.Clear(ClearMask.ColorBuffer | ClearMask.DepthBuffer);
///         // Draw calls
///     })
///     .Run();
/// </code>
/// </example>
[PluginExtension("SilkGraphics")]
public sealed class SilkGraphicsContext : IGraphicsContext, I2DRendererProvider, ITextRendererProvider, IFontManagerProvider
{
    private readonly SilkGraphicsConfig config;
    private readonly ISilkWindowProvider windowProvider;
    private readonly MeshManager meshManager = new();
    private readonly TextureManager textureManager = new();
    private readonly ShaderManager shaderManager = new();
    private readonly InstanceBufferManager instanceBufferManager = new();

    private SilkWindow? window;
    private IGraphicsDevice? device;
    private RenderTargetManager? renderTargetManager;
    private Silk2DRenderer? renderer2D;
    private SilkFontManager? fontManager;
    private SilkTextRenderer? textRenderer;
    private int currentBoundShaderId = -1;
    private bool initialized;
    private bool disposed;
    private bool gpuResourcesDisposed;

    /// <summary>
    /// Initializes a new instance of <see cref="SilkGraphicsContext"/> using a shared window.
    /// </summary>
    /// <param name="windowProvider">The window provider for the shared window.</param>
    /// <param name="config">The graphics configuration (rendering settings only).</param>
    internal SilkGraphicsContext(ISilkWindowProvider windowProvider, SilkGraphicsConfig? config = null)
    {
        this.windowProvider = windowProvider;
        this.config = config ?? new SilkGraphicsConfig();

        // Subscribe to window lifecycle events
        windowProvider.OnLoad += HandleWindowLoad;
        windowProvider.OnResize += HandleWindowResize;
        windowProvider.OnClosing += HandleWindowClosing;
    }

    /// <summary>
    /// Gets the graphics configuration.
    /// </summary>
    public SilkGraphicsConfig Config => config;

    /// <inheritdoc />
    public IWindow? Window => window;

    /// <inheritdoc />
    public IGraphicsDevice? Device => device;

    /// <inheritdoc />
    public bool IsInitialized => initialized;

    /// <summary>
    /// Gets the current window width in pixels.
    /// </summary>
    public int Width => window?.Width ?? 0;

    /// <summary>
    /// Gets the current window height in pixels.
    /// </summary>
    public int Height => window?.Height ?? 0;

    /// <summary>
    /// The built-in unlit shader handle.
    /// </summary>
    public ShaderHandle UnlitShader { get; private set; }

    /// <summary>
    /// The built-in lit shader handle.
    /// </summary>
    public ShaderHandle LitShader { get; private set; }

    /// <summary>
    /// The built-in solid color shader handle.
    /// </summary>
    public ShaderHandle SolidShader { get; private set; }

    /// <summary>
    /// The built-in PBR (Physically Based Rendering) shader handle.
    /// </summary>
    public ShaderHandle PbrShader { get; private set; }

    /// <summary>
    /// The built-in PBR shader with Cascaded Shadow Maps support.
    /// </summary>
    public ShaderHandle PbrShadowShader { get; private set; }

    /// <summary>
    /// The built-in PBR shader with Image-Based Lighting support.
    /// </summary>
    public ShaderHandle PbrIblShader { get; private set; }

    /// <summary>
    /// The built-in white texture handle (1x1 white pixel).
    /// </summary>
    public TextureHandle WhiteTexture { get; private set; }

    /// <summary>
    /// The built-in instanced lit shader handle.
    /// </summary>
    public ShaderHandle InstancedLitShader { get; private set; }

    /// <summary>
    /// The built-in instanced unlit shader handle.
    /// </summary>
    public ShaderHandle InstancedUnlitShader { get; private set; }

    /// <summary>
    /// The built-in instanced solid color shader handle.
    /// </summary>
    public ShaderHandle InstancedSolidShader { get; private set; }

    /// <summary>
    /// Gets the 2D renderer for UI and sprite rendering.
    /// </summary>
    /// <remarks>
    /// This is only available after initialization (when the window loads).
    /// </remarks>
    public I2DRenderer? Renderer2D => renderer2D;

    /// <inheritdoc />
    public I2DRenderer? Get2DRenderer() => renderer2D;

    /// <summary>
    /// Gets the text renderer for font rendering.
    /// </summary>
    /// <remarks>
    /// This is only available after initialization (when the window loads).
    /// </remarks>
    public ITextRenderer? TextRenderer => textRenderer;

    /// <inheritdoc />
    public ITextRenderer? GetTextRenderer() => textRenderer;

    /// <summary>
    /// Gets the font manager for loading and managing fonts.
    /// </summary>
    /// <remarks>
    /// This is only available after initialization (when the window loads).
    /// </remarks>
    public IFontManager? FontManager => fontManager;

    /// <inheritdoc />
    public IFontManager? GetFontManager() => fontManager;

    internal MeshManager MeshManager => meshManager;
    internal TextureManager TextureManager => textureManager;
    internal ShaderManager ShaderManager => shaderManager;

    /// <inheritdoc />
    public void ProcessEvents()
    {
        window?.DoEvents();
    }

    /// <inheritdoc />
    public void SwapBuffers()
    {
        window?.SwapBuffers();
    }

    private void HandleWindowLoad()
    {
        // Wrap the shared window. Pass isAlreadyLoaded=true because this is called
        // from the window's Load event, so the window is already initialized.
        window = new SilkWindow(windowProvider.Window, isAlreadyLoaded: true);

        // Create device from window
        device = window.CreateDevice();

        // Initialize resource managers with the device
        meshManager.Device = device;
        textureManager.Device = device;
        shaderManager.Device = device;
        instanceBufferManager.Device = device;
        renderTargetManager = new RenderTargetManager(device);

        // Apply default settings
        if (config.EnableDepthTest)
        {
            device.Enable(RenderCapability.DepthTest);
        }

        if (config.EnableCulling)
        {
            device.Enable(RenderCapability.CullFace);
            device.CullFace(CullFaceMode.Back);
        }

        // Set clear color
        device.ClearColor(config.ClearColor.X, config.ClearColor.Y, config.ClearColor.Z, config.ClearColor.W);

        // Create built-in resources
        CreateBuiltInResources();

        initialized = true;
    }

    private void HandleWindowResize(int width, int height)
    {
        device?.Viewport(0, 0, (uint)width, (uint)height);
        renderer2D?.SetScreenSize(width, height);
        textRenderer?.SetScreenSize(width, height);
    }

    private void HandleWindowClosing()
    {
        // Dispose GPU resources while the OpenGL context is still valid
        DisposeGpuResources();
    }

    private void CreateBuiltInResources()
    {
        // Create default shaders
        var unlitId = shaderManager.CreateShader(
            DefaultShaders.UnlitVertexShader,
            DefaultShaders.UnlitFragmentShader);
        UnlitShader = new ShaderHandle(unlitId);

        var litId = shaderManager.CreateShader(
            DefaultShaders.LitVertexShader,
            DefaultShaders.LitFragmentShader);
        LitShader = new ShaderHandle(litId);

        var solidId = shaderManager.CreateShader(
            DefaultShaders.SolidVertexShader,
            DefaultShaders.SolidFragmentShader);
        SolidShader = new ShaderHandle(solidId);

        var pbrId = shaderManager.CreateShader(
            DefaultShaders.PbrVertexShader,
            DefaultShaders.PbrFragmentShader);
        PbrShader = new ShaderHandle(pbrId);

        var pbrShadowId = shaderManager.CreateShader(
            PbrShadowShaders.PbrShadowVertexShader,
            PbrShadowShaders.PbrShadowFragmentShader);
        PbrShadowShader = new ShaderHandle(pbrShadowId);

        var pbrIblId = shaderManager.CreateShader(
            DefaultShaders.PbrVertexShader,
            DefaultShaders.PbrIblFragmentShader);
        PbrIblShader = new ShaderHandle(pbrIblId);

        // Create instanced shaders (use same fragment shaders)
        var instancedUnlitId = shaderManager.CreateShader(
            DefaultShaders.InstancedUnlitVertexShader,
            DefaultShaders.UnlitFragmentShader);
        InstancedUnlitShader = new ShaderHandle(instancedUnlitId);

        var instancedLitId = shaderManager.CreateShader(
            DefaultShaders.InstancedLitVertexShader,
            DefaultShaders.LitFragmentShader);
        InstancedLitShader = new ShaderHandle(instancedLitId);

        var instancedSolidId = shaderManager.CreateShader(
            DefaultShaders.InstancedSolidVertexShader,
            DefaultShaders.SolidFragmentShader);
        InstancedSolidShader = new ShaderHandle(instancedSolidId);

        // Create default white texture (1x1 pixel)
        var whiteId = textureManager.CreateSolidColorTexture(255, 255, 255, 255);
        WhiteTexture = new TextureHandle(whiteId, 1, 1);

        // Create 2D renderer
        renderer2D = new Silk2DRenderer(device!, textureManager, Width, Height);

        // Create font manager and text renderer
        fontManager = new SilkFontManager(device!);
        textRenderer = new SilkTextRenderer(device!, fontManager, fontManager.TextureManager, Width, Height);
    }

    #region Mesh Operations

    /// <inheritdoc />
    public MeshHandle CreateMesh(ReadOnlySpan<byte> vertices, int vertexCount, ReadOnlySpan<uint> indices)
    {
        // Convert byte span to Vertex span (assuming Vertex struct layout)
        var vertexSize = System.Runtime.InteropServices.Marshal.SizeOf<Vertex>();
        if (vertices.Length != vertexCount * vertexSize)
        {
            throw new ArgumentException(
                $"Vertex data size ({vertices.Length}) does not match expected size ({vertexCount * vertexSize}).");
        }

        var vertexArray = new Vertex[vertexCount];
        var byteSpan = System.Runtime.InteropServices.MemoryMarshal.AsBytes(vertexArray.AsSpan());
        vertices.CopyTo(byteSpan);

        var id = meshManager.CreateMesh(vertexArray, indices.ToArray());
        return new MeshHandle(id);
    }

    /// <summary>
    /// Creates a mesh from typed vertex data.
    /// </summary>
    /// <param name="vertices">The vertex data.</param>
    /// <param name="indices">The index data.</param>
    /// <returns>The mesh handle.</returns>
    public MeshHandle CreateMesh(ReadOnlySpan<Vertex> vertices, ReadOnlySpan<uint> indices)
    {
        var id = meshManager.CreateMesh(vertices, indices);
        return new MeshHandle(id);
    }

    /// <summary>
    /// Creates a simple quad mesh.
    /// </summary>
    /// <param name="width">The width of the quad.</param>
    /// <param name="height">The height of the quad.</param>
    /// <returns>The mesh handle.</returns>
    public MeshHandle CreateQuad(float width = 1f, float height = 1f)
    {
        float hw = width / 2f;
        float hh = height / 2f;

        Vertex[] vertices =
        [
            new(new Vector3(-hw, -hh, 0), Vector3.UnitZ, new Vector2(0, 0)),
            new(new Vector3(hw, -hh, 0), Vector3.UnitZ, new Vector2(1, 0)),
            new(new Vector3(hw, hh, 0), Vector3.UnitZ, new Vector2(1, 1)),
            new(new Vector3(-hw, hh, 0), Vector3.UnitZ, new Vector2(0, 1))
        ];

        uint[] indices = [0, 1, 2, 0, 2, 3];

        return CreateMesh(vertices, indices);
    }

    /// <summary>
    /// Creates a simple cube mesh.
    /// </summary>
    /// <param name="size">The size of the cube.</param>
    /// <returns>The mesh handle.</returns>
    public MeshHandle CreateCube(float size = 1f)
    {
        float hs = size / 2f;

        Vertex[] vertices =
        [
            // Front face
            new(new Vector3(-hs, -hs, hs), new Vector3(0, 0, 1), new Vector2(0, 0)),
            new(new Vector3(hs, -hs, hs), new Vector3(0, 0, 1), new Vector2(1, 0)),
            new(new Vector3(hs, hs, hs), new Vector3(0, 0, 1), new Vector2(1, 1)),
            new(new Vector3(-hs, hs, hs), new Vector3(0, 0, 1), new Vector2(0, 1)),
            // Back face
            new(new Vector3(hs, -hs, -hs), new Vector3(0, 0, -1), new Vector2(0, 0)),
            new(new Vector3(-hs, -hs, -hs), new Vector3(0, 0, -1), new Vector2(1, 0)),
            new(new Vector3(-hs, hs, -hs), new Vector3(0, 0, -1), new Vector2(1, 1)),
            new(new Vector3(hs, hs, -hs), new Vector3(0, 0, -1), new Vector2(0, 1)),
            // Top face
            new(new Vector3(-hs, hs, hs), new Vector3(0, 1, 0), new Vector2(0, 0)),
            new(new Vector3(hs, hs, hs), new Vector3(0, 1, 0), new Vector2(1, 0)),
            new(new Vector3(hs, hs, -hs), new Vector3(0, 1, 0), new Vector2(1, 1)),
            new(new Vector3(-hs, hs, -hs), new Vector3(0, 1, 0), new Vector2(0, 1)),
            // Bottom face
            new(new Vector3(-hs, -hs, -hs), new Vector3(0, -1, 0), new Vector2(0, 0)),
            new(new Vector3(hs, -hs, -hs), new Vector3(0, -1, 0), new Vector2(1, 0)),
            new(new Vector3(hs, -hs, hs), new Vector3(0, -1, 0), new Vector2(1, 1)),
            new(new Vector3(-hs, -hs, hs), new Vector3(0, -1, 0), new Vector2(0, 1)),
            // Right face
            new(new Vector3(hs, -hs, hs), new Vector3(1, 0, 0), new Vector2(0, 0)),
            new(new Vector3(hs, -hs, -hs), new Vector3(1, 0, 0), new Vector2(1, 0)),
            new(new Vector3(hs, hs, -hs), new Vector3(1, 0, 0), new Vector2(1, 1)),
            new(new Vector3(hs, hs, hs), new Vector3(1, 0, 0), new Vector2(0, 1)),
            // Left face
            new(new Vector3(-hs, -hs, -hs), new Vector3(-1, 0, 0), new Vector2(0, 0)),
            new(new Vector3(-hs, -hs, hs), new Vector3(-1, 0, 0), new Vector2(1, 0)),
            new(new Vector3(-hs, hs, hs), new Vector3(-1, 0, 0), new Vector2(1, 1)),
            new(new Vector3(-hs, hs, -hs), new Vector3(-1, 0, 0), new Vector2(0, 1))
        ];

        uint[] indices =
        [
            0, 1, 2, 0, 2, 3,       // Front
            4, 5, 6, 4, 6, 7,       // Back
            8, 9, 10, 8, 10, 11,    // Top
            12, 13, 14, 12, 14, 15, // Bottom
            16, 17, 18, 16, 18, 19, // Right
            20, 21, 22, 20, 22, 23  // Left
        ];

        return CreateMesh(vertices, indices);
    }

    /// <inheritdoc />
    public void DeleteMesh(MeshHandle handle)
    {
        meshManager.DeleteMesh(handle.Id);
    }

    /// <inheritdoc />
    public void BindMesh(MeshHandle handle)
    {
        var data = meshManager.GetMesh(handle.Id);
        if (data is not null)
        {
            device?.BindVertexArray(data.Vao);
        }
    }

    /// <inheritdoc />
    public void DrawMesh(MeshHandle handle)
    {
        var data = meshManager.GetMesh(handle.Id);
        if (data is not null && device is not null)
        {
            device.BindVertexArray(data.Vao);
            device.DrawElements(PrimitiveType.Triangles, (uint)data.IndexCount, IndexType.UnsignedInt);
        }
    }

    #endregion

    #region Texture Operations

    /// <inheritdoc />
    public TextureHandle CreateTexture(int width, int height, ReadOnlySpan<byte> pixels)
    {
        var id = textureManager.CreateTexture(width, height, pixels);
        return new TextureHandle(id, width, height);
    }

    /// <inheritdoc />
    public TextureHandle LoadTexture(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Texture file not found.", path);
        }

        using var stream = File.OpenRead(path);
        ImageResult image;

        try
        {
            image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            throw new ArgumentException($"Failed to load texture from '{path}'. The file may be corrupted or in an unsupported format.", nameof(path), ex);
        }

        return CreateTexture(image.Width, image.Height, image.Data);
    }

    /// <inheritdoc />
    public TextureHandle CreateCompressedTexture(
        int width,
        int height,
        CompressedTextureFormat format,
        ReadOnlySpan<ReadOnlyMemory<byte>> mipmaps)
    {
        var id = textureManager.CreateCompressedTexture(width, height, format, mipmaps);
        return new TextureHandle(id, width, height);
    }

    /// <summary>
    /// Creates a solid color texture (1x1 pixel).
    /// </summary>
    /// <param name="r">Red component (0-255).</param>
    /// <param name="g">Green component (0-255).</param>
    /// <param name="b">Blue component (0-255).</param>
    /// <param name="a">Alpha component (0-255).</param>
    /// <returns>The texture handle.</returns>
    public TextureHandle CreateSolidColorTexture(byte r, byte g, byte b, byte a = 255)
    {
        var id = textureManager.CreateSolidColorTexture(r, g, b, a);
        return new TextureHandle(id, 1, 1);
    }

    /// <inheritdoc />
    public void DeleteTexture(TextureHandle handle)
    {
        textureManager.DeleteTexture(handle.Id);
    }

    /// <inheritdoc />
    public void BindTexture(TextureHandle handle, int unit = 0)
    {
        var data = textureManager.GetTexture(handle.Id);
        if (data is not null && device is not null)
        {
            device.ActiveTexture((TextureUnit)((int)TextureUnit.Texture0 + unit));
            device.BindTexture(TextureTarget.Texture2D, data.Handle);
        }
    }

    /// <inheritdoc />
    public TextureHandle CreateHdrTexture(int width, int height, ReadOnlySpan<float> pixels)
    {
        if (device is null)
        {
            throw new InvalidOperationException("Graphics device not initialized");
        }

        uint textureId = device.GenTexture();
        device.BindTexture(TextureTarget.Texture2D, textureId);

        // Upload HDR (RGB32F) data
        device.TexImage2D(TextureTarget.Texture2D, 0, width, height, PixelFormat.RGB32F, pixels);

        device.TexParameter(TextureTarget.Texture2D, TextureParam.MinFilter, (int)TextureMinFilter.Linear);
        device.TexParameter(TextureTarget.Texture2D, TextureParam.MagFilter, (int)TextureMagFilter.Linear);
        device.TexParameter(TextureTarget.Texture2D, TextureParam.WrapS, (int)TextureWrapMode.ClampToEdge);
        device.TexParameter(TextureTarget.Texture2D, TextureParam.WrapT, (int)TextureWrapMode.ClampToEdge);

        device.BindTexture(TextureTarget.Texture2D, 0);

        // Register with texture manager and return handle
        int handleId = textureManager.RegisterExternalTexture(textureId, width, height);
        return new TextureHandle(handleId, width, height);
    }

    /// <inheritdoc />
    public void BindCubemapTexture(TextureHandle handle, int unit = 0)
    {
        if (device is not null)
        {
            device.ActiveTexture((TextureUnit)((int)TextureUnit.Texture0 + unit));
            device.BindTexture(TextureTarget.TextureCubeMap, (uint)handle.Id);
        }
    }

    /// <inheritdoc />
    public void DeleteCubemapTexture(TextureHandle handle)
    {
        if (device is not null && handle.IsValid)
        {
            device.DeleteTexture((uint)handle.Id);
        }
    }

    #endregion

    #region Shader Operations

    /// <inheritdoc />
    public ShaderHandle CreateShader(string vertexSource, string fragmentSource)
    {
        var id = shaderManager.CreateShader(vertexSource, fragmentSource);
        return new ShaderHandle(id);
    }

    /// <inheritdoc />
    public void DeleteShader(ShaderHandle handle)
    {
        shaderManager.DeleteShader(handle.Id);
    }

    /// <inheritdoc />
    public void BindShader(ShaderHandle handle)
    {
        var data = shaderManager.GetShader(handle.Id);
        if (data is not null)
        {
            device?.UseProgram(data.Handle);
            currentBoundShaderId = handle.Id;
        }
        else
        {
            device?.UseProgram(0);
            currentBoundShaderId = -1;
        }
    }

    /// <inheritdoc />
    public void SetUniform(string name, float value)
    {
        // Need current shader to get uniform location
        // For now, use direct device call if shader is bound
        device?.Uniform1(GetUniformLocation(name), value);
    }

    /// <inheritdoc />
    public void SetUniform(string name, int value)
    {
        device?.Uniform1(GetUniformLocation(name), value);
    }

    /// <inheritdoc />
    public void SetUniform(string name, Vector2 value)
    {
        device?.Uniform2(GetUniformLocation(name), value.X, value.Y);
    }

    /// <inheritdoc />
    public void SetUniform(string name, Vector3 value)
    {
        device?.Uniform3(GetUniformLocation(name), value.X, value.Y, value.Z);
    }

    /// <inheritdoc />
    public void SetUniform(string name, Vector4 value)
    {
        device?.Uniform4(GetUniformLocation(name), value.X, value.Y, value.Z, value.W);
    }

    /// <inheritdoc />
    public void SetUniform(string name, in Matrix4x4 value)
    {
        device?.UniformMatrix4(GetUniformLocation(name), value);
    }

    private int GetUniformLocation(string name)
    {
        if (currentBoundShaderId < 0)
        {
            return -1;
        }

        return shaderManager.GetUniformLocation(currentBoundShaderId, name);
    }

    #endregion

    #region Instance Buffer Operations

    /// <inheritdoc />
    public InstanceBufferHandle CreateInstanceBuffer(int maxInstances)
    {
        var id = instanceBufferManager.CreateInstanceBuffer(maxInstances);
        return new InstanceBufferHandle(id);
    }

    /// <inheritdoc />
    public void UpdateInstanceBuffer(InstanceBufferHandle buffer, ReadOnlySpan<InstanceData> data)
    {
        instanceBufferManager.UpdateInstanceBuffer(buffer.Id, data);
    }

    /// <inheritdoc />
    public void DeleteInstanceBuffer(InstanceBufferHandle buffer)
    {
        instanceBufferManager.DeleteBuffer(buffer.Id);
    }

    /// <inheritdoc />
    public void DrawMeshInstanced(MeshHandle mesh, InstanceBufferHandle instances, int instanceCount)
    {
        if (device is null)
        {
            return;
        }

        var meshData = meshManager.GetMesh(mesh.Id);
        var instanceBufferData = instanceBufferManager.GetBuffer(instances.Id);

        if (meshData is null || instanceBufferData is null)
        {
            return;
        }

        // Bind the mesh VAO
        device.BindVertexArray(meshData.Vao);

        // Bind instance buffer and set up per-instance vertex attributes
        instanceBufferManager.BindInstanceBufferToVao(instances.Id);

        // Draw all instances with a single call
        device.DrawElementsInstanced(PrimitiveType.Triangles, (uint)meshData.IndexCount, IndexType.UnsignedInt, (uint)instanceCount);
    }

    #endregion

    #region Render State

    /// <inheritdoc />
    public void SetClearColor(Vector4 color)
    {
        device?.ClearColor(color.X, color.Y, color.Z, color.W);
    }

    /// <inheritdoc />
    public void Clear(ClearMask mask)
    {
        device?.Clear(mask);
    }

    /// <inheritdoc />
    public void SetViewport(int x, int y, int width, int height)
    {
        device?.Viewport(x, y, (uint)width, (uint)height);
    }

    /// <inheritdoc />
    public void SetDepthTest(bool enabled)
    {
        if (enabled)
        {
            device?.Enable(RenderCapability.DepthTest);
        }
        else
        {
            device?.Disable(RenderCapability.DepthTest);
        }
    }

    /// <inheritdoc />
    public void SetBlending(bool enabled)
    {
        if (enabled)
        {
            device?.Enable(RenderCapability.Blend);
        }
        else
        {
            device?.Disable(RenderCapability.Blend);
        }
    }

    /// <inheritdoc />
    public void SetCulling(bool enabled, CullFaceMode mode = CullFaceMode.Back)
    {
        if (enabled)
        {
            device?.Enable(RenderCapability.CullFace);
            device?.CullFace(mode);
        }
        else
        {
            device?.Disable(RenderCapability.CullFace);
        }
    }

    #endregion

    #region Render Target Operations

    /// <inheritdoc />
    public RenderTargetHandle CreateRenderTarget(int width, int height, RenderTargetFormat format)
    {
        if (renderTargetManager is null)
        {
            throw new InvalidOperationException("Graphics context is not initialized.");
        }

        return renderTargetManager.CreateRenderTarget(width, height, format);
    }

    /// <inheritdoc />
    public RenderTargetHandle CreateDepthOnlyRenderTarget(int width, int height)
    {
        if (renderTargetManager is null)
        {
            throw new InvalidOperationException("Graphics context is not initialized.");
        }

        return renderTargetManager.CreateDepthOnlyRenderTarget(width, height);
    }

    /// <inheritdoc />
    public CubemapRenderTargetHandle CreateCubemapRenderTarget(int size, bool withDepth, int mipLevels = 1)
    {
        if (renderTargetManager is null)
        {
            throw new InvalidOperationException("Graphics context is not initialized.");
        }

        return renderTargetManager.CreateCubemapRenderTarget(size, withDepth, mipLevels);
    }

    /// <inheritdoc />
    public void BindRenderTarget(RenderTargetHandle target)
    {
        if (renderTargetManager is null)
        {
            throw new InvalidOperationException("Graphics context is not initialized.");
        }

        renderTargetManager.BindRenderTarget(target);
    }

    /// <inheritdoc />
    public void BindCubemapRenderTarget(CubemapRenderTargetHandle target, CubemapFace face, int mipLevel = 0)
    {
        if (renderTargetManager is null)
        {
            throw new InvalidOperationException("Graphics context is not initialized.");
        }

        renderTargetManager.BindCubemapRenderTarget(target, face, mipLevel);
    }

    /// <inheritdoc />
    public void UnbindRenderTarget()
    {
        renderTargetManager?.UnbindRenderTarget();

        // Restore the default viewport
        if (window is not null)
        {
            device?.Viewport(0, 0, (uint)window.Width, (uint)window.Height);
        }
    }

    /// <inheritdoc />
    public TextureHandle GetRenderTargetColorTexture(RenderTargetHandle target)
    {
        if (renderTargetManager is null)
        {
            return TextureHandle.Invalid;
        }

        var textureId = renderTargetManager.GetColorTextureId(target);
        if (textureId == 0)
        {
            return TextureHandle.Invalid;
        }

        return new TextureHandle((int)textureId, target.Width, target.Height);
    }

    /// <inheritdoc />
    public TextureHandle GetRenderTargetDepthTexture(RenderTargetHandle target)
    {
        if (renderTargetManager is null)
        {
            return TextureHandle.Invalid;
        }

        var textureId = renderTargetManager.GetDepthTextureId(target);
        if (textureId == 0)
        {
            return TextureHandle.Invalid;
        }

        return new TextureHandle((int)textureId, target.Width, target.Height);
    }

    /// <inheritdoc />
    public TextureHandle GetCubemapRenderTargetTexture(CubemapRenderTargetHandle target)
    {
        if (renderTargetManager is null)
        {
            return TextureHandle.Invalid;
        }

        var textureId = renderTargetManager.GetCubemapTextureId(target);
        if (textureId == 0)
        {
            return TextureHandle.Invalid;
        }

        return new TextureHandle((int)textureId, target.Size, target.Size);
    }

    /// <inheritdoc />
    public void DeleteRenderTarget(RenderTargetHandle target)
    {
        renderTargetManager?.DeleteRenderTarget(target);
    }

    /// <inheritdoc />
    public void DeleteRenderTargetKeepTexture(RenderTargetHandle target)
    {
        renderTargetManager?.DeleteRenderTargetKeepTexture(target);
    }

    /// <inheritdoc />
    public void DeleteCubemapRenderTarget(CubemapRenderTargetHandle target)
    {
        renderTargetManager?.DeleteCubemapRenderTarget(target);
    }

    /// <inheritdoc />
    public void DeleteCubemapRenderTargetKeepTexture(CubemapRenderTargetHandle target)
    {
        renderTargetManager?.DeleteCubemapRenderTargetKeepTexture(target);
    }

    #endregion

    private void DisposeGpuResources()
    {
        if (gpuResourcesDisposed)
        {
            return;
        }

        gpuResourcesDisposed = true;

        renderTargetManager?.Dispose();
        renderer2D?.Dispose();
        textRenderer?.Dispose();
        fontManager?.Dispose();
        meshManager.Dispose();
        textureManager.Dispose();
        shaderManager.Dispose();
        instanceBufferManager.Dispose();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        // Unsubscribe from window provider events
        windowProvider.OnLoad -= HandleWindowLoad;
        windowProvider.OnResize -= HandleWindowResize;
        windowProvider.OnClosing -= HandleWindowClosing;

        DisposeGpuResources();

        // Note: We don't dispose the window - it's owned by SilkWindowPlugin
    }
}
