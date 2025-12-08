using System.Diagnostics.CodeAnalysis;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace KeenEyes.Graphics.Backend;

/// <summary>
/// Silk.NET implementation of <see cref="IGraphicsWindow"/>.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Requires real window and GPU; tested via MockGraphicsWindow")]
internal sealed class SilkNetWindow : IGraphicsWindow
{
    private readonly IWindow window;
    private bool disposed;

    /// <inheritdoc />
    public int Width => window.Size.X;

    /// <inheritdoc />
    public int Height => window.Size.Y;

    /// <inheritdoc />
    public bool IsClosing => window.IsClosing;

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
    /// Creates a new Silk.NET window wrapper.
    /// </summary>
    /// <param name="config">The graphics configuration.</param>
    public SilkNetWindow(GraphicsConfig config)
    {
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

        window = Window.Create(windowOptions);
        window.Load += HandleLoad;
        window.Resize += HandleResize;
        window.Closing += HandleClosing;
        window.Update += HandleUpdate;
        window.Render += HandleRender;
    }

    private void HandleLoad()
    {
        OnLoad?.Invoke();
    }

    private void HandleResize(Silk.NET.Maths.Vector2D<int> size)
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
    public IGraphicsDevice CreateDevice()
    {
        var gl = window.CreateOpenGL();
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

        window.Dispose();
    }
}
