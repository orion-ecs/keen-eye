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
}
