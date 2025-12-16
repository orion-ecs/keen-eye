using KeenEyes.Platform.Silk;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace KeenEyes.Graphics.Silk;

/// <summary>
/// Silk.NET OpenGL implementation of the graphics context.
/// </summary>
/// <remarks>
/// <para>
/// This context uses a shared window from <see cref="ISilkWindowProvider"/>,
/// allowing graphics and input plugins to share the same Silk.NET window.
/// </para>
/// <para>
/// The OpenGL context is created from the window during initialization.
/// Resources (meshes, textures, shaders) are managed internally and disposed
/// when the context is disposed.
/// </para>
/// </remarks>
[PluginExtension("SilkGraphics")]
public sealed class SilkGraphicsContext : IDisposable
{
    private readonly ISilkWindowProvider windowProvider;
    private readonly SilkGraphicsConfig config;
    private GL? gl;
    private bool initialized;
    private bool disposed;

    /// <summary>
    /// Gets the graphics configuration.
    /// </summary>
    public SilkGraphicsConfig Config => config;

    /// <summary>
    /// Gets whether the graphics context has been initialized.
    /// </summary>
    public bool IsInitialized => initialized;

    /// <summary>
    /// Gets the current window width.
    /// </summary>
    public int Width => windowProvider.Window.Size.X;

    /// <summary>
    /// Gets the current window height.
    /// </summary>
    public int Height => windowProvider.Window.Size.Y;

    /// <summary>
    /// Gets whether the window should close.
    /// </summary>
    public bool ShouldClose => windowProvider.Window.IsClosing;

    /// <summary>
    /// Event raised when the graphics context is initialized.
    /// </summary>
    public event Action? OnInitialized;

    /// <summary>
    /// Event raised when the window is resized.
    /// </summary>
    public event Action<int, int>? OnResize;

    internal SilkGraphicsContext(ISilkWindowProvider windowProvider, SilkGraphicsConfig config)
    {
        this.windowProvider = windowProvider;
        this.config = config;

        // Hook into window events
        windowProvider.Window.Load += OnWindowLoad;
        windowProvider.Window.Resize += OnWindowResize;
        windowProvider.Window.Closing += OnWindowClosing;
    }

    private void OnWindowLoad()
    {
        // Create OpenGL context from the shared window
        gl = windowProvider.Window.CreateOpenGL();

        // Apply default settings
        if (config.EnableDepthTest)
        {
            gl.Enable(EnableCap.DepthTest);
        }

        if (config.EnableCulling)
        {
            gl.Enable(EnableCap.CullFace);
            gl.CullFace(TriangleFace.Back);
        }

        initialized = true;
        OnInitialized?.Invoke();
    }

    private void OnWindowResize(Vector2D<int> size)
    {
        gl?.Viewport(0, 0, (uint)size.X, (uint)size.Y);
        OnResize?.Invoke(size.X, size.Y);
    }

    private void OnWindowClosing()
    {
        // Dispose GPU resources while the context is still valid
        DisposeGpuResources();
    }

    /// <summary>
    /// Clears the screen with the configured clear color.
    /// </summary>
    public void Clear()
    {
        Clear(config.ClearColor);
    }

    /// <summary>
    /// Clears the screen with the specified color.
    /// </summary>
    /// <param name="color">The clear color (RGBA).</param>
    public void Clear(System.Numerics.Vector4 color)
    {
        if (gl is null)
        {
            return;
        }

        gl.ClearColor(color.X, color.Y, color.Z, color.W);
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    /// <summary>
    /// Sets the viewport dimensions.
    /// </summary>
    public void SetViewport(int x, int y, int width, int height)
    {
        gl?.Viewport(x, y, (uint)width, (uint)height);
    }

    // TODO: Migrate mesh, texture, shader creation APIs from KeenEyes.Graphics

    private void DisposeGpuResources()
    {
        // TODO: Dispose managed GPU resources (meshes, textures, shaders)
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        windowProvider.Window.Load -= OnWindowLoad;
        windowProvider.Window.Resize -= OnWindowResize;
        windowProvider.Window.Closing -= OnWindowClosing;

        DisposeGpuResources();
        gl?.Dispose();
    }
}
