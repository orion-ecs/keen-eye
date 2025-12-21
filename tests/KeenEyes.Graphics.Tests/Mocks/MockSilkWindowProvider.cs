using KeenEyes.Platform.Silk;

using Silk.NET.Input;
using Silk.NET.Windowing;

namespace KeenEyes.Graphics.Tests.Mocks;

/// <summary>
/// Mock implementation of <see cref="ISilkWindowProvider"/> for testing.
/// </summary>
/// <param name="window">The window to use (null creates a mock).</param>
/// <param name="inputContext">The input context to use (null creates a mock).</param>
public sealed class MockSilkWindowProvider(IWindow? window = null, IInputContext? inputContext = null) : ISilkWindowProvider
{
    private bool disposed;

    public IWindow Window { get; } = window ?? CreateMockWindow();

    public IInputContext InputContext { get; } = inputContext ?? CreateMockInputContext();

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
    }

    private static IWindow CreateMockWindow()
    {
        // Return a minimal mock window
        // In real tests, this would need to be a proper mock/stub
        return null!;
    }

    private static IInputContext CreateMockInputContext()
    {
        // Return a minimal mock input context
        return null!;
    }
}
