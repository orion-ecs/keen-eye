using System.Numerics;

using KeenEyes.Graphics.Abstractions;
using KeenEyes.Graphics.Silk.Resources;
using KeenEyes.Graphics.Silk.Shaders;

namespace KeenEyes.Graphics.Silk;

/// <summary>
/// Silk.NET OpenGL implementation of <see cref="IGraphicsContext"/>.
/// </summary>
/// <remarks>
/// <para>
/// This context provides a complete graphics API using Silk.NET and OpenGL as the backend.
/// It manages the window, device, and all GPU resources (meshes, textures, shaders).
/// </para>
/// <para>
/// Usage: Install <see cref="SilkGraphicsPlugin"/> to get access to this context via
/// <c>world.GetExtension&lt;IGraphicsContext&gt;()</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// world.InstallPlugin(new SilkGraphicsPlugin(config));
/// var graphics = world.GetExtension&lt;IGraphicsContext&gt;();
///
/// graphics.OnReady += () =>
/// {
///     // Create resources here
///     var mesh = graphics.CreateMesh(vertices, vertexCount, indices);
/// };
///
/// graphics.OnRender += deltaTime =>
/// {
///     graphics.Clear(ClearMask.ColorBuffer | ClearMask.DepthBuffer);
///     // Draw calls
/// };
///
/// graphics.Initialize();
/// graphics.Run();
/// </code>
/// </example>
[PluginExtension("SilkGraphics")]
public sealed class SilkGraphicsContext(SilkGraphicsConfig? config = null) : IGraphicsContext
{
    private readonly SilkGraphicsConfig config = config ?? new SilkGraphicsConfig();
    private readonly MeshManager meshManager = new();
    private readonly TextureManager textureManager = new();
    private readonly ShaderManager shaderManager = new();

    private SilkWindow? window;
    private IGraphicsDevice? device;
    private int currentBoundShaderId = -1;
    private bool initialized;
    private bool disposed;
    private bool gpuResourcesDisposed;

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

    /// <inheritdoc />
    public bool ShouldClose => window?.IsClosing ?? true;

    /// <summary>
    /// Gets the current window width in pixels.
    /// </summary>
    public int Width => window?.Width ?? 0;

    /// <summary>
    /// Gets the current window height in pixels.
    /// </summary>
    public int Height => window?.Height ?? 0;

    /// <inheritdoc />
    public event Action? OnReady;

    /// <inheritdoc />
    public event Action<int, int>? OnResize;

    /// <inheritdoc />
    public event Action? OnClosing;

    /// <inheritdoc />
    public event Action<float>? OnUpdate;

    /// <inheritdoc />
    public event Action<float>? OnRender;

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
    /// The built-in white texture handle (1x1 white pixel).
    /// </summary>
    public TextureHandle WhiteTexture { get; private set; }

    internal MeshManager MeshManager => meshManager;
    internal TextureManager TextureManager => textureManager;
    internal ShaderManager ShaderManager => shaderManager;

    /// <inheritdoc />
    public void Initialize()
    {
        if (initialized)
        {
            return;
        }

        // Create the window
        window = new SilkWindow(config);

        // Hook up window events
        window.OnLoad += HandleWindowLoad;
        window.OnResize += HandleWindowResize;
        window.OnClosing += HandleWindowClosing;
        window.OnUpdate += HandleWindowUpdate;
        window.OnRender += HandleWindowRender;
    }

    /// <inheritdoc />
    public void Run()
    {
        if (window is null)
        {
            throw new InvalidOperationException(
                "Call Initialize() before Run().");
        }

        window.Run();
    }

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
        if (window is null)
        {
            return;
        }

        // Create device from window
        device = window.CreateDevice();

        // Initialize resource managers with the device
        meshManager.Device = device;
        textureManager.Device = device;
        shaderManager.Device = device;

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
        OnReady?.Invoke();
    }

    private void HandleWindowResize(int width, int height)
    {
        device?.Viewport(0, 0, (uint)width, (uint)height);
        OnResize?.Invoke(width, height);
    }

    private void HandleWindowClosing()
    {
        OnClosing?.Invoke();

        // Dispose GPU resources while the OpenGL context is still valid
        DisposeGpuResources();
    }

    private void HandleWindowUpdate(double deltaTime)
    {
        OnUpdate?.Invoke((float)deltaTime);
    }

    private void HandleWindowRender(double deltaTime)
    {
        OnRender?.Invoke((float)deltaTime);
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

        // Create default white texture
        var whiteId = textureManager.CreateSolidColorTexture(255, 255, 255, 255);
        WhiteTexture = new TextureHandle(whiteId);
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
        return new TextureHandle(id);
    }

    /// <inheritdoc />
    public TextureHandle LoadTexture(string path)
    {
        // TODO: Implement texture loading from file
        throw new NotImplementedException("Texture loading from file not yet implemented.");
    }

    /// <summary>
    /// Creates a solid color texture.
    /// </summary>
    /// <param name="r">Red component (0-255).</param>
    /// <param name="g">Green component (0-255).</param>
    /// <param name="b">Blue component (0-255).</param>
    /// <param name="a">Alpha component (0-255).</param>
    /// <returns>The texture handle.</returns>
    public TextureHandle CreateSolidColorTexture(byte r, byte g, byte b, byte a = 255)
    {
        var id = textureManager.CreateSolidColorTexture(r, g, b, a);
        return new TextureHandle(id);
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

    private void DisposeGpuResources()
    {
        if (gpuResourcesDisposed)
        {
            return;
        }

        gpuResourcesDisposed = true;

        meshManager.Dispose();
        textureManager.Dispose();
        shaderManager.Dispose();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        if (window is not null)
        {
            window.OnLoad -= HandleWindowLoad;
            window.OnResize -= HandleWindowResize;
            window.OnClosing -= HandleWindowClosing;
            window.OnUpdate -= HandleWindowUpdate;
            window.OnRender -= HandleWindowRender;
        }

        DisposeGpuResources();
        window?.Dispose();
    }
}
