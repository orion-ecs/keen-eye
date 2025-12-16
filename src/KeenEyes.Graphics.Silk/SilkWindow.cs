using KeenEyes.Graphics.Abstractions;
using KeenEyes.Graphics.Silk.Backend;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using IWindow = KeenEyes.Graphics.Abstractions.IWindow;
using SilkIWindow = Silk.NET.Windowing.IWindow;

namespace KeenEyes.Graphics.Silk;

/// <summary>
/// Silk.NET implementation of <see cref="IWindow"/>.
/// </summary>
/// <remarks>
/// <para>
/// This class wraps an existing Silk.NET window to provide the KeenEyes graphics abstraction layer.
/// The window itself is created and managed by <c>SilkWindowPlugin</c>.
/// </para>
/// </remarks>
internal sealed class SilkWindow : IWindow
{
    private readonly SilkIWindow window;
    private GL? gl;
    private bool disposed;

    /// <summary>
    /// Wraps an existing Silk.NET window.
    /// </summary>
    /// <param name="existingWindow">The existing window to wrap.</param>
    public SilkWindow(SilkIWindow existingWindow)
    {
        window = existingWindow;

        // Wire up Silk.NET events to our events
        window.Load += HandleLoad;
        window.Resize += HandleResize;
        window.Closing += HandleClosing;
        window.Update += HandleUpdate;
        window.Render += HandleRender;
    }

    /// <inheritdoc />
    public int Width => window.Size.X;

    /// <inheritdoc />
    public int Height => window.Size.Y;

    /// <inheritdoc />
    public string Title => window.Title;

    /// <inheritdoc />
    public bool IsClosing => window.IsClosing;

    /// <inheritdoc />
    public bool IsFocused => window.IsVisible; // Silk.NET doesn't expose IsFocused directly

    /// <inheritdoc />
    public float AspectRatio => Height > 0 ? (float)Width / Height : 1f;

    /// <inheritdoc />
    public event Action? OnLoad;

    /// <inheritdoc />
    public event Action<int, int>? OnResize;

    /// <inheritdoc />
    public event Action? OnClosing;

    /// <inheritdoc />
    public event Action<double>? OnUpdate;

    /// <inheritdoc />
    public event Action<double>? OnRender;

    /// <summary>
    /// Gets the underlying Silk.NET window.
    /// </summary>
    internal SilkIWindow NativeWindow => window;

    /// <summary>
    /// Gets the OpenGL context, available after Load event.
    /// </summary>
    internal GL? GL => gl;

    /// <inheritdoc />
    public IGraphicsDevice CreateDevice()
    {
        if (gl is null)
        {
            throw new InvalidOperationException(
                "Cannot create device before window is loaded. " +
                "Call CreateDevice() from the OnLoad event handler.");
        }

        return new OpenGLDevice(gl);
    }

    /// <inheritdoc />
    public void Run()
    {
        window.Run();
    }

    /// <inheritdoc />
    public void DoEvents()
    {
        window.DoEvents();
    }

    /// <inheritdoc />
    public void SwapBuffers()
    {
        window.SwapBuffers();
    }

    /// <inheritdoc />
    public void Close()
    {
        window.Close();
    }

    private void HandleLoad()
    {
        gl = window.CreateOpenGL();
        OnLoad?.Invoke();
    }

    private void HandleResize(Vector2D<int> size)
    {
        OnResize?.Invoke(size.X, size.Y);
    }

    private void HandleClosing()
    {
        OnClosing?.Invoke();
    }

    private void HandleUpdate(double deltaTime)
    {
        OnUpdate?.Invoke(deltaTime);
    }

    private void HandleRender(double deltaTime)
    {
        OnRender?.Invoke(deltaTime);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        window.Load -= HandleLoad;
        window.Resize -= HandleResize;
        window.Closing -= HandleClosing;
        window.Update -= HandleUpdate;
        window.Render -= HandleRender;

        gl?.Dispose();

        // Note: We don't dispose the window - it's owned by SilkWindowPlugin
    }
}
