using System.Numerics;
using KeenEyes.Graphics.Backend;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace KeenEyes.Graphics;

/// <summary>
/// Configuration options for the graphics plugin.
/// </summary>
public sealed class GraphicsConfig
{
    /// <summary>
    /// The initial window width in pixels.
    /// </summary>
    public int WindowWidth { get; init; } = 1280;

    /// <summary>
    /// The initial window height in pixels.
    /// </summary>
    public int WindowHeight { get; init; } = 720;

    /// <summary>
    /// The window title.
    /// </summary>
    public string WindowTitle { get; init; } = "KeenEyes Application";

    /// <summary>
    /// Whether to enable VSync.
    /// </summary>
    public bool VSync { get; init; } = true;

    /// <summary>
    /// Whether the window is resizable.
    /// </summary>
    public bool Resizable { get; init; } = true;

    /// <summary>
    /// Whether to start in fullscreen mode.
    /// </summary>
    public bool Fullscreen { get; init; }

    /// <summary>
    /// The target frames per second (0 for unlimited).
    /// </summary>
    public double TargetFps { get; init; }

    /// <summary>
    /// The default clear color.
    /// </summary>
    public Vector4 ClearColor { get; init; } = new(0.1f, 0.1f, 0.1f, 1f);
}

/// <summary>
/// Main graphics API providing access to rendering functionality.
/// </summary>
/// <remarks>
/// <para>
/// The GraphicsContext is the primary interface for graphics operations in KeenEyes.
/// It provides methods for creating and managing GPU resources (meshes, textures, shaders),
/// as well as window and rendering control.
/// </para>
/// <para>
/// Access via <c>world.GetExtension&lt;GraphicsContext&gt;()</c> after installing the
/// <see cref="GraphicsPlugin"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var graphics = world.GetExtension&lt;GraphicsContext&gt;();
///
/// // Create resources
/// var meshId = graphics.CreateMesh(vertices, indices);
/// var textureId = graphics.CreateTexture(width, height, pixelData);
/// var shaderId = graphics.CreateShader(vertexSource, fragmentSource);
///
/// // Create entity with renderable
/// world.Spawn()
///     .With(Transform3D.Identity)
///     .With(new Renderable(meshId, materialId))
///     .Build();
/// </code>
/// </example>
[PluginExtension("Graphics")]
public sealed class GraphicsContext : IDisposable
{
    private readonly World world;
    private readonly GraphicsConfig config;
    private readonly MeshManager meshManager;
    private readonly TextureManager textureManager;
    private readonly ShaderManager shaderManager;

    private IWindow? window;
    private IGraphicsDevice? device;
    private bool disposed;
    private bool initialized;

    /// <summary>
    /// The built-in unlit shader ID.
    /// </summary>
    public int UnlitShaderId { get; private set; }

    /// <summary>
    /// The built-in lit shader ID.
    /// </summary>
    public int LitShaderId { get; private set; }

    /// <summary>
    /// The built-in solid color shader ID.
    /// </summary>
    public int SolidShaderId { get; private set; }

    /// <summary>
    /// The built-in white texture ID (1x1 white pixel).
    /// </summary>
    public int WhiteTextureId { get; private set; }

    /// <summary>
    /// Gets the current window, if initialized.
    /// </summary>
    public IWindow? Window => window;

    /// <summary>
    /// Gets the graphics device, if initialized.
    /// </summary>
    internal IGraphicsDevice? Device => device;

    /// <summary>
    /// Gets whether the graphics context has been initialized.
    /// </summary>
    public bool IsInitialized => initialized;

    /// <summary>
    /// Gets the graphics configuration.
    /// </summary>
    public GraphicsConfig Config => config;

    /// <summary>
    /// Event raised when the window is loaded and ready.
    /// </summary>
    public event Action? OnLoad;

    /// <summary>
    /// Event raised when the window is resized.
    /// </summary>
    public event Action<int, int>? OnResize;

    /// <summary>
    /// Event raised when the window is closing.
    /// </summary>
    public event Action? OnClosing;

    internal MeshManager MeshManager => meshManager;
    internal TextureManager TextureManager => textureManager;
    internal ShaderManager ShaderManager => shaderManager;

    /// <summary>
    /// Creates a new graphics context.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="config">The graphics configuration.</param>
    public GraphicsContext(World world, GraphicsConfig? config = null)
    {
        this.world = world;
        this.config = config ?? new GraphicsConfig();
        meshManager = new MeshManager();
        textureManager = new TextureManager();
        shaderManager = new ShaderManager();
    }

    /// <summary>
    /// Initializes the graphics context and creates the window.
    /// </summary>
    /// <remarks>
    /// This must be called before using any graphics features.
    /// The <see cref="GraphicsPlugin"/> calls this automatically during installation.
    /// </remarks>
    public void Initialize()
    {
        if (initialized)
        {
            return;
        }

        var windowOptions = WindowOptions.Default with
        {
            Size = new Silk.NET.Maths.Vector2D<int>(config.WindowWidth, config.WindowHeight),
            Title = config.WindowTitle,
            VSync = config.VSync,
            WindowBorder = config.Resizable ? WindowBorder.Resizable : WindowBorder.Fixed,
            WindowState = config.Fullscreen ? WindowState.Fullscreen : WindowState.Normal,
            FramesPerSecond = config.TargetFps > 0 ? config.TargetFps : 0,
            UpdatesPerSecond = config.TargetFps > 0 ? config.TargetFps : 0
        };

        window = Silk.NET.Windowing.Window.Create(windowOptions);
        window.Load += OnWindowLoad;
        window.Resize += OnWindowResize;
        window.Closing += OnWindowClosing;
    }

    private void OnWindowLoad()
    {
        var gl = window!.CreateOpenGL();
        device = new OpenGLDevice(gl);

        // Initialize resource managers with the device
        meshManager.Device = device;
        textureManager.Device = device;
        shaderManager.Device = device;

        // Create built-in resources
        CreateBuiltInResources();

        initialized = true;
        OnLoad?.Invoke();
    }

    private void OnWindowResize(Silk.NET.Maths.Vector2D<int> size)
    {
        device?.Viewport(0, 0, (uint)size.X, (uint)size.Y);
        OnResize?.Invoke(size.X, size.Y);
    }

    private void OnWindowClosing()
    {
        OnClosing?.Invoke();
    }

    private void CreateBuiltInResources()
    {
        // Create default shaders
        UnlitShaderId = shaderManager.CreateShader(
            DefaultShaders.UnlitVertexShader,
            DefaultShaders.UnlitFragmentShader);

        LitShaderId = shaderManager.CreateShader(
            DefaultShaders.LitVertexShader,
            DefaultShaders.LitFragmentShader);

        SolidShaderId = shaderManager.CreateShader(
            DefaultShaders.SolidVertexShader,
            DefaultShaders.SolidFragmentShader);

        // Create default white texture
        WhiteTextureId = textureManager.CreateSolidColorTexture(255, 255, 255, 255);
    }

    /// <summary>
    /// Runs the main window loop. Blocks until the window is closed.
    /// </summary>
    /// <remarks>
    /// This method blocks the calling thread. For integration with external
    /// game loops, use <see cref="ProcessEvents"/> and <see cref="SwapBuffers"/> instead.
    /// </remarks>
    public void Run()
    {
        window?.Run();
    }

    /// <summary>
    /// Processes pending window events without blocking.
    /// </summary>
    public void ProcessEvents()
    {
        window?.DoEvents();
    }

    /// <summary>
    /// Swaps the front and back buffers.
    /// </summary>
    public void SwapBuffers()
    {
        window?.SwapBuffers();
    }

    /// <summary>
    /// Gets whether the window should close.
    /// </summary>
    public bool ShouldClose => window?.IsClosing ?? true;

    #region Mesh API

    /// <summary>
    /// Creates a mesh from vertex and index data.
    /// </summary>
    /// <param name="vertices">The vertex data.</param>
    /// <param name="indices">The index data.</param>
    /// <returns>The mesh resource handle.</returns>
    public int CreateMesh(ReadOnlySpan<Vertex> vertices, ReadOnlySpan<uint> indices)
    {
        return meshManager.CreateMesh(vertices, indices);
    }

    /// <summary>
    /// Creates a simple quad mesh (two triangles forming a square).
    /// </summary>
    /// <param name="width">The width of the quad.</param>
    /// <param name="height">The height of the quad.</param>
    /// <returns>The mesh resource handle.</returns>
    public int CreateQuad(float width = 1f, float height = 1f)
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
    /// <returns>The mesh resource handle.</returns>
    public int CreateCube(float size = 1f)
    {
        float hs = size / 2f;

        // Each face has its own vertices for proper normals
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

    /// <summary>
    /// Deletes a mesh resource.
    /// </summary>
    /// <param name="meshId">The mesh resource handle.</param>
    /// <returns>True if deleted, false if not found.</returns>
    public bool DeleteMesh(int meshId)
    {
        return meshManager.DeleteMesh(meshId);
    }

    #endregion

    #region Texture API

    /// <summary>
    /// Creates a texture from raw RGBA pixel data.
    /// </summary>
    /// <param name="width">The texture width.</param>
    /// <param name="height">The texture height.</param>
    /// <param name="data">The RGBA pixel data.</param>
    /// <param name="filter">The texture filtering mode.</param>
    /// <param name="wrap">The texture wrapping mode.</param>
    /// <returns>The texture resource handle.</returns>
    public int CreateTexture(
        int width,
        int height,
        ReadOnlySpan<byte> data,
        TextureFilter filter = TextureFilter.Linear,
        TextureWrap wrap = TextureWrap.Repeat)
    {
        return textureManager.CreateTexture(width, height, data, filter, wrap);
    }

    /// <summary>
    /// Creates a solid color texture.
    /// </summary>
    /// <param name="r">Red component (0-255).</param>
    /// <param name="g">Green component (0-255).</param>
    /// <param name="b">Blue component (0-255).</param>
    /// <param name="a">Alpha component (0-255).</param>
    /// <returns>The texture resource handle.</returns>
    public int CreateSolidColorTexture(byte r, byte g, byte b, byte a = 255)
    {
        return textureManager.CreateSolidColorTexture(r, g, b, a);
    }

    /// <summary>
    /// Deletes a texture resource.
    /// </summary>
    /// <param name="textureId">The texture resource handle.</param>
    /// <returns>True if deleted, false if not found.</returns>
    public bool DeleteTexture(int textureId)
    {
        return textureManager.DeleteTexture(textureId);
    }

    #endregion

    #region Shader API

    /// <summary>
    /// Creates a shader program from vertex and fragment source code.
    /// </summary>
    /// <param name="vertexSource">The vertex shader GLSL source.</param>
    /// <param name="fragmentSource">The fragment shader GLSL source.</param>
    /// <returns>The shader resource handle.</returns>
    public int CreateShader(string vertexSource, string fragmentSource)
    {
        return shaderManager.CreateShader(vertexSource, fragmentSource);
    }

    /// <summary>
    /// Deletes a shader resource.
    /// </summary>
    /// <param name="shaderId">The shader resource handle.</param>
    /// <returns>True if deleted, false if not found.</returns>
    public bool DeleteShader(int shaderId)
    {
        return shaderManager.DeleteShader(shaderId);
    }

    #endregion

    #region Rendering API

    /// <summary>
    /// Clears the screen with the specified color.
    /// </summary>
    /// <param name="color">The clear color.</param>
    public void Clear(Vector4 color)
    {
        if (device is null)
        {
            return;
        }

        device.ClearColor(color.X, color.Y, color.Z, color.W);
        device.Clear(ClearMask.ColorBuffer | ClearMask.DepthBuffer);
    }

    /// <summary>
    /// Clears the screen with the default clear color.
    /// </summary>
    public void Clear()
    {
        Clear(config.ClearColor);
    }

    /// <summary>
    /// Enables depth testing.
    /// </summary>
    public void EnableDepthTest()
    {
        device?.Enable(RenderCapability.DepthTest);
    }

    /// <summary>
    /// Disables depth testing.
    /// </summary>
    public void DisableDepthTest()
    {
        device?.Disable(RenderCapability.DepthTest);
    }

    /// <summary>
    /// Enables backface culling.
    /// </summary>
    public void EnableCulling()
    {
        device?.Enable(RenderCapability.CullFace);
        device?.CullFace(CullFaceMode.Back);
    }

    /// <summary>
    /// Disables backface culling.
    /// </summary>
    public void DisableCulling()
    {
        device?.Disable(RenderCapability.CullFace);
    }

    /// <summary>
    /// Sets the viewport.
    /// </summary>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    /// <param name="width">The viewport width.</param>
    /// <param name="height">The viewport height.</param>
    public void SetViewport(int x, int y, int width, int height)
    {
        device?.Viewport(x, y, (uint)width, (uint)height);
    }

    #endregion

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        meshManager.Dispose();
        textureManager.Dispose();
        shaderManager.Dispose();

        device?.Dispose();
        window?.Dispose();
    }
}
