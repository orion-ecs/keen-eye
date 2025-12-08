using KeenEyes.Graphics.Backend;

namespace KeenEyes.Graphics.Tests.Mocks;

/// <summary>
/// Mock implementation of <see cref="IGraphicsWindow"/> for unit testing.
/// </summary>
public sealed class MockGraphicsWindow : IGraphicsWindow
{
    private readonly MockGraphicsDevice device;
    private bool disposed;
    private bool isClosing;

    /// <summary>
    /// Gets the mock graphics device created by this window.
    /// </summary>
    public MockGraphicsDevice MockDevice => device;

    /// <inheritdoc />
    public int Width { get; set; } = 1280;

    /// <inheritdoc />
    public int Height { get; set; } = 720;

    /// <inheritdoc />
    public bool IsClosing => isClosing;

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
    /// Creates a new mock graphics window.
    /// </summary>
    public MockGraphicsWindow()
    {
        device = new MockGraphicsDevice();
    }

    /// <summary>
    /// Creates a new mock graphics window with a custom device.
    /// </summary>
    /// <param name="device">The mock device to use.</param>
    public MockGraphicsWindow(MockGraphicsDevice device)
    {
        this.device = device;
    }

    /// <inheritdoc />
    public IGraphicsDevice CreateDevice()
    {
        return device;
    }

    /// <summary>
    /// Simulates the window load event.
    /// </summary>
    public void SimulateLoad()
    {
        OnLoad?.Invoke();
    }

    /// <summary>
    /// Simulates a window resize event.
    /// </summary>
    /// <param name="width">The new width.</param>
    /// <param name="height">The new height.</param>
    public void SimulateResize(int width, int height)
    {
        Width = width;
        Height = height;
        OnResize?.Invoke(width, height);
    }

    /// <summary>
    /// Simulates a frame update.
    /// </summary>
    /// <param name="deltaTime">The delta time in seconds.</param>
    public void SimulateUpdate(double deltaTime)
    {
        OnUpdate?.Invoke(deltaTime);
    }

    /// <summary>
    /// Simulates a frame render.
    /// </summary>
    /// <param name="deltaTime">The delta time in seconds.</param>
    public void SimulateRender(double deltaTime)
    {
        OnRender?.Invoke(deltaTime);
    }

    /// <summary>
    /// Simulates the window closing event.
    /// </summary>
    public void SimulateClosing()
    {
        isClosing = true;
        OnClosing?.Invoke();
    }

    /// <inheritdoc />
    public void Run()
    {
        // Mock: immediate return for testing
        SimulateLoad();
    }

    /// <inheritdoc />
    public void DoEvents()
    {
        // Mock: no-op
    }

    /// <inheritdoc />
    public void SwapBuffers()
    {
        // Mock: no-op
    }

    /// <inheritdoc />
    public void Close()
    {
        SimulateClosing();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        device.Dispose();
    }
}
