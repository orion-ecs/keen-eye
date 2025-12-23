using KeenEyes.Platform.Silk;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace KeenEyes.Graphics.Tests.Mocks;

/// <summary>
/// Mock implementation of <see cref="ISilkWindowProvider"/> for testing.
/// This provider creates a headless window for testing graphics functionality without requiring a display.
/// </summary>
public sealed class MockSilkWindowProvider : ISilkWindowProvider
{
    private bool disposed;
    private readonly IWindow window;

    public MockSilkWindowProvider()
    {
        // Create a headless Silk.NET window for testing
        var options = WindowOptions.Default with
        {
            Title = "Mock Window",
            Size = new Vector2D<int>(800, 600),
            IsVisible = false, // Headless
            API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.ForwardCompatible, new APIVersion(3, 3))
        };

        window = global::Silk.NET.Windowing.Window.Create(options);
    }

    public IWindow Window => window;

    public IInputContext InputContext => null!; // Not needed for graphics tests

    public event Action? OnLoad;
    public event Action<double>? OnUpdate;
    public event Action<double>? OnRender;
    public event Action<int, int>? OnResize;
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
    /// Simulates window resize.
    /// </summary>
    public void SimulateResize(int width, int height)
    {
        OnResize?.Invoke(width, height);
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
        window?.Dispose();
    }
}

