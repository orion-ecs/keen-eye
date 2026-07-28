using KeenEyes.Platform.Silk;
using Silk.NET.Input;
using Silk.NET.Windowing;

namespace KeenEyes.Graphics.Tests.Mocks;

/// <summary>
/// Simple mock implementation of <see cref="ISilkWindowProvider"/> for testing.
/// This mock does not create a real window and is suitable for CI environments.
/// </summary>
public sealed class MockSilkWindowProvider : ISilkWindowProvider
{
    private bool disposed;

    /// <summary>
    /// Gets a null window reference. Tests using this mock should not access Window directly.
    /// </summary>
    public IWindow Window => null!;

    public IInputContext InputContext => null!;

    /// <summary>
    /// Gets or sets the window width in logical points.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Gets or sets the window height in logical points.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Gets or sets the framebuffer width in device pixels.
    /// </summary>
    /// <remarks>
    /// Set this to a multiple of <see cref="Width"/> to simulate a HiDPI display.
    /// </remarks>
    public int FramebufferWidth { get; set; }

    /// <summary>
    /// Gets or sets the framebuffer height in device pixels.
    /// </summary>
    /// <remarks>
    /// Set this to a multiple of <see cref="Height"/> to simulate a HiDPI display.
    /// </remarks>
    public int FramebufferHeight { get; set; }

    public event Action? OnLoad;
    public event Action<double>? OnUpdate;
    public event Action<double>? OnRender;
    public event Action<int, int>? OnResize;
    public event Action<int, int>? OnFramebufferResize;
    public event Action? OnClosing;

    /// <summary>
    /// Simulates the window loading.
    /// </summary>
    public void SimulateLoad()
    {
        OnLoad?.Invoke();
    }

    /// <summary>
    /// Simulates an update frame.
    /// </summary>
    public void SimulateUpdate(double deltaTime)
    {
        OnUpdate?.Invoke(deltaTime);
    }

    /// <summary>
    /// Simulates a render frame.
    /// </summary>
    public void SimulateRender(double deltaTime)
    {
        OnRender?.Invoke(deltaTime);
    }

    /// <summary>
    /// Simulates a window resize, which reports logical points.
    /// </summary>
    /// <remarks>
    /// Updates <see cref="Width"/>/<see cref="Height"/> before raising the event, the way a
    /// real window does. The framebuffer size is deliberately left alone - raise
    /// <see cref="SimulateFramebufferResize"/> for that.
    /// </remarks>
    public void SimulateResize(int width, int height)
    {
        Width = width;
        Height = height;
        OnResize?.Invoke(width, height);
    }

    /// <summary>
    /// Simulates a framebuffer resize, which reports device pixels.
    /// </summary>
    /// <remarks>
    /// Updates <see cref="FramebufferWidth"/>/<see cref="FramebufferHeight"/> before raising
    /// the event, the way a real window does.
    /// </remarks>
    public void SimulateFramebufferResize(int width, int height)
    {
        FramebufferWidth = width;
        FramebufferHeight = height;
        OnFramebufferResize?.Invoke(width, height);
    }

    /// <summary>
    /// Simulates window closing.
    /// </summary>
    public void SimulateClosing()
    {
        OnClosing?.Invoke();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
    }
}
